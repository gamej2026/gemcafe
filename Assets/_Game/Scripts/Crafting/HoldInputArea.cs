using UnityEngine;
using UnityEngine.EventSystems;

namespace GemCafe.Crafting
{
    public class HoldInputArea : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        public bool IsHolding { get; private set; }

        private TouchHoldHint _hint;
        private Vector2 _pointerScreenPos;
        private int _originalSiblingIndex = -1;
        private int _activePointerId = int.MinValue;
        private int _activeTouchId = int.MinValue;

        private void Awake()
        {
            EnsureFullscreenRect();
        }

        private void Update()
        {
            // Pointer 이벤트가 누락되어도 실제 화면 입력 상태를 매 프레임 보정한다.
            if (TryReadCurrentPress(out var currentPos))
            {
                var moved = (currentPos - _pointerScreenPos).sqrMagnitude > 0.01f;
                _pointerScreenPos = currentPos;
                if (!IsHolding)
                {
                    IsHolding = true;
                }

                if (moved || _hint != null)
                {
                    _hint?.SetPress(true, _pointerScreenPos);
                }

                return;
            }

            if (IsHolding)
            {
                IsHolding = false;
                _activePointerId = int.MinValue;
                _activeTouchId = int.MinValue;
                _hint?.SetPress(false, _pointerScreenPos);
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            IsHolding = true;
            _pointerScreenPos = eventData.position;
            _activePointerId = eventData.pointerId;
            _activeTouchId = int.MinValue;
            _hint?.SetPress(true, _pointerScreenPos);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_activePointerId != int.MinValue && eventData.pointerId != _activePointerId)
            {
                return;
            }

            _pointerScreenPos = eventData.position;
            if (IsHolding)
            {
                _hint?.SetPress(true, _pointerScreenPos);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (_activePointerId != int.MinValue && eventData.pointerId != _activePointerId)
            {
                return;
            }

            IsHolding = false;
            _activePointerId = int.MinValue;
            _activeTouchId = int.MinValue;
            _hint?.SetPress(false, eventData.position);
        }

        private void OnDisable()
        {
            IsHolding = false;
            _activePointerId = int.MinValue;
            _activeTouchId = int.MinValue;
            _hint?.SetPress(false, _pointerScreenPos);
        }

        public void ShowHint(MinigameTouchPrompt prompt)
        {
            BringToFront();
            EnsureHint().Show(prompt);
        }

        public void HideHint()
        {
            _hint?.Hide();
            RestoreOrder();
        }

        public void SetHintHighlight(bool on, MinigameTouchAccent accent)
        {
            EnsureHint().SetHighlight(on, accent);
        }

        private void BringToFront()
        {
            if (_originalSiblingIndex < 0)
            {
                _originalSiblingIndex = transform.GetSiblingIndex();
            }

            transform.SetAsLastSibling();
        }

        private void RestoreOrder()
        {
            if (_originalSiblingIndex >= 0)
            {
                transform.SetSiblingIndex(_originalSiblingIndex);
                _originalSiblingIndex = -1;
            }
        }

        private TouchHoldHint EnsureHint()
        {
            if (_hint == null)
            {
                _hint = TouchHoldHint.Create(transform as RectTransform);
            }

            return _hint;
        }

        private bool TryReadCurrentPress(out Vector2 screenPos)
        {
            if (Input.touchCount > 0)
            {
                if (TryGetTouchPosition(_activeTouchId, out screenPos, out var touchId))
                {
                    _activeTouchId = touchId;
                    return true;
                }

                if (TryGetTouchPosition(int.MinValue, out screenPos, out touchId))
                {
                    _activeTouchId = touchId;
                    return true;
                }
            }

            if (Input.GetMouseButton(0))
            {
                _activeTouchId = int.MinValue;
                screenPos = Input.mousePosition;
                return true;
            }

            _activeTouchId = int.MinValue;
            screenPos = default;
            return false;
        }

        private static bool TryGetTouchPosition(int preferredTouchId, out Vector2 position, out int touchId)
        {
            for (var i = 0; i < Input.touchCount; i++)
            {
                var touch = Input.GetTouch(i);
                if (touch.phase == TouchPhase.Canceled || touch.phase == TouchPhase.Ended)
                {
                    continue;
                }

                if (preferredTouchId != int.MinValue && touch.fingerId != preferredTouchId)
                {
                    continue;
                }

                position = touch.position;
                touchId = touch.fingerId;
                return true;
            }

            position = default;
            touchId = int.MinValue;
            return false;
        }

        private void EnsureFullscreenRect()
        {
            var rect = transform as RectTransform;
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
