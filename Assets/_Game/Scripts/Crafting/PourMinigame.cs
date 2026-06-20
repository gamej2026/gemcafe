using System;
using GemCafe.Data;
using UnityEngine;
using UnityEngine.UI;

namespace GemCafe.Crafting
{
    public class PourMinigame : MonoBehaviour
    {
        [SerializeField] private PourMinigameConfig config;
        [SerializeField] private CanvasGroup root;
        [SerializeField] private Image fillImage;
        [SerializeField] private RectTransform targetBandRect;
        [SerializeField] private RectTransform teapotRect;
        [SerializeField] private HoldInputArea holdArea;

        private Action _onSuccess;
        private Action _onFail;
        private float _level;
        private float _releaseTimer;
        private bool _wasHolding;
        private bool _isConfirming;

        public bool IsRunning { get; private set; }

        private void Update()
        {
            if (!IsRunning)
            {
                return;
            }

            var dt = Time.deltaTime;
            var isHolding = holdArea != null && holdArea.IsHolding;

            if (isHolding)
            {
                _isConfirming = false;
                _level += config.fillSpeed * dt;
            }

            if (fillImage != null)
            {
                fillImage.fillAmount = Mathf.Clamp01(_level);
            }

            if (config.overflowFail)
            {
                if (_level > 1f)
                {
                    Finish(false);
                    return;
                }
            }

            if (_wasHolding && !isHolding)
            {
                _isConfirming = true;
                _releaseTimer = 0f;
            }

            if (_isConfirming && !isHolding)
            {
                _releaseTimer += dt;
                var delay = Mathf.Max(0f, config.confirmDelay);
                if (_releaseTimer >= delay)
                {
                    EvaluateCurrentLevel();
                    return;
                }
            }

            _wasHolding = isHolding;
        }

        public void Begin(Action onSuccess, Action onFail)
        {
            _onSuccess = onSuccess;
            _onFail = onFail;

            if (config == null)
            {
                IsRunning = false;
                SetVisible(false);
                var failCb = _onFail;
                _onSuccess = null;
                _onFail = null;
                failCb?.Invoke();
                return;
            }

            _level = 0f;
            _releaseTimer = 0f;
            _isConfirming = false;
            _wasHolding = holdArea != null && holdArea.IsHolding;

            if (fillImage != null)
            {
                fillImage.fillAmount = 0f;
            }

            LayoutTargetBand();
            SetVisible(true);
            IsRunning = true;
        }

        public void Cancel()
        {
            IsRunning = false;
            _onSuccess = null;
            _onFail = null;
            SetVisible(false);
        }

        private void EvaluateCurrentLevel()
        {
            var min = config.targetMin;
            var max = config.targetMax;
            var clampedLevel = Mathf.Clamp01(_level);
            var success = clampedLevel >= min && clampedLevel <= max;
            Finish(success);
        }

        private void Finish(bool success)
        {
            if (!IsRunning)
            {
                return;
            }

            IsRunning = false;
            SetVisible(false);

            var successCb = _onSuccess;
            var failCb = _onFail;
            _onSuccess = null;
            _onFail = null;

            if (success)
            {
                successCb?.Invoke();
                return;
            }

            failCb?.Invoke();
        }

        private void LayoutTargetBand()
        {
            if (targetBandRect == null)
            {
                return;
            }

            var min = Mathf.Clamp01(config.targetMin);
            var max = Mathf.Clamp01(config.targetMax);
            if (max < min)
            {
                var swap = min;
                min = max;
                max = swap;
            }

            var parentHeight = 0f;
            var parentRect = targetBandRect.parent as RectTransform;
            if (parentRect != null)
            {
                parentHeight = parentRect.rect.height;
            }

            var size = targetBandRect.sizeDelta;
            size.y = parentHeight * (max - min);
            targetBandRect.sizeDelta = size;

            var pos = targetBandRect.anchoredPosition;
            pos.y = parentHeight * (min + max - 1f) * 0.5f;
            targetBandRect.anchoredPosition = pos;
        }

        private void SetVisible(bool visible)
        {
            if (root == null)
            {
                return;
            }

            root.alpha = visible ? 1f : 0f;
            root.interactable = visible;
            root.blocksRaycasts = visible;
        }
    }
}
