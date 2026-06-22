using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace GemCafe.UI
{
    public class DayIntro : MonoBehaviour
    {
        [SerializeField] private CanvasGroup root;
        [SerializeField] private Text dayText;
        [SerializeField] private string format = "{0}일차";
        [SerializeField] private float fadeInDuration = 0.3f;
        [SerializeField] private float holdDuration = 1.0f;
        [SerializeField] private float fadeOutDuration = 1.4f;

        private Coroutine _routine;

        private void Awake()
        {
            if (root != null)
            {
                root.alpha = 0f;
                root.blocksRaycasts = false;
            }
        }

        public void Show(int day, Action onDone)
        {
            if (dayText != null)
            {
                dayText.text = string.Format(format, day);
            }

            if (_routine != null)
            {
                StopCoroutine(_routine);
            }

            _routine = StartCoroutine(PlayRoutine(onDone));
        }

        private IEnumerator PlayRoutine(Action onDone)
        {
            if (root != null)
            {
                root.blocksRaycasts = true;
                yield return Fade(0f, 1f, fadeInDuration);

                if (holdDuration > 0f)
                {
                    yield return new WaitForSeconds(holdDuration);
                }

                yield return Fade(1f, 0f, fadeOutDuration);
                root.blocksRaycasts = false;
            }

            _routine = null;
            onDone?.Invoke();
        }

        private IEnumerator Fade(float from, float to, float duration)
        {
            if (root == null)
            {
                yield break;
            }

            if (duration <= 0f)
            {
                root.alpha = to;
                yield break;
            }

            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                root.alpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }

            root.alpha = to;
        }
    }
}
