using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GemCafe.Crafting
{
    /// <summary>
    /// 재료 네임태그 범위. 이 범위(영역)에 마우스 커서가 들어오면 해당 재료의
    /// DisplayName을 하단 라벨에 표시하고, 범위를 벗어나면 다시 숨긴다.
    /// 단, 어떤 재료라도 픽업(드래그) 중이면 표시하지 않는다.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class IngredientNameTag : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private DraggableIngredient ingredient;
        [SerializeField] private Text label;

        private bool _pointerInside;

        private void Awake()
        {
            Apply();
        }

        private void OnEnable()
        {
            Apply();
        }

        private void OnDisable()
        {
            _pointerInside = false;
            Apply();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _pointerInside = true;
            Apply();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _pointerInside = false;
            Apply();
        }

        private void Update()
        {
            // 커서가 범위 안에 있는 동안에는 픽업 상태 변화에 맞춰 매 프레임 갱신한다.
            if (_pointerInside)
            {
                Apply();
            }
        }

        private void Apply()
        {
            if (label == null)
            {
                return;
            }

            var name = ingredient != null && ingredient.Ingredient != null
                ? ingredient.Ingredient.displayName
                : string.Empty;

            var show = _pointerInside && !DraggableIngredient.IsAnyDragging && !string.IsNullOrEmpty(name);

            label.text = show ? name : string.Empty;
            label.enabled = show;
        }
    }
}
