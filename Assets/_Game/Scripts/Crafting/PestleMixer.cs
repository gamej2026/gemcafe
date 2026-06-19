using GemCafe.Core;
using GemCafe.Data;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GemCafe.Crafting
{
    public class PestleMixer : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private BowlReceiver bowl;
        [SerializeField] private RectTransform bowlRect;
        [SerializeField] private Camera uiCamera;
        [SerializeField] private RectTransform pestleRect;
        [SerializeField] private RecipeSO _targetRecipe;

        private Vector2 _origin;

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (pestleRect != null)
            {
                _origin = pestleRect.anchoredPosition;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (pestleRect == null || eventData == null)
            {
                return;
            }

            pestleRect.anchoredPosition += eventData.delta;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (pestleRect == null || bowlRect == null || eventData == null)
            {
                return;
            }

            if (RectTransformUtility.RectangleContainsScreenPoint(bowlRect, eventData.position, ResolveEventCamera()))
            {
                Mix();
                return;
            }

            pestleRect.anchoredPosition = _origin;
        }

        public void Mix()
        {
            if (bowl == null || bowl.IsLocked)
            {
                if (pestleRect != null)
                {
                    pestleRect.anchoredPosition = _origin;
                }

                return;
            }

            bowl.Lock();
            var ok = RecipeEvaluator.Matches(bowl.Contents, _targetRecipe);
            EventBus.RaiseDrinkCompleted(ok ? _targetRecipe : null);

            if (pestleRect != null)
            {
                pestleRect.anchoredPosition = _origin;
            }
        }

        public void SetTargetRecipe(RecipeSO target)
        {
            _targetRecipe = target;
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
