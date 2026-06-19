using System;
using GemCafe.Core;
using UnityEngine;

namespace GemCafe.Player
{
    public class Interactor : MonoBehaviour
    {
        [SerializeField] private float fallbackRadius = 1.5f;
        [SerializeField] private LayerMask interactableMask = ~0;
        [SerializeField] private GameObject keyPromptUI;

        private Interactable _current;

        public event Action<Interactable> OnInteract;

        private void Update()
        {
            var radius = GameManager.Instance != null ? GameManager.Instance.Config.interactRadius : fallbackRadius;
            var next = FindClosestInteractable(radius);

            if (next != _current)
            {
                if (_current != null)
                {
                    _current.SetHighlight(false);
                }

                _current = next;

                if (_current != null)
                {
                    _current.SetHighlight(true);
                }
            }

            if (keyPromptUI != null)
            {
                keyPromptUI.SetActive(_current != null);
            }

            if (_current != null && Input.GetKeyDown(KeyCode.F))
            {
                OnInteract?.Invoke(_current);
            }
        }

        private Interactable FindClosestInteractable(float radius)
        {
            var hits = Physics2D.OverlapCircleAll(transform.position, radius, interactableMask);
            Interactable closest = null;
            var closestSqrDistance = float.PositiveInfinity;
            var center = transform.position;

            for (var i = 0; i < hits.Length; i++)
            {
                var hit = hits[i];
                if (hit == null)
                {
                    continue;
                }

                var interactable = hit.GetComponent<Interactable>();
                if (interactable == null)
                {
                    continue;
                }

                var delta = hit.transform.position - center;
                var sqrDistance = delta.sqrMagnitude;
                if (sqrDistance < closestSqrDistance)
                {
                    closestSqrDistance = sqrDistance;
                    closest = interactable;
                }
            }

            return closest;
        }

        private void OnDisable()
        {
            if (_current != null)
            {
                _current.SetHighlight(false);
                _current = null;
            }

            if (keyPromptUI != null)
            {
                keyPromptUI.SetActive(false);
            }
        }

        private void OnDrawGizmosSelected()
        {
            var radius = GameManager.Instance != null ? GameManager.Instance.Config.interactRadius : fallbackRadius;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
