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
        }

        /// <summary>
        /// 미니게임을 시작하지 않고 UI 비주얼만 초기 상태로 정렬한다.
        /// 포커스 연출에서 미리보기로 UI를 보여줄 때 사용한다. (alpha는 건드리지 않음)
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
        }

        private void UpdateBar(float dt)
        {
            var riseAccel = config.barRiseAccel;
            var gravity = config.barGravity;
            var maxSpeed = Mathf.Abs(config.barMaxSpeed);
            var isHolding = holdArea != null && holdArea.IsHolding;

            _barVelocity += (isHolding ? riseAccel : -gravity) * dt;
            _barVelocity = Mathf.Clamp(_barVelocity, -maxSpeed, maxSpeed);

            if (barRect == null)
            {
                return;
            }

            var pos = barRect.anchoredPosition;
            pos.y += _barVelocity * dt;
            pos.y = ClampCenterY(pos.y, GetHalfBarHeight());
            barRect.anchoredPosition = pos;
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

            var y = amplitude * Mathf.Sin(2f * Mathf.PI * frequency * _time) + (patternOffset * amplitude);
            var pos = leafRect.anchoredPosition;
            pos.y = ClampCenterY(y, GetHalfLeafHeight());
            leafRect.anchoredPosition = pos;
        }

        private void UpdateProgress(float dt)
        {
            var leafY = leafRect != null ? leafRect.anchoredPosition.y : 0f;
            var barY = barRect != null ? barRect.anchoredPosition.y : 0f;
            var halfBar = GetHalfBarHeight();
            var inside = leafY >= (barY - halfBar) && leafY <= (barY + halfBar);

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
            // UI는 즉시 숨기지 않는다. 포커스 연출(MixFocusController)이 천천히 페이드 아웃한다.

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
            pos.y = ClampCenterY(0f, GetHalfBarHeight());
            barRect.anchoredPosition = pos;
        }

        private float ClampCenterY(float y, float halfHeight)
        {
            var limit = GetTrackHalfHeight() - halfHeight;
            if (limit < 0f)
            {
                return 0f;
            }

            return Mathf.Clamp(y, -limit, limit);
        }

        private float GetTrackHalfHeight()
        {
            if (trackRect == null)
            {
                return 0f;
            }

            return trackRect.rect.height * 0.5f;
        }

        private float GetHalfBarHeight()
        {
            if (barRect != null && barRect.rect.height > 0f)
            {
                return barRect.rect.height * 0.5f;
            }

            var h = config != null ? config.barHeight : 0f;
            return Mathf.Max(0f, h * 0.5f);
        }

        private float GetHalfLeafHeight()
        {
            if (leafRect == null)
            {
                return 0f;
            }

            return leafRect.rect.height * 0.5f;
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
