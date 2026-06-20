using UnityEngine;
using UnityEngine.EventSystems;

namespace GemCafe.Crafting
{
    public class PestleMixer : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private BowlReceiver bowl;
        [SerializeField] private RectTransform bowlRect;
        [SerializeField] private Camera uiCamera;
        [SerializeField] private RectTransform pestleRect;
        [SerializeField] private CraftingController controller;

        private bool _interactable;

        private void Awake()
        {
            _interactable = true;
        }

        public void SetInteractable(bool value)
        {
            _interactable = value;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!_interactable || controller == null)
            {
                return;
            }

            controller.OnPestleClicked();
        }
    }
}
