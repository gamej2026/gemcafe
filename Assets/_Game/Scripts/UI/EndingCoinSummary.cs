using System;
using UnityEngine;
using UnityEngine.UI;

namespace GemCafe.UI
{
    public class EndingCoinSummary : MonoBehaviour
    {
        [SerializeField] private CanvasGroup root;
        [SerializeField] private Image[] coinSlots;
        [SerializeField] private GameObject[] greatBadges;
        [SerializeField] private Text messageText;
        [SerializeField] private Button nextButton;
        [SerializeField] private string message = "3일간 모은 돈... 마님과 인사를 나누자";

        private Action _onNext;

        public void Show(int totalCoins, int greatCoins, Action onNext)
        {
            _onNext = onNext;

            var coinCount = Mathf.Max(0, totalCoins);
            var greatCount = Mathf.Max(0, greatCoins);

            if (coinSlots != null)
            {
                for (int i = 0; i < coinSlots.Length; i++)
                {
                    if (coinSlots[i] != null)
                    {
                        coinSlots[i].enabled = i < coinCount;
                    }
                }
            }

            if (greatBadges != null)
            {
                for (int i = 0; i < greatBadges.Length; i++)
                {
                    if (greatBadges[i] != null)
                    {
                        greatBadges[i].SetActive(i < greatCount);
                    }
                }
            }

            if (messageText != null)
            {
                messageText.text = message;
            }

            if (nextButton != null)
            {
                nextButton.onClick.RemoveListener(HandleNext);
                nextButton.onClick.AddListener(HandleNext);
            }

            SetVisible(true);
        }

        private void HandleNext()
        {
            if (nextButton != null)
            {
                nextButton.onClick.RemoveListener(HandleNext);
            }

            SetVisible(false);
            var cb = _onNext;
            _onNext = null;
            cb?.Invoke();
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
