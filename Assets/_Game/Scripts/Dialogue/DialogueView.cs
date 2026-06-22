using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace GemCafe.Dialogue
{
    public class DialogueView : MonoBehaviour
    {
        // 나레이션(이 화자)은 네임태그를 표시하지 않는다.
        private const string NarrationSpeaker = "나레이션";

        [SerializeField] private CanvasGroup root;
        [SerializeField] private Text speakerNameText;
        [SerializeField] private Text bodyText;
        [SerializeField] private Button nextButton;

        private Coroutine _typingCoroutine;
        private string _fullBodyText = string.Empty;
        private Action _onTypingComplete;

        public bool IsTyping { get; private set; }

        public void Show(bool visible)
        {
            if (root == null)
            {
                return;
            }

            // 대화 루트 GameObject가 비활성 상태면 활성화한다. 비활성 상태에서는
            // CanvasGroup.alpha 만 바꿔도 화면에 보이지 않고 타이핑 코루틴도 시작되지 않는다.
            if (root.gameObject.activeSelf != visible)
            {
                root.gameObject.SetActive(visible);
            }

            root.alpha = visible ? 1f : 0f;
            root.interactable = visible;
            root.blocksRaycasts = visible;
        }

        public void SetLine(string speakerName, string body, float typingCps, Action onComplete = null)
        {
            if (_typingCoroutine != null)
            {
                StopCoroutine(_typingCoroutine);
                _typingCoroutine = null;
            }

            _fullBodyText = body ?? string.Empty;
            _onTypingComplete = onComplete;

            if (speakerNameText != null)
            {
                // 나레이션이거나 화자가 비어 있으면 네임태그를 숨긴다.
                string trimmedSpeaker = speakerName != null ? speakerName.Trim() : string.Empty;
                bool hideName = trimmedSpeaker.Length == 0 || trimmedSpeaker == NarrationSpeaker;
                speakerNameText.text = hideName ? string.Empty : speakerName;
                if (speakerNameText.gameObject.activeSelf == hideName)
                {
                    speakerNameText.gameObject.SetActive(!hideName);
                }
            }

            if (typingCps <= 0f)
            {
                if (bodyText != null)
                {
                    bodyText.text = _fullBodyText;
                }

                IsTyping = false;
                var callback = _onTypingComplete;
                _onTypingComplete = null;
                callback?.Invoke();
                return;
            }

            if (bodyText != null)
            {
                bodyText.text = string.Empty;
            }

            _typingCoroutine = StartCoroutine(TypeRoutine(_fullBodyText, typingCps));
        }

        public void CompleteTyping()
        {
            if (_typingCoroutine != null)
            {
                StopCoroutine(_typingCoroutine);
                _typingCoroutine = null;
            }

            if (bodyText != null)
            {
                bodyText.text = _fullBodyText;
            }

            IsTyping = false;

            var callback = _onTypingComplete;
            _onTypingComplete = null;
            callback?.Invoke();
        }

        public void BindNext(Action onNext)
        {
            if (nextButton == null)
            {
                return;
            }

            nextButton.onClick.RemoveAllListeners();
            if (onNext != null)
            {
                nextButton.onClick.AddListener(() => onNext());
            }
        }

        private IEnumerator TypeRoutine(string fullText, float typingCps)
        {
            IsTyping = true;
            float elapsed = 0f;
            int shownCount = 0;
            int length = fullText.Length;

            while (shownCount < length)
            {
                elapsed += Time.deltaTime;
                int targetCount = Mathf.Min(length, Mathf.FloorToInt(elapsed * typingCps));

                if (targetCount != shownCount)
                {
                    shownCount = targetCount;
                    if (bodyText != null)
                    {
                        bodyText.text = fullText.Substring(0, shownCount);
                    }
                }

                yield return null;
            }

            if (bodyText != null)
            {
                bodyText.text = fullText;
            }

            IsTyping = false;
            _typingCoroutine = null;

            var callback = _onTypingComplete;
            _onTypingComplete = null;
            callback?.Invoke();
        }
    }
}
