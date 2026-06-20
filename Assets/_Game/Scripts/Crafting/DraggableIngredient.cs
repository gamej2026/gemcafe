using GemCafe.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GemCafe.Crafting
{
    public class DraggableIngredient : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private IngredientSO ingredient;
        [SerializeField] private Canvas canvas;
        [SerializeField] private Image iconImage;

        private Vector2 _originAnchored;
        private Transform _originParent;
        private CanvasGroup _cg;
        private bool _droppedIntoBowl;
        private bool _settled;
        private RectTransform _rectTransform;

        public IngredientSO Ingredient => ingredient;

        private void Awake()
        {
            _rectTransform = transform as RectTransform;
            _originParent = transform.parent;
            if (_rectTransform != null)
            {
                _originAnchored = _rectTransform.anchoredPosition;
            }

            _cg = GetComponent<CanvasGroup>();
            if (_cg == null)
            {
                _cg = gameObject.AddComponent<CanvasGroup>();
            }

            ApplyIcon();
            SetIconAlpha(0f);
        }

        private void OnEnable()
        {
            ApplyIcon();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_settled)
            {
                return;
            }
            SetIconAlpha(1f);

            _droppedIntoBowl = false;
            _originParent = transform.parent;

            if (_rectTransform != null)
            {
                _originAnchored = _rectTransform.anchoredPosition;
            }

            if (_cg != null)
            {
                _cg.blocksRaycasts = false;
            }

            transform.SetAsLastSibling();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_settled || _rectTransform == null || canvas == null)
            {
                return;
            }

            var scale = canvas.scaleFactor;
            if (scale <= 0f)
            {
                scale = 1f;
            }

            _rectTransform.anchoredPosition += eventData.delta / scale;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_settled)
            {
                return;
            }

            if (_cg != null)
            {
                _cg.blocksRaycasts = true;
            }

            if (!_droppedIntoBowl)
            {
                ReturnToOrigin();
            }
        }

        public void Settle()
        {
            _droppedIntoBowl = true;
            _settled = true;

            if (_cg != null)
            {
                _cg.blocksRaycasts = false;
            }
        }

        public void ReturnToOrigin()
        {
            _settled = false;
            _droppedIntoBowl = false;

            if (_cg != null)
            {
                _cg.blocksRaycasts = true;
            }

            if (_originParent != null)
            {
                transform.SetParent(_originParent, false);
            }

            if (_rectTransform != null)
            {
                _rectTransform.anchoredPosition = _originAnchored;
            }

            SetIconAlpha(0f);
        }

        private void SetIconAlpha(float alpha)
        {
            if (iconImage == null)
            {
                return;
            }

            Color clr = iconImage.color;
            iconImage.color = new Color(clr.r, clr.g, clr.b, alpha);
        }

        private void ApplyIcon()
        {
            if (iconImage != null && ingredient != null && ingredient.icon != null)
            {
                iconImage.sprite = ingredient.icon;
            }
        }
    }
}
