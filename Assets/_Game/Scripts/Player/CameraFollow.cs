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

            var lerp = GameManager.Instance != null ? GameManager.Instance.Config.cameraLerp : fallbackLerp;
            transform.position = Vector3.Lerp(current, desired, lerp * Time.deltaTime);
        }

        public void SetTarget(Transform t)
        {
            target = t;
        }
    }
}
