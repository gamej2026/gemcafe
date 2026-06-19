using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace GemCafe.UI
{
    public class ResultToast : MonoBehaviour
    {
        [SerializeField] private CanvasGroup root;
        [SerializeField] private Text messageText;
        [SerializeField] private string successMessage = "성공!";
        [SerializeField] private string failMessage = "실패...";
        [SerializeField] private float showDuration = 1.5f;

        private Coroutine _showRoutine;

        public void ShowResult(bool success, Action onDone = null)
        {
            if (_showRoutine != null)
            {
                StopCoroutine(_showRoutine);
                _showRoutine = null;
            }

            if (messageText != null)
            {
                messageText.text = success ? successMessage : failMessage;
            }

            SetVisible(true);
            _showRoutine = StartCoroutine(ShowRoutine(onDone));
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
