using System;
using System.Collections;
using GemCafe.Core;
using GemCafe.Data;
using UnityEngine;
using UnityEngine.UI;

namespace GemCafe.UI
{
    public class ResultToast : MonoBehaviour
    {
        [SerializeField] private CanvasGroup root;
        [SerializeField] private Text messageText;
        [SerializeField] private Image portraitImage;
        [SerializeField] private GameObject strongLight;
        [SerializeField] private GameObject weakLight;
        [SerializeField] private string greatSuccessMessage = "대성공!";
        [SerializeField] private string successMessage = "성공!";
        [SerializeField] private string failMessage = "실패...";
        [SerializeField] private float showDuration = 1.5f;

        private Coroutine _showRoutine;

        public void ShowResult(bool success, Action onDone = null)
        {
            ShowResult(success ? DrinkResult.Success : DrinkResult.Fail, onDone);
        }

        public void ShowResult(DrinkResult result, Action onDone = null)
        {
            ShowResult(result, null, onDone);
        }

        public void ShowResult(DrinkResult result, CustomerSO customer, Action onDone = null)
        {
            if (_showRoutine != null)
            {
                StopCoroutine(_showRoutine);
                _showRoutine = null;
            }

            AudioManager.Instance?.PlayResult(result);

            if (messageText != null)
            {
                messageText.text = ResolveMessage(result, customer);
            }

            ApplyPortrait(result, customer);
            ApplyLights(result);

            SetVisible(true);
            _showRoutine = StartCoroutine(ShowRoutine(onDone));
        }

        private string ResolveMessage(DrinkResult result, CustomerSO customer)
        {
            if (customer != null)
            {
                if (result == DrinkResult.GreatSuccess && !string.IsNullOrEmpty(customer.greatSuccessLine))
                {
                    return customer.greatSuccessLine;
                }

                if (result == DrinkResult.Success && !string.IsNullOrEmpty(customer.successLine))
                {
                    return customer.successLine;
                }

                if (result == DrinkResult.Fail && !string.IsNullOrEmpty(customer.failLine))
                {
                    return customer.failLine;
                }
            }

            if (result == DrinkResult.GreatSuccess)
            {
                return greatSuccessMessage;
            }

            if (result == DrinkResult.Success)
            {
                return successMessage;
            }

            return failMessage;
        }

        private void ApplyPortrait(DrinkResult result, CustomerSO customer)
        {
            if (portraitImage == null)
            {
                return;
            }

            Sprite sprite = null;
            if (customer != null)
            {
                sprite = result == DrinkResult.Fail ? customer.disappointedPortrait : customer.satisfiedPortrait;
            }

            portraitImage.sprite = sprite;
            portraitImage.enabled = sprite != null;
        }

        private void ApplyLights(DrinkResult result)
        {
            if (strongLight != null)
            {
                strongLight.SetActive(result == DrinkResult.GreatSuccess);
            }

            if (weakLight != null)
            {
                weakLight.SetActive(result == DrinkResult.Success);
            }
        }

        private IEnumerator ShowRoutine(Action onDone)
        {
            var wait = showDuration > 0f ? showDuration : 0f;
            if (wait > 0f)
            {
                yield return new WaitForSeconds(wait);
            }

            SetVisible(false);
            _showRoutine = null;
            onDone?.Invoke();
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
