using System;
using GemCafe.Data;
using UnityEngine;
using UnityEngine.UI;

namespace GemCafe.UI
{
    public class CoinGainScreen : MonoBehaviour
    {
        [SerializeField] private CanvasGroup root;
        [SerializeField] private Image coinImage;
        [SerializeField] private Text messageText;
        [SerializeField] private Button nextButton;
        [SerializeField] private string greatTemplate = "최고의 코인을 얻었다.";
        [SerializeField] private string successTemplate = "코인을 얻었다.";
        [SerializeField] private string failTemplate = "이번엔 코인을 얻지 못했다.";

        private Action _onNext;

        public void Show(DrinkResult result, Action onNext = null)
        {
            _onNext = onNext;

            if (messageText != null)
            {
                messageText.text = ResolveText(result);
            }

            if (coinImage != null)
            {
                coinImage.enabled = result != DrinkResult.Fail;
            }

            if (nextButton != null)
            {
                nextButton.onClick.RemoveListener(HandleNext);
                nextButton.onClick.AddListener(HandleNext);
            }

            SetVisible(true);
        }

        private string ResolveText(DrinkResult result)
        {
            if (result == DrinkResult.GreatSuccess)
            {
                return greatTemplate;
            }

            if (result == DrinkResult.Success)
            {
                return successTemplate;
            }

            return failTemplate;
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
