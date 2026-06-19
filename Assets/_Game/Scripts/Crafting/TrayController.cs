using System.Collections;
using GemCafe.Core;
using UnityEngine;

namespace GemCafe.Crafting
{
    public class TrayController : MonoBehaviour
    {
        [SerializeField] private RectTransform panel;
        [SerializeField] private Vector2 openAnchoredPos;
        [SerializeField] private Vector2 closedAnchoredPos;
        [SerializeField] private float fallbackDuration = 0.3f;

        private Coroutine _slideRoutine;

        public bool IsOpen { get; private set; }

        public void Open()
        {
            IsOpen = true;
            StartSlide(openAnchoredPos);
        }

        public void Close()
        {
            IsOpen = false;
            StartSlide(closedAnchoredPos);
        }

        public void Toggle()
        {
            if (IsOpen)
            {
                Close();
                return;
            }

            Open();
        }

        public void ResetIngredients()
        {
            if (panel == null)
            {
                return;
            }

            var items = panel.GetComponentsInChildren<DraggableIngredient>(true);
            foreach (var it in items)
            {
                it.gameObject.SetActive(true);
                it.ReturnToOrigin();
            }
        }

        private void StartSlide(Vector2 target)
        {
            if (panel == null)
            {
                return;
            }

            if (_slideRoutine != null)
            {
                StopCoroutine(_slideRoutine);
            }

            _slideRoutine = StartCoroutine(SlideRoutine(target, GetSlideDuration()));
        }

        private float GetSlideDuration()
        {
            var manager = GameManager.Instance;
            if (manager != null && manager.Config != null)
            {
                return manager.Config.traySlideDuration;
            }

            return fallbackDuration;
        }

        private IEnumerator SlideRoutine(Vector2 target, float duration)
        {
            var start = panel.anchoredPosition;
            if (duration <= 0f)
            {
                panel.anchoredPosition = target;
                _slideRoutine = null;
                yield break;
            }

            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                panel.anchoredPosition = Vector2.Lerp(start, target, t);
                yield return null;
            }

            panel.anchoredPosition = target;
            _slideRoutine = null;
        }
    }
}
