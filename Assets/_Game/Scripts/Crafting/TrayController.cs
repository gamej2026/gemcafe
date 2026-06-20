using System;
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

        [Header("Reveal (Tray 도착 후 페이드인되는 대상들)")]
        [SerializeField] private CanvasGroup[] revealTargets;
        [SerializeField] private float revealFadeDuration = 0.3f;

        private Coroutine _slideRoutine;
        private Coroutine _revealRoutine;

        public bool IsOpen { get; private set; }

        private void Awake()
        {
            // 씬 시작 시 Tray는 Close 위치, 나머지는 페이드 아웃 + 상호작용 불가
            if (panel != null)
            {
                panel.anchoredPosition = closedAnchoredPos;
            }

            IsOpen = false;
            ApplyRevealAlpha(0f, false);
        }

        public void Open()
        {
            IsOpen = true;
            ApplyRevealAlpha(0f, false);
            StartSlide(openAnchoredPos, FadeInRevealTargets);
        }

        public void Close()
        {
            IsOpen = false;
            // Tray를 닫으면서 나머지 대상도 페이드 아웃 + 즉시 상호작용 차단
            FadeOutRevealTargets();
            StartSlide(closedAnchoredPos, null);
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

        private void StartSlide(Vector2 target, Action onComplete)
        {
            if (panel == null)
            {
                onComplete?.Invoke();
                return;
            }

            if (_slideRoutine != null)
            {
                StopCoroutine(_slideRoutine);
            }

            _slideRoutine = StartCoroutine(SlideRoutine(target, GetSlideDuration(), onComplete));
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

        private IEnumerator SlideRoutine(Vector2 target, float duration, Action onComplete)
        {
            var start = panel.anchoredPosition;
            if (duration <= 0f)
            {
                panel.anchoredPosition = target;
                _slideRoutine = null;
                onComplete?.Invoke();
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
            onComplete?.Invoke();
        }

        private void FadeInRevealTargets()
        {
            if (_revealRoutine != null)
            {
                StopCoroutine(_revealRoutine);
            }

            _revealRoutine = StartCoroutine(FadeInRoutine(revealFadeDuration));
        }

        private void FadeOutRevealTargets()
        {
            if (_revealRoutine != null)
            {
                StopCoroutine(_revealRoutine);
            }

            // 페이드 아웃이 진행되는 동안 상호작용 즉시 차단
            ApplyRevealAlpha(GetCurrentRevealAlpha(), false);
            _revealRoutine = StartCoroutine(FadeOutRoutine(revealFadeDuration));
        }

        private IEnumerator FadeOutRoutine(float duration)
        {
            var startAlpha = GetCurrentRevealAlpha();
            if (duration <= 0f || startAlpha <= 0f)
            {
                ApplyRevealAlpha(0f, false);
                _revealRoutine = null;
                yield break;
            }

            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                ApplyRevealAlpha(Mathf.Lerp(startAlpha, 0f, t), false);
                yield return null;
            }

            ApplyRevealAlpha(0f, false);
            _revealRoutine = null;
        }

        private float GetCurrentRevealAlpha()
        {
            if (revealTargets != null)
            {
                for (int i = 0; i < revealTargets.Length; i++)
                {
                    if (revealTargets[i] != null)
                    {
                        return revealTargets[i].alpha;
                    }
                }
            }

            return 0f;
        }

        private IEnumerator FadeInRoutine(float duration)
        {
            if (duration <= 0f)
            {
                ApplyRevealAlpha(1f, true);
                _revealRoutine = null;
                yield break;
            }

            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var a = Mathf.Clamp01(elapsed / duration);
                ApplyRevealAlpha(a, false);
                yield return null;
            }

            ApplyRevealAlpha(1f, true);
            _revealRoutine = null;
        }

        private void ApplyRevealAlpha(float alpha, bool interactable)
        {
            if (revealTargets == null)
            {
                return;
            }

            for (int i = 0; i < revealTargets.Length; i++)
            {
                var cg = revealTargets[i];
                if (cg == null)
                {
                    continue;
                }

                cg.alpha = alpha;
                cg.interactable = interactable;
                cg.blocksRaycasts = interactable;
            }
        }
    }
}
