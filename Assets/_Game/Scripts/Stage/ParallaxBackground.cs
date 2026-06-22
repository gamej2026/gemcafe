using System.Collections.Generic;
using UnityEngine;

namespace GemCafe.Stage
{
    /// <summary>
    /// 카메라 X 이동에 맞춰 끊김 없이 반복(루프)되는 스프라이트 배경.
    /// <para>
    /// - <see cref="parallaxFactor"/> 로 컴포넌트(레이어)별 이동 속도를 조절한다.<br/>
    ///   1 = 카메라와 함께 이동(가장 먼 배경, 멈춘 듯 보임),
    ///   0 = 월드에 고정(가장 가까운 배경, 카메라와 같은 속도로 흐름).<br/>
    /// - 사용 중인 이미지의 좌우 끝단이 서로 일치하지 않아 단순 반복 시 이음새가 보이는 문제를
    ///   방지하기 위해, 인접 타일을 <b>좌우 반전(거울상)</b> 으로 이어 붙인다.<br/>
    ///   짝수 타일은 원본, 홀수 타일은 좌우 반전이므로 맞닿는 가장자리 픽셀이 항상 동일해
    ///   이음새가 보이지 않는다.
    /// </para>
    /// 같은 오브젝트에 여러 배경 레이어를 두고 <see cref="parallaxFactor"/> 를 다르게 주면
    /// 시차(parallax) 스크롤 효과를 얻을 수 있다.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class ParallaxBackground : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("기준이 되는 카메라. 비워두면 Camera.main 을 사용한다.")]
        [SerializeField] private Camera targetCamera;

        [Header("Scroll")]
        [Tooltip("카메라 이동 대비 배경의 이동 속도(시차 계수).\n" +
                 "1 = 카메라와 함께 이동(먼 배경, 멈춘 듯 보임), 0 = 월드 고정(가까운 배경, 빠르게 흐름).")]
        [Range(0f, 1f)]
        [SerializeField] private float parallaxFactor = 0.5f;

        [Tooltip("Y 축도 카메라를 따라가게 할지 여부(세로 스크롤이 있을 때 사용).")]
        [SerializeField] private bool followVertical;

        [Header("Tiling")]
        [Tooltip("카메라 가시 영역 좌우로 추가로 채울 여유 폭(월드 단위). 빠른 이동 시 가장자리 빈틈 방지.")]
        [SerializeField] private float padding = 2f;

        private SpriteRenderer _source;
        private Transform _camTransform;

        private readonly List<SpriteRenderer> _tiles = new List<SpriteRenderer>();

        private float _unitWidth;        // 한 타일(원본 스프라이트)의 월드 가로 폭.
        private float _baseY;            // 시작 시점의 Y 위치(또는 카메라 기준 오프셋).
        private float _baseZ;            // 시작 시점의 Z 위치.
        private float _lastOrthoSize;
        private float _lastAspect;

        private void Awake()
        {
            _source = GetComponent<SpriteRenderer>();
        }

        private void OnEnable()
        {
            ResolveCamera();
            CacheBaseTransform();
            RebuildTiles(force: true);
        }

