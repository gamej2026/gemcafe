using UnityEngine;
using UnityEngine.EventSystems;

namespace GemCafe.Crafting
{
    public class HoldInputArea : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public bool IsHolding { get; private set; }

        public void OnPointerDown(PointerEventData eventData)
        {
            IsHolding = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            IsHolding = false;
        }

        private void OnDisable()
        {
            IsHolding = false;
        }
    }
}
