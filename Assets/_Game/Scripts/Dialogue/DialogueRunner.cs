using System;
using System.Collections.Generic;
using GemCafe.Core;
using GemCafe.Data;
using UnityEngine;

namespace GemCafe.Dialogue
{
    public class DialogueRunner : MonoBehaviour
    {
        [SerializeField] private DialogueView view;
        [SerializeField] private SpeakerView speakerView;

        private IReadOnlyList<DialogueLine> _lines;
        private int _index;
        private Action _onComplete;

        public bool IsPlaying { get; private set; }

        // partnerOnRight: 대화 상대 NPC가 플레이어 기준 오른쪽에 있으면 true(기본값).
        public void Play(IReadOnlyList<DialogueLine> lines, Action onComplete = null, bool partnerOnRight = true)
        {
            if (lines == null || lines.Count == 0)
            {
                onComplete?.Invoke();
                return;
            }

            _lines = lines;
            _index = 0;
            _onComplete = onComplete;
            IsPlaying = true;

            EventBus.RaiseDialogueStarted();

            if (view != null)
            {
                view.Show(true);
                view.BindNext(Next);
            }

            if (speakerView != null)
            {
                speakerView.Show(true);
                speakerView.SetPartnerSide(partnerOnRight);
                speakerView.SetBackgroundDim(true);
            }

            ShowCurrent();
        }

        public void Next()
        {
            if (!IsPlaying)
            {
                return;
            }

            if (view != null && view.IsTyping)
            {
                view.CompleteTyping();
                return;
            }

            _index++;
            if (_lines == null || _index >= _lines.Count)
            {
                End();
                return;
            }

            ShowCurrent();
        }

        private void ShowCurrent()
        {
            if (_lines == null || _index < 0 || _index >= _lines.Count)
            {
                return;
            }

            DialogueLine line = _lines[_index];

            if (speakerView != null)
            {
                speakerView.SetSpeakerPortrait(line.speakerId, line.portrait);
                speakerView.Highlight(line.speakerId);
            }

            float typingCps = 30f;
            if (GameManager.Instance != null && GameManager.Instance.Config != null)
            {
                typingCps = GameManager.Instance.Config.typingCps;
            }

            if (view != null)
            {
                view.SetLine(line.speakerId, line.text, typingCps);
            }
        }

        private void End()
        {
            IsPlaying = false;

            if (view != null)
            {
                view.Show(false);
            }

            if (speakerView != null)
            {
                speakerView.SetBackgroundDim(false);
                speakerView.Show(false);
            }

            EventBus.RaiseDialogueEnded();

            var callback = _onComplete;
            _onComplete = null;
            _lines = null;
            _index = 0;
            callback?.Invoke();
        }

        private void Update()
        {
            if (!IsPlaying)
            {
                return;
            }

            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
            {
                Next();
            }
        }
    }
}
