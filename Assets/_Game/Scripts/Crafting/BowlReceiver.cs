using System.Collections.Generic;
using GemCafe.Core;
using GemCafe.Data;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GemCafe.Crafting
{
    public class BowlReceiver : MonoBehaviour, IDropHandler
    {
        [SerializeField] private RectTransform bowlRect;
        [SerializeField] private Camera uiCamera;
        [SerializeField] private int maxContents = 3;

        private readonly List<IngredientSO> _contents = new();

        public IReadOnlyList<IngredientSO> Contents => _contents;
        public bool IsLocked { get; private set; }

        public void OnDrop(PointerEventData eventData)
        {
            if (IsLocked || eventData == null)
            {
                return;
            }

            var dragObject = eventData.pointerDrag;
            if (dragObject == null)
            {
                return;
            }

            if (!dragObject.TryGetComponent<DraggableIngredient>(out var draggable))
            {
                return;
            }

            if (bowlRect == null)
            {
                return;
            }

            if (!RectTransformUtility.RectangleContainsScreenPoint(bowlRect, eventData.position, ResolveEventCamera()))
            {
                return;
            }

            if (_contents.Count >= maxContents)
            {
                return;
            }

            Add(draggable.Ingredient);
            draggable.Settle();
        }

        public void Add(IngredientSO ingredient)
        {
            if (ingredient == null)
            {
                return;
            }

            _contents.Add(ingredient);
            EventBus.RaiseIngredientAdded(ingredient);
        }

        public void Lock()
        {
            IsLocked = true;
        }

        public void Clear()
        {
            _contents.Clear();
            IsLocked = false;
        }

        private Camera ResolveEventCamera()
        {
            var parentCanvas = bowlRect != null ? bowlRect.GetComponentInParent<Canvas>() : null;
            if (parentCanvas != null && parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                return null;
            }

            return uiCamera;
        }
    }
}
