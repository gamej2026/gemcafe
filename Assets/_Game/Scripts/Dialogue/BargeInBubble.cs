using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace GemCafe.Dialogue
{
    public class BargeInBubble : MonoBehaviour
    {
        [SerializeField] private CanvasGroup bubbleRoot;
        [SerializeField] private Text bubbleText;
        [SerializeField] private Image dim;
        [SerializeField] private float defaultDuration = 1.5f;

        private Coroutine _showCoroutine;

        public void Show(string text, float duration = -1f, Action onDone = null)
        {
            if (_showCoroutine != null)
            {
                StopCoroutine(_showCoroutine);
                _showCoroutine = null;
            }

            float actualDuration = duration < 0f ? defaultDuration : duration;

            if (bubbleText != null)
            {
                bubbleText.text = text ?? string.Empty;
            }

            SetBubbleVisible(true);
            SetDimVisible(true);

            _showCoroutine = StartCoroutine(ShowRoutine(actualDuration, onDone));
        }

        public void Hide()
        {
            if (_showCoroutine != null)
            {
                StopCoroutine(_showCoroutine);
                _showCoroutine = null;
            }

            SetBubbleVisible(false);
            SetDimVisible(false);
        }

        private IEnumerator ShowRoutine(float duration, Action onDone)
        {
            if (duration > 0f)
            {
                yield return new WaitForSeconds(duration);
            }

            Hide();
            onDone?.Invoke();
        }

        private void SetBubbleVisible(bool visible)
        {
            if (bubbleRoot == null)
            {
                return;
            }

            bubbleRoot.alpha = visible ? 1f : 0f;
            bubbleRoot.interactable = visible;
            bubbleRoot.blocksRaycasts = visible;
        }

        private void SetDimVisible(bool visible)
        {
            if (dim == null)
            {
                return;
            }

            dim.gameObject.SetActive(visible);
            var color = dim.color;
            color.a = visible ? 1f : 0f;
            dim.color = color;
        }
    }
}
