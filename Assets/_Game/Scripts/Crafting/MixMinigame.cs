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

            // 모바일 첫 프레임 hitch에서 Time.deltaTime이 급증하면 한 프레임에 성공/실패가 확정될 수 있다.
            // 물리적으로 가능한 입력 반응 범위로 제한해 시작 직후 즉시 종료를 방지한다.
            var dt = Mathf.Min(Time.deltaTime, 0.05f);
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
            var isHolding = holdArea != null ? holdArea.IsHolding : IsTouchingByAnyInput();

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
            // fallback: HoldInputArea 참조가 없는 경우에만 직접 입력 상태를 읽는다.
            return Input.GetMouseButton(0) || Input.touchCount > 0;
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
            // UI는 즉시 숨기지 않는다. MixFocusController가 마무리 연출에서 페이드 아웃한다.

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
