using System.Text;
using GemCafe.Core;
using GemCafe.Data;
using UnityEngine;
using UnityEngine.UI;

namespace GemCafe.UI
{
    /// <summary>
    /// 직전 손님이 재료/주문에 대해 말한 대사를 다시 볼 수 있는 토글 팝업.
    /// 버튼을 한 번 누르면 보이고, 다시 누르면 닫힌다.
    /// </summary>
    public class OrderRecallPopup : MonoBehaviour
    {
        [SerializeField] private CanvasGroup root;
        [SerializeField] private Button toggleButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Image dim;
        [SerializeField] private Text contentText;
        [SerializeField] private string emptyMessage = "아직 손님의 주문이 없습니다.";

        public bool IsOpen { get; private set; }

        private string _orderText = string.Empty;

        private void Awake()
        {
            if (toggleButton != null)
            {
                toggleButton.onClick.AddListener(Toggle);
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Close);
            }

            Close();
        }

        private void OnEnable()
        {
            EventBus.OnCustomerArrived += HandleCustomerArrived;
        }

        private void OnDisable()
        {
            EventBus.OnCustomerArrived -= HandleCustomerArrived;
        }

        private void OnDestroy()
        {
            if (toggleButton != null)
            {
                toggleButton.onClick.RemoveListener(Toggle);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Close);
            }
        }

        public void Toggle()
        {
            if (IsOpen)
            {
                Close();
            }
            else
            {
                Open();
            }
        }

        public void Open()
        {
            if (contentText != null)
            {
                contentText.text = string.IsNullOrEmpty(_orderText) ? emptyMessage : _orderText;
            }

            SetVisible(true);
            IsOpen = true;
        }

        public void Close()
        {
            SetVisible(false);
            IsOpen = false;
        }

        private void HandleCustomerArrived(CustomerSO customer)
        {
            _orderText = BuildOrderText(customer);

            if (IsOpen && contentText != null)
            {
                contentText.text = string.IsNullOrEmpty(_orderText) ? emptyMessage : _orderText;
            }
        }

        private void SetVisible(bool visible)
        {
            if (root != null)
            {
                root.alpha = visible ? 1f : 0f;
                root.interactable = visible;
                root.blocksRaycasts = visible;
            }

            if (dim != null)
            {
                dim.enabled = visible;
            }
        }

        private static string BuildOrderText(CustomerSO customer)
        {
            if (customer == null || customer.orderDialogue == null || customer.orderDialogue.Length == 0)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            for (int i = 0; i < customer.orderDialogue.Length; i++)
            {
                var line = customer.orderDialogue[i];
                if (string.IsNullOrEmpty(line.text))
                {
                    continue;
                }

                if (sb.Length > 0)
                {
                    sb.Append('\n');
                }

                if (!string.IsNullOrEmpty(line.speakerId))
                {
                    sb.Append(line.speakerId).Append(": ");
                }

                sb.Append(line.text);
            }

            return sb.ToString();
        }
    }
}
