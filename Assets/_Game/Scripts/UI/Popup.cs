using UnityEngine;

namespace GemCafe.UI
{
    public class Popup : MonoBehaviour
    {
        [SerializeField] private PopupType type;
        [SerializeField] private CanvasGroup root;
        [SerializeField] private UnityEngine.UI.Button closeButton;
        [SerializeField] private UnityEngine.UI.Image dim;

        public PopupType Type => type;
        public bool IsOpen { get; private set; }

        private void Awake()
        {
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Close);
            }

            Close();
        }

        private void OnDestroy()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Close);
            }
        }

        public void Open()
        {
            if (root != null)
            {
                root.alpha = 1f;
                root.interactable = true;
                root.blocksRaycasts = true;
            }

            if (dim != null)
            {
                dim.enabled = true;
            }

            IsOpen = true;
        }

        public void Close()
        {
            if (root != null)
            {
                root.alpha = 0f;
                root.interactable = false;
                root.blocksRaycasts = false;
            }

            if (dim != null)
            {
                dim.enabled = false;
            }

            IsOpen = false;
        }
    }
}
