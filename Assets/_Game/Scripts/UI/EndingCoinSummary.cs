using System;
using System.Collections.Generic;
using GemCafe.Core;
using UnityEngine;
using UnityEngine.UI;

namespace GemCafe.UI
{
    public class EndingCoinSummary : MonoBehaviour
    {
        [SerializeField] private Sprite goldCoin;
        [SerializeField] private Sprite silverCoin;
        [SerializeField] private CanvasGroup root;
        [SerializeField] private Image[] coinSlots;
        [SerializeField] private GameObject[] greatBadges;
        [SerializeField] private Text messageText;
        [SerializeField] private Button nextButton;
        [SerializeField] private string message = "3일간 모은 돈... 마님과 인사를 나누자";

        private Action _onNext;

        public void Show(IReadOnlyList<CoinType> coinsByDay, Action onNext)
        {
            _onNext = onNext;

            if (coinSlots != null)
            {
                for (int i = 0; i < coinSlots.Length; i++)
                {
                    if (coinSlots[i] != null)
                    {
                        var hasCoin = coinsByDay != null && i < coinsByDay.Count;
                        var isVisible = hasCoin;
                        coinSlots[i].enabled = isVisible;
                        if (isVisible)
                        {
                            coinSlots[i].sprite = coinsByDay[i] == CoinType.Gold ? goldCoin : silverCoin;
                        }
                    }
                }
            }

            if (greatBadges != null)
            {
                for (int i = 0; i < greatBadges.Length; i++)
                {
                    if (greatBadges[i] != null)
                    {
                        var isGreatDay = coinsByDay != null && i < coinsByDay.Count && coinsByDay[i] == CoinType.Gold;
                        greatBadges[i].SetActive(isGreatDay);
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