        private void OnDisable()
        {
            // 생성한 타일을 정리하고 원본 렌더러를 복구한다.
            for (int i = _tiles.Count - 1; i >= 0; i--)
            {
                if (_tiles[i] != null)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(_tiles[i].gameObject);
                    }
                    else
                    {
                        DestroyImmediate(_tiles[i].gameObject);
                    }
                }
            }

            _tiles.Clear();

            if (_source != null)
            {
                _source.enabled = true;
            }
        }

        private void LateUpdate()
        {
            if (_camTransform == null)
            {
                ResolveCamera();
                if (_camTransform == null)
                {
                    return;
                }
            }

            RebuildTiles(force: false);

            if (_unitWidth <= Mathf.Epsilon || _tiles.Count == 0 || targetCamera == null)
            {
                return;
            }

            float camX = _camTransform.position.x;
            float y = followVertical ? _camTransform.position.y + _baseY : _baseY;

            // 배경 콘텐츠 원점은 시차 계수만큼만 카메라를 따라간다.
            // (factor=1 → 카메라와 함께 이동, factor=0 → 월드 고정)
            float originX = parallaxFactor * camX;

            float halfView = targetCamera.orthographic
                ? targetCamera.orthographicSize * targetCamera.aspect
                : _unitWidth;

            float leftWorld = camX - halfView - padding;

            // 화면 왼쪽 바깥에서 시작하도록 시작 인덱스를 한 칸 앞당긴다.
            int startIndex = Mathf.FloorToInt((leftWorld - originX) / _unitWidth) - 1;

            for (int k = 0; k < _tiles.Count; k++)
            {
                var tile = _tiles[k];
                if (tile == null)
                {
                    continue;
                }

                int idx = startIndex + k;
                float worldX = originX + idx * _unitWidth;

                tile.transform.position = new Vector3(worldX, y, _baseZ);

                // 인접 타일을 거울상으로: 홀수 인덱스는 좌우 반전.
                // 반전 시 맞닿는 가장자리 픽셀이 서로 동일해져 이음새가 사라진다.
                tile.flipX = (idx % 2) != 0;
            }
        }

        private void ResolveCamera()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            _camTransform = targetCamera != null ? targetCamera.transform : null;
        }

        private void CacheBaseTransform()
        {
            var pos = transform.position;
            // followVertical 사용 시 _baseY 는 카메라 기준 오프셋으로, 아닐 때는 절대 Y 로 동작.
            _baseY = followVertical && _camTransform != null ? pos.y - _camTransform.position.y : pos.y;
            _baseZ = pos.z;
        }

        /// <summary>
        /// 타일 폭을 계산하고, 가시 영역을 덮을 만큼의 자식 타일 스프라이트를 생성/유지한다.
        /// 원본 SpriteRenderer 는 비활성화하고 생성된 타일들로 그린다.
        /// </summary>
        private void RebuildTiles(bool force)
        {
            if (_source == null || _source.sprite == null)
            {
                _unitWidth = 0f;
                return;
            }

            var sprite = _source.sprite;
            float scaleX = Mathf.Abs(transform.lossyScale.x);
            float nativeWidth = sprite.rect.width / sprite.pixelsPerUnit;
            _unitWidth = nativeWidth * scaleX;

            if (_unitWidth <= Mathf.Epsilon)
            {
                return;
            }

            // 원본 렌더러는 끄고, 동일한 스프라이트로 타일을 그린다.
            if (_source.enabled)
            {
                _source.enabled = false;
            }

            float halfView = targetCamera != null && targetCamera.orthographic
                ? targetCamera.orthographicSize * targetCamera.aspect
                : _unitWidth;

            // 가시 폭 + 좌우 여유를 덮는 데 필요한 타일 수(+여분). 거울상 패턴 유지를 위해 짝수로 맞춘다.
            float coverWidth = (halfView * 2f) + (padding * 2f);
            int needed = Mathf.CeilToInt(coverWidth / _unitWidth) + 3;
            if ((needed & 1) != 0)
            {
                needed++;
            }

            bool sizeChanged = targetCamera != null &&
                               (!Mathf.Approximately(_lastOrthoSize, targetCamera.orthographicSize) ||
                                !Mathf.Approximately(_lastAspect, targetCamera.aspect));

            if (!force && !sizeChanged && _tiles.Count == needed)
            {
                return;
            }

            if (targetCamera != null)
            {
                _lastOrthoSize = targetCamera.orthographicSize;
                _lastAspect = targetCamera.aspect;
            }

            // 부족하면 생성, 남으면 제거.
            while (_tiles.Count < needed)
            {
                _tiles.Add(CreateTile(_tiles.Count));
            }

            while (_tiles.Count > needed)
            {
                int last = _tiles.Count - 1;
                if (_tiles[last] != null)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(_tiles[last].gameObject);
                    }
                    else
                    {
                        DestroyImmediate(_tiles[last].gameObject);
                    }
                }

                _tiles.RemoveAt(last);
            }

            // 스프라이트/정렬/색상 등 원본 속성을 타일에 동기화.
            for (int i = 0; i < _tiles.Count; i++)
            {
                SyncTile(_tiles[i]);
            }
        }

        private SpriteRenderer CreateTile(int index)
        {
            var go = new GameObject($"ParallaxTile_{index}");
            go.transform.SetParent(transform, worldPositionStays: false);
            go.transform.localScale = Vector3.one;

            var sr = go.AddComponent<SpriteRenderer>();
            SyncTile(sr);
            return sr;
        }

        private void SyncTile(SpriteRenderer sr)
        {
            if (sr == null || _source == null)
            {
                return;
            }

            sr.sprite = _source.sprite;
            sr.color = _source.color;
            sr.sharedMaterial = _source.sharedMaterial;
            sr.sortingLayerID = _source.sortingLayerID;
            sr.sortingOrder = _source.sortingOrder;
            sr.maskInteraction = _source.maskInteraction;
            sr.drawMode = SpriteDrawMode.Simple;
            sr.flipY = _source.flipY;
        }
    }
}
