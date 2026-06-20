using System;
using System.Collections;
using UnityEngine;

namespace GemCafe.UI
{
    public class ScreenTransition : MonoBehaviour
    {
        [SerializeField] private RectTransform panel;
        [SerializeField] private CanvasGroup fadeGroup;
        [SerializeField] private Vector2 offscreenRight;
        [SerializeField] private Vector2 onscreen;
        [SerializeField] private float duration = 0.4f;
        [SerializeField] private float fadeDuration = 0.4f;

        private Coroutine _slideRoutine;
        private Coroutine _fadeRoutine;

        public void SlideIn(Action onComplete = null)
        {
            StartSlide(offscreenRight, onscreen, onComplete);
        }

        public void SlideOut(Action onComplete = null)
        {
            StartSlide(onscreen, offscreenRight, onComplete);
        }

        public void FadeOut(Action onComplete = null)
        {
            StartFade(0f, 1f, true, onComplete);
        }

        public void FadeIn(Action onComplete = null)
        {
            StartFade(1f, 0f, false, onComplete);
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

        private void StartFade(float from, float to, bool blockAtEnd, Action onComplete)
        {
            if (_fadeRoutine != null)
            {
                StopCoroutine(_fadeRoutine);
                _fadeRoutine = null;
            }

            _fadeRoutine = StartCoroutine(FadeRoutine(from, to, blockAtEnd, onComplete));
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

        private IEnumerator FadeRoutine(float from, float to, bool blockAtEnd, Action onComplete)
        {
            if (fadeGroup == null)
            {
                _fadeRoutine = null;
                onComplete?.Invoke();
                yield break;
            }

            fadeGroup.blocksRaycasts = true;
            fadeGroup.alpha = from;

            var time = fadeDuration > 0f ? fadeDuration : 0f;
            if (time <= 0f)
            {
                fadeGroup.alpha = to;
            }
            else
            {
                var elapsed = 0f;
                while (elapsed < time)
                {
                    elapsed += Time.deltaTime;
                    var t = Mathf.Clamp01(elapsed / time);
                    fadeGroup.alpha = Mathf.Lerp(from, to, t);
                    yield return null;
                }

                fadeGroup.alpha = to;
            }

            fadeGroup.blocksRaycasts = blockAtEnd;
            _fadeRoutine = null;
            onComplete?.Invoke();
        }
    }
}
