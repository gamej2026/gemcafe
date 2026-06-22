using System;
using GemCafe.Data;
using UnityEngine;
using UnityEngine.UI;

namespace GemCafe.Crafting
{
    public class MixMinigame : MonoBehaviour
    {
        [SerializeField] private MixMinigameConfig config;
        [SerializeField] private CanvasGroup root;
        [SerializeField] private RectTransform trackRect;
        [SerializeField] private RectTransform barRect;
        [SerializeField] private RectTransform leafRect;
        [SerializeField] private Image progressFill;
        [SerializeField] private HoldInputArea holdArea;

        private Action _onSuccess;
        private Action _onFail;
        private float _progress;
        private float _barVelocity;
        private float _time;
        private bool _lastTouchState;

        public bool IsRunning { get; private set; }

        private void Update()
        {
            if (!IsRunning)
            {
                return;
            }

            var dt = Time.deltaTime;
            _time += dt;

            UpdateBar(dt);
            UpdateLeaf();
            UpdateProgress(dt);
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

            _progress = Mathf.Clamp01(config.startProgress);
            _barVelocity = 0f;
            _time = 0f;

            CenterBar();
            UpdateLeaf();

            if (progressFill != null)
            {
                progressFill.fillAmount = _progress;
            }

            SetVisible(true);
            IsRunning = true;

            if (holdArea != null)
            {
                holdArea.ShowHint(MinigameTouchPrompt.MixHold);
            }
        }

        /// <summary>
        /// ÎØ∏ÎãàÍ≤åÏûÑ?ùÑ ?ãú?ûë?ïòÏß? ?ïäÍ≥? UI ÎπÑÏ£º?ñºÎß? Ï¥àÍ∏∞ ?ÉÅ?ÉúÎ°? ?†ï?†¨?ïú?ã§.
        /// ?è¨Ïª§Ïä§ ?ó∞Ï∂úÏóê?Ñú ÎØ∏Î¶¨Î≥¥Í∏∞Î°? UIÎ•? Î≥¥Ïó¨Ï§? ?ïå ?Ç¨?ö©?ïú?ã§. (alpha?äî Í±¥ÎìúÎ¶¨Ï?? ?ïä?ùå)
        /// </summary>
        public void PrepareVisuals()
        {
            if (config == null)
            {
                return;
            }

            _progress = Mathf.Clamp01(config.startProgress);
            _barVelocity = 0f;
            _time = 0f;

            CenterBar();
            UpdateLeaf();

            if (progressFill != null)
            {
                progressFill.fillAmount = _progress;
            }
        }

        public void Cancel()
        {
            IsRunning = false;
            _onSuccess = null;
            _onFail = null;
            SetVisible(false);

            if (holdArea != null)
            {
                holdArea.HideHint();
            }
        }

        private void UpdateBar(float dt)
        {
            var riseAccel = config.barRiseAccel;
            var gravity = config.barGravity;
            var maxSpeed = Mathf.Abs(config.barMaxSpeed);
            var isHolding = IsTouchingByAnyInput();

            if (_lastTouchState != isHolding)
            {
                _lastTouchState = isHolding;
                // Debug.Log($"[MixMinigame] Touch state changed: {(isHolding ? "ON" : "OFF")}");
            }

            _barVelocity += (isHolding ? riseAccel : -gravity) * dt;
            _barVelocity = Mathf.Clamp(_barVelocity, -maxSpeed, maxSpeed);

            if (barRect == null)
            {
                return;
            }

            var pos = barRect.anchoredPosition;
            pos.x += _barVelocity * dt;
            pos.x = ClampCenter(pos.x, GetHalfBarExtent());
            barRect.anchoredPosition = pos;
        }

        private static bool IsTouchingByAnyInput()
        {
            // ƒµπˆΩ∫ ¿Ã∫•∆Æ ¿«¡∏ æ¯¿Ã, æÓ∂≤ ¿‘∑¬(≈∞/∏∂øÏΩ∫/≈Õƒ°)¿ÃµÁ ¥©∏£∞Ì ¿÷¿∏∏È ≈Õƒ°∑Œ ∞£¡÷«—¥Ÿ.
            return Input.anyKey || Input.GetMouseButton(0) || Input.touchCount > 0;
        }

        private void UpdateLeaf()
        {
            if (leafRect == null)
            {
                return;
            }

            var amplitude = config.leafAmplitude;
            var frequency = config.leafFrequency;
            var normalized = Mathf.Repeat(_time * frequency, 1f);
            var patternOffset = 0f;

            if (config.leafPattern != null && config.leafPattern.length > 0)
            {
                patternOffset = config.leafPattern.Evaluate(normalized);
            }

            var x = amplitude * Mathf.Sin(2f * Mathf.PI * frequency * _time) + (patternOffset * amplitude);
            var pos = leafRect.anchoredPosition;
            pos.x = ClampCenter(x, GetHalfLeafExtent());
            leafRect.anchoredPosition = pos;
        }

        private void UpdateProgress(float dt)
        {
            var leafX = leafRect != null ? leafRect.anchoredPosition.x : 0f;
            var barX = barRect != null ? barRect.anchoredPosition.x : 0f;
            var halfBar = GetHalfBarExtent();
            var inside = leafX >= (barX - halfBar) && leafX <= (barX + halfBar);

            if (holdArea != null)
            {
                holdArea.SetHintHighlight(inside, MinigameTouchAccent.MixInside);
            }

            var gainRate = config.progressGainRate;
            var lossRate = config.progressLossRate;
            _progress += (inside ? gainRate : -lossRate) * dt;
            _progress = Mathf.Clamp01(_progress);

            if (progressFill != null)
            {
                progressFill.fillAmount = _progress;
            }

            if (_progress >= 1f)
            {
                Finish(true);
                return;
            }

            if (_progress <= 0f)
            {
                Finish(false);
            }
        }

        private void Finish(bool success)
        {
            if (!IsRunning)
            {
                return;
            }

            IsRunning = false;
            // UI?äî Ï¶âÏãú ?à®Í∏∞Ï?? ?ïä?äî?ã§. ?è¨Ïª§Ïä§ ?ó∞Ï∂?(MixFocusController)?ù¥ Ï≤úÏ≤ú?ûà ?éò?ù¥?ìú ?ïÑ?õÉ?ïú?ã§.

            if (holdArea != null)
            {
                holdArea.HideHint();
            }

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

        private void CenterBar()
        {
            if (barRect == null)
            {
                return;
            }

            var pos = barRect.anchoredPosition;
            pos.x = ClampCenter(0f, GetHalfBarExtent());
            barRect.anchoredPosition = pos;
        }

        private float ClampCenter(float value, float halfExtent)
        {
            var limit = GetTrackHalfWidth() - halfExtent;
            if (limit < 0f)
            {
                return 0f;
            }

            return Mathf.Clamp(value, -limit, limit);
        }

        private float GetTrackHalfWidth()
        {
            if (trackRect == null)
            {
                return 0f;
            }

            return trackRect.rect.width * 0.5f;
        }

        private float GetHalfBarExtent()
        {
            if (barRect != null && barRect.rect.width > 0f)
            {
                return barRect.rect.width * 0.5f;
            }

            var w = config != null ? config.barHeight : 0f;
            return Mathf.Max(0f, w * 0.5f);
        }

        private float GetHalfLeafExtent()
        {
            if (leafRect == null)
            {
                return 0f;
            }

            return leafRect.rect.width * 0.5f;
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
