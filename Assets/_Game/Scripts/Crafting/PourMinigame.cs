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

        [Header("Sprite 교체 방식 (Images/Cup 1..N 순서)")]
        [SerializeField] private Sprite[] cupSprites;

        [Tooltip("각 스프라이트에서 다음 스프라이트로 넘어가기까지의 대기 시간(초). 비어있거나 0 이하면 기본 대기시간을 사용한다.")]
        [SerializeField] private float[] holdDurations;

        [SerializeField] private float defaultHoldDuration = 0.2f;

        [Header("성공 판정 범위 (이미지 번호, 1-based, 양끝 포함)")]
        [SerializeField] private int successMin = 6;
        [SerializeField] private int successMax = 8;

        private Action _onSuccess;
        private Action _onFail;
        private float _holdTime;
        private int _index;
        private bool _hasHeld;
        private bool _wasHolding;

        public bool IsRunning { get; private set; }

        private void Update()
        {
            if (!IsRunning)
            {
                return;
            }

            var isHolding = holdArea != null && holdArea.IsHolding;

            if (isHolding)
            {
                _hasHeld = true;
                _holdTime += Time.deltaTime;
                UpdateSpriteIndex();
            }

            if (holdArea != null)
            {
                var number = _index + 1;
                var lo = Mathf.Min(successMin, successMax);
                var hi = Mathf.Max(successMin, successMax);
                holdArea.SetHintHighlight(isHolding && number >= lo && number <= hi, MinigameTouchAccent.PourRelease);
            }

            // 홀드 후 손을 뗼 때(마우스를 뗼 때) 즉시 판정한다.
            if (_hasHeld && _wasHolding && !isHolding)
            {
                Evaluate();
                return;
            }

            _wasHolding = isHolding;
        }

        public void Begin(Action onSuccess, Action onFail)
        {
            _onSuccess = onSuccess;
            _onFail = onFail;

            _holdTime = 0f;
            _index = -1;
            _hasHeld = false;
            _wasHolding = holdArea != null && holdArea.IsHolding;

            // 기존 타깃 밴드(채우기 방식 UI)는 스프라이트 교체 방식에서는 사용하지 않는다.
            if (targetBandRect != null)
            {
                targetBandRect.gameObject.SetActive(false);
            }

            if (fillImage != null)
            {
                fillImage.type = Image.Type.Simple;
                fillImage.preserveAspect = true;
                fillImage.color = Color.white;
            }

            ApplySprite(0, true);

            SetVisible(true);
            IsRunning = true;

            if (holdArea != null)
            {
                holdArea.ShowHint(MinigameTouchPrompt.PourHold);
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

        /// <summary>
        /// 미니게임을 시작하지 않은 채로 컵(Pour_Fill)을 초기 상태로 보여준다.
        /// 포커스 연출(PourFocusController) 단계에서 Pour_Teapot을 누르기 전에
        /// 컵과 주전자를 보이게 하고 클릭을 받을 수 있도록 한다.
        /// </summary>
        public void PrepareVisuals()
        {
            _holdTime = 0f;
            _index = -1;
            _hasHeld = false;
            _wasHolding = false;

            if (targetBandRect != null)
            {
                targetBandRect.gameObject.SetActive(false);
            }

            if (fillImage != null)
            {
                fillImage.type = Image.Type.Simple;
                fillImage.preserveAspect = true;
                fillImage.color = Color.white;
            }

            ApplySprite(0, true);
            SetVisible(true);
        }

        private void UpdateSpriteIndex()
        {
            if (cupSprites == null || cupSprites.Length == 0)
            {
                return;
            }

            var idx = 0;
            var cumulative = 0f;
            var steps = cupSprites.Length - 1;
            for (var i = 0; i < steps; i++)
            {
                cumulative += GetHoldDuration(i);
                if (_holdTime >= cumulative)
                {
                    idx = i + 1;
                }
                else
                {
                    break;
                }
            }

            ApplySprite(idx, false);
        }

        private float GetHoldDuration(int spriteIndex)
        {
            if (holdDurations != null && spriteIndex < holdDurations.Length && holdDurations[spriteIndex] > 0f)
            {
                return holdDurations[spriteIndex];
            }

            return Mathf.Max(0.0001f, defaultHoldDuration);
        }

        private void ApplySprite(int idx, bool force)
        {
            if (cupSprites == null || cupSprites.Length == 0)
            {
                return;
            }

            idx = Mathf.Clamp(idx, 0, cupSprites.Length - 1);
            if (!force && idx == _index)
            {
                return;
            }

            _index = idx;

            if (fillImage != null)
            {
                fillImage.sprite = cupSprites[idx];
                fillImage.enabled = cupSprites[idx] != null;
            }
        }

        private void Evaluate()
        {
            // 이미지 번호(1-based)가 성공 범위 안이면 성공.
            var number = _index + 1;
            var min = Mathf.Min(successMin, successMax);
            var max = Mathf.Max(successMin, successMax);
            var success = number >= min && number <= max;
            Finish(success);
        }

        private void Finish(bool success)
        {
            if (!IsRunning)
            {
                return;
            }

            IsRunning = false;
            // 화면을 즉시 숨기지 않는다. PourFocusController가 마무리 연출 후
            // 천천히 페이드아웃하도록 둔다.

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
