using GemCafe.Core;
using UnityEngine;

namespace GemCafe.Player
{
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float fallbackLerp = 2f;
        [SerializeField] private bool followX = true;
        [SerializeField] private bool followY;
        [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);

        [Header("Bounds")]
        [Tooltip("카메라 시야가 ±값 범위를 벗어나지 못하도록 제한한다. 주인공 이동 한계와 동일하게 맞춘다.")]
        [SerializeField] private bool clampToBounds = true;

        [Tooltip("카메라 시야 가장자리가 넘어갈 수 없는 X 경계(±값).")]
        [SerializeField] private float horizontalLimit = 25f;

        [SerializeField] private Camera targetCamera;

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            var current = transform.position;
            var desired = current;

            if (followX)
            {
                desired.x = target.position.x + offset.x;
            }

            if (followY)
            {
                desired.y = target.position.y + offset.y;
            }

            desired.z = target.position.z + offset.z;

            if (clampToBounds && horizontalLimit > 0f && followX)
            {
                var cam = targetCamera != null ? targetCamera : Camera.main;
                if (cam != null && cam.orthographic)
                {
                    float halfView = cam.orthographicSize * cam.aspect;
                    float maxCenter = horizontalLimit - halfView;
                    if (maxCenter < 0f)
                    {
                        // 시야가 경계보다 넓으면 중앙에 고정.
                        desired.x = 0f;
                    }
                    else
                    {
                        desired.x = Mathf.Clamp(desired.x, -maxCenter, maxCenter);
                    }
                }
            }

            var lerp = GameManager.Instance != null ? GameManager.Instance.Config.cameraLerp : fallbackLerp;
            transform.position = Vector3.Lerp(current, desired, lerp * Time.deltaTime);
        }

        public void SetTarget(Transform t)
        {
            target = t;
        }
    }
}
