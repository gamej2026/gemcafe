using System;
using System.Collections;
using UnityEngine;

namespace GemCafe.UI
{
    public class ScreenTransition : MonoBehaviour
    {
        [SerializeField] private RectTransform panel;
        [SerializeField] private Vector2 offscreenRight;
        [SerializeField] private Vector2 onscreen;
        [SerializeField] private float duration = 0.4f;

        private Coroutine _slideRoutine;

        public void SlideIn(Action onComplete = null)
        {
            StartSlide(offscreenRight, onscreen, onComplete);
        }

        public void SlideOut(Action onComplete = null)
        {
            StartSlide(onscreen, offscreenRight, onComplete);
        }

        private void StartSlide(Vector2 from, Vector2 to, Action onComplete)
        {
            if (_slideRoutine != null)
            {
                StopCoroutine(_slideRoutine);
                _slideRoutine = null;
            }

            _slideRoutine = StartCoroutine(SlideRoutine(from, to, onComplete));
        }

        private IEnumerator SlideRoutine(Vector2 from, Vector2 to, Action onComplete)
        {
            if (panel == null)
            {
                _slideRoutine = null;
                onComplete?.Invoke();
                yield break;
            }

            panel.anchoredPosition = from;

            var time = duration > 0f ? duration : 0f;
            if (time <= 0f)
            {
                panel.anchoredPosition = to;
                _slideRoutine = null;
                onComplete?.Invoke();
                yield break;
            }

            var elapsed = 0f;
            while (elapsed < time)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / time);
                panel.anchoredPosition = Vector2.Lerp(from, to, t);
                yield return null;
            }

            panel.anchoredPosition = to;
            _slideRoutine = null;
            onComplete?.Invoke();
        }
    }
}
