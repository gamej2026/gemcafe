using UnityEngine;

namespace GemCafe.Stage
{
    /// <summary>
    /// 카메라 X 이동에 맞춰 끊김 없이 반복(루프)되는 스프라이트 배경.
    /// <para>
    /// - <see cref="parallaxFactor"/> 로 컴포넌트(레이어)별 이동 속도를 조절한다.<br/>
    ///   1 = 카메라와 함께 이동(가장 먼 배경, 멈춘 듯 보임),
    ///   0 = 월드에 고정(가장 가까운 배경, 카메라와 같은 속도로 흐름).<br/>
    /// - SpriteRenderer 를 <see cref="SpriteDrawMode.Tiled"/> 로 설정하고 카메라 가시 영역을
    ///   충분히 덮도록 가로 크기를 자동 확장하여, 좌우 어느 방향으로 이동해도 이음새 없이 반복된다.
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
        [Tooltip("스프라이트를 Tiled 모드로 두고 가로 크기를 자동으로 카메라 가시 영역에 맞춘다.")]
        [SerializeField] private bool autoTile = true;

        [Tooltip("카메라 가시 영역 좌우로 추가로 채울 여유 폭(월드 단위). 빠른 이동 시 가장자리 빈틈 방지.")]
        [SerializeField] private float padding = 2f;

        private SpriteRenderer _renderer;
        private Transform _camTransform;

        private float _unitWidth;        // 한 타일(원본 스프라이트)의 월드 가로 폭.
        private float _baseY;            // 시작 시점의 Y 위치.
        private float _baseZ;            // 시작 시점의 Z 위치.
        private float _lastOrthoSize;
        private float _lastAspect;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
        }

        private void OnEnable()
        {
            ResolveCamera();
            CacheBaseTransform();
            ConfigureTiling(force: true);
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

            ConfigureTiling(force: false);

            if (_unitWidth <= Mathf.Epsilon)
            {
                return;
            }

            float camX = _camTransform.position.x;

            // 배경 콘텐츠가 흘러가야 하는 거리(시차 계수 반영).
            // factor=1 이면 0(정지), factor=0 이면 camX 전체(완전 스크롤).
            float scrolled = camX * (1f - parallaxFactor);

            // 한 타일 폭 안으로 감싸서, 패턴이 반복되는 지점에서 위치를 되돌린다.
            // Tiled 패턴은 _unitWidth 마다 동일하므로 이 점프는 화면상 보이지 않는다.
            float wrapped = Mathf.Repeat(scrolled, _unitWidth);

            var pos = transform.position;
            pos.x = camX - wrapped;
            pos.y = followVertical ? _camTransform.position.y + _baseY : _baseY;
            pos.z = _baseZ;
            transform.position = pos;
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
        /// 스프라이트의 타일 폭을 계산하고, 필요하면 SpriteRenderer 를 Tiled 모드로 전환하여
        /// 카메라 가시 영역을 덮도록 가로 크기를 설정한다.
        /// </summary>
        private void ConfigureTiling(bool force)
        {
            if (_renderer == null || _renderer.sprite == null)
            {
                _unitWidth = 0f;
                return;
            }

            var sprite = _renderer.sprite;
            float scaleX = Mathf.Abs(transform.lossyScale.x);
            float nativeWidth = sprite.rect.width / sprite.pixelsPerUnit;
            _unitWidth = nativeWidth * scaleX;

            if (!autoTile || targetCamera == null || !targetCamera.orthographic)
            {
                return;
            }

            // 카메라 설정이 바뀌지 않았으면 매 프레임 재계산하지 않는다.
            if (!force &&
                Mathf.Approximately(_lastOrthoSize, targetCamera.orthographicSize) &&
                Mathf.Approximately(_lastAspect, targetCamera.aspect))
            {
                return;
            }

            _lastOrthoSize = targetCamera.orthographicSize;
            _lastAspect = targetCamera.aspect;

            if (_renderer.drawMode == SpriteDrawMode.Simple)
            {
                _renderer.drawMode = SpriteDrawMode.Tiled;
                _renderer.tileMode = SpriteTileMode.Continuous;
            }

            float viewWidth = targetCamera.orthographicSize * 2f * targetCamera.aspect;

            // 가시 영역 + 좌우 여유 + 한 타일 폭(랩 이동분)을 모두 덮도록 한다.
            float targetWorldWidth = viewWidth + (padding * 2f) + (_unitWidth * 2f);

            if (scaleX <= Mathf.Epsilon)
            {
                return;
            }

            // 가로(Size.x)만 조정한다. 세로(Size.y)는 인스펙터 설정값을 그대로 유지.
            var size = _renderer.size;
            size.x = targetWorldWidth / scaleX;
            _renderer.size = size;
        }
    }
}
