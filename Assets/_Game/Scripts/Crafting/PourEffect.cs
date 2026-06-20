using UnityEngine;

namespace GemCafe.Crafting
{
    /// <summary>
    /// Pour_Effect 오브젝트에 부착되어, 인스펙터에서 직렬화로 할당한 ParticleSystem(프리팹 또는
    /// 씬 인스턴스)을 재생한다. 프리팹 에셋이 할당된 경우 직접 Play 해도 화면에 보이지 않으므로,
    /// 재생 시 인스턴스를 새로 생성한다. 메인 Canvas가 ScreenSpace-Camera 모드일 때 UI 위에
    /// 보이도록, 생성한 인스턴스를 렌더 카메라 앞에 배치한다. 기준 위치는 target 오브젝트의
    /// 로컬 0,0(원점)이며, target이 없으면 화면 중앙이다. 여기에 X/Y 오프셋을 더할 수 있다.
    /// 파티클이 할당되지 않았으면 아무 동작도 하지 않는다.
    /// </summary>
    public class PourEffect : MonoBehaviour
    {
        [Header("Effect")]
        [SerializeField] private ParticleSystem effect;        // 재생할 파티클(프리팹 또는 씬 인스턴스)

        [Header("Placement")]
        [SerializeField] private Camera renderCamera;          // UI를 렌더링하는 카메라(Main Camera)
        [Tooltip("이펙트를 띄울 기준 오브젝트. 설정하면 해당 타겟의 로컬 0,0(원점) 위치에 재생된다. 비워두면 화면 중앙.")]
        [SerializeField] private Transform target;             // 기준 오브젝트(없으면 화면 중앙)
        [Tooltip("기준 위치에서의 X/Y 오프셋(카메라 기준: X=오른쪽, Y=위). 월드 유닛.")]
        [SerializeField] private Vector2 offset = Vector2.zero; // 기준 위치에서의 X/Y 오프셋
        [SerializeField] private float cameraDistance = 1f;    // 카메라 앞 배치 거리(UI 평면보다 가깝게)
        [SerializeField] private float effectScale = 1f;       // 인스턴스에 적용할 스케일

        private GameObject _instance;

        /// <summary>할당된 파티클이 있으면 인스턴스를 생성해 처음부터 재생한다.</summary>
        public void Play()
        {
            if (effect == null)
            {
                return;
            }

            StopAndClear();

            _instance = Instantiate(effect.gameObject);
            _instance.name = effect.name + "_FX";

            var cam = renderCamera != null ? renderCamera : Camera.main;
            if (cam != null)
            {
                // 기본은 화면 중앙(카메라 로컬 0,0). 타겟이 있으면 타겟 위치를 카메라 평면에 투영.
                _instance.transform.SetParent(cam.transform, false);

                float dist = Mathf.Max(0.01f, cameraDistance);
                Vector3 localPos = new Vector3(0f, 0f, dist);

                if (target != null)
                {
                    // 타겟의 원점(로컬 0,0)을 스크린으로 변환 → 카메라 앞 dist 평면의 월드 좌표로 환원 → 카메라 로컬로 변환.
                    Vector3 screen = cam.WorldToScreenPoint(target.position);
                    Vector3 world = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, dist));
                    localPos = cam.transform.InverseTransformPoint(world);
                    localPos.z = dist;
                }

                // 기준 위치에 X/Y 오프셋 적용(카메라 기준: X=오른쪽, Y=위).
                localPos.x += offset.x;
                localPos.y += offset.y;

                _instance.transform.localPosition = localPos;
                _instance.transform.localRotation = Quaternion.identity;
            }
            else
            {
                Vector3 basePos = target != null ? target.position : transform.position;
                _instance.transform.position = basePos + new Vector3(offset.x, offset.y, 0f);
                _instance.transform.rotation = Quaternion.identity;
            }

            _instance.transform.localScale = Vector3.one * Mathf.Max(0.0001f, effectScale);

            foreach (var ps in _instance.GetComponentsInChildren<ParticleSystem>(true))
            {
                ps.Clear(true);
                ps.Play(true);
            }
        }

        /// <summary>생성된 인스턴스를 즉시 제거한다.</summary>
        public void StopAndClear()
        {
            if (_instance != null)
            {
                Destroy(_instance);
                _instance = null;
            }
        }
    }
}
