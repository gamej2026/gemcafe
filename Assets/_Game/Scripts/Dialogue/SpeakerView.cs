using System.Collections.Generic;
using GemCafe.Data;
using UnityEngine;
using UnityEngine.UI;

namespace GemCafe.Dialogue
{
    public class SpeakerView : MonoBehaviour
    {
        [SerializeField] private Image leftPortrait;
        [SerializeField] private Image rightPortrait;
        [SerializeField] private Image backgroundDim;
        [SerializeField] private RectTransform dialogueRect;
        [SerializeField] private Vector2 leftStandingPosition = new Vector2(180f, 260f);
        [SerializeField] private Vector2 rightStandingPosition = new Vector2(-180f, 260f);
        [SerializeField] private bool alignPortraitAboveDialogue;
        [SerializeField] private float portraitBottomMargin = 20f;
        [SerializeField] private float dimAlpha = 0.3f;   
        [SerializeField] private Color activeColor = Color.white;
        [SerializeField] private Color dimColor = new Color(0.4f, 0.4f, 0.4f, 1f);
        [SerializeField] private string leftSpeakerId;
        private static readonly Vector2 BottomCenterPivot = new Vector2(0.5f, 0f);
        private static readonly Vector2 LeftAnchor = new Vector2(0f, 0f);
        private static readonly Vector2 RightAnchor = new Vector2(1f, 0f);
        // 대화 상대(주인공이 아닌 화자)가 화면 오른쪽에 표시될지 여부.
        // 게임 월드에서 NPC가 플레이어 오른쪽에 있으면 true, 왼쪽이면 false.
        private bool _partnerOnRight = true;

        private void Awake()
        {
            ApplyPortraitLayout();
        }

        private void OnValidate()
        {
            ApplyPortraitLayout();
        }

        // 대화 시작 시 상대방 NPC가 플레이어 기준 어느 쪽에 있는지 알려준다.
        public void SetPartnerSide(bool partnerOnRight)
        {
            _partnerOnRight = partnerOnRight;
        }
        // 대화 시작/종료에 맞춰 SpeakerView 자신을 켜고 끌다. 빌드 직후에는 꺼져 있다.
        public void Show(bool visible)
        {
            if (gameObject.activeSelf != visible)
            {
                gameObject.SetActive(visible);
            }
        }

        public void SetPortrait(bool left, Sprite sprite)
        {
            Image target = left ? leftPortrait : rightPortrait;
            if (target == null)
            {
                return;
            }

            ConfigurePortraitRect(target.rectTransform, left);

            target.sprite = sprite;
            target.gameObject.SetActive(sprite != null);

            if (sprite != null)
            {
                target.SetNativeSize();
            }
        }

        // 화자 ID와 상대방 위치에 맞는 쪽에 일러스트를 표시한다.
        // 상대방이 오른쪽이면 주인공은 왼쪽, 상대방이 왼쪽이면 주인공은 오른쪽에 선다.
        // sprite가 null이면 기존 일러스트를 유지하기 위해 아무 것도 하지 않는다.
        public void SetSpeakerPortrait(string speakerId, Sprite sprite)
        {
            if (sprite == null)
            {
                return;
            }

            SetPortrait(UsesLeftSlot(speakerId), sprite);
        }

        // 대화 시작 시 양쪽 슬롯의 일러스트를 미리 채운다.
        // 아직 말하지 않은 상대방도 미리 표시되어 "듣고 있다"는 느낌을 준다.
        // 각 슬롯에는 해당 슬롯을 사용하는 화자의 첫 일러스트를 넣는다.
        public void PrimePortraits(IReadOnlyList<DialogueLine> lines)
        {
            if (lines == null)
            {
                return;
            }

            bool leftFilled = false;
            bool rightFilled = false;

            for (int i = 0; i < lines.Count; i++)
            {
                DialogueLine line = lines[i];
                if (line.portrait == null)
                {
                    continue;
                }

                bool usesLeft = UsesLeftSlot(line.speakerId);
                if (usesLeft && !leftFilled)
                {
                    SetPortrait(true, line.portrait);
                    leftFilled = true;
                }
                else if (!usesLeft && !rightFilled)
                {
                    SetPortrait(false, line.portrait);
                    rightFilled = true;
                }

                if (leftFilled && rightFilled)
                {
                    break;
                }
            }
        }

        public void Highlight(string activeSpeakerId)
        {
            bool activeUsesLeft = UsesLeftSlot(activeSpeakerId);

            if (leftPortrait != null)
            {
                leftPortrait.color = activeUsesLeft ? activeColor : dimColor;
            }

            if (rightPortrait != null)
            {
                rightPortrait.color = activeUsesLeft ? dimColor : activeColor;
            }
        }

        // 해당 화자가 왼쪽 슬롯을 사용하는지 판단한다.
        // 주인공은 상대방의 반대쪽, 상대방은 _partnerOnRight가 가리키는 쪽에 선다.
        private bool UsesLeftSlot(string speakerId)
        {
            bool isProtagonist = string.Equals(speakerId, leftSpeakerId, System.StringComparison.Ordinal);
            return isProtagonist == _partnerOnRight;
        }

        public void SetBackgroundDim(bool on)
        {
            if (backgroundDim == null)
            {
                return;
            }

            backgroundDim.gameObject.SetActive(on);
            var color = backgroundDim.color;
            color.a = on ? dimAlpha : 0f;
            backgroundDim.color = color;
        }

        private void ApplyPortraitLayout()
        {
            if (leftPortrait != null)
            {
                ConfigurePortraitRect(leftPortrait.rectTransform, true);
            }

            if (rightPortrait != null)
            {
                ConfigurePortraitRect(rightPortrait.rectTransform, false);
            }
        }

        private void ConfigurePortraitRect(RectTransform rect, bool left)
        {
            rect.anchorMin = left ? LeftAnchor : RightAnchor;
            rect.anchorMax = left ? LeftAnchor : RightAnchor;
            rect.pivot = BottomCenterPivot;
            rect.anchoredPosition = GetStandingPosition(left);
        }

        private Vector2 GetStandingPosition(bool left)
        {
            Vector2 pos = left ? leftStandingPosition : rightStandingPosition;

            if (!alignPortraitAboveDialogue || dialogueRect == null)
            {
                return pos;
            }

            float dialogueTop = dialogueRect.anchoredPosition.y + (dialogueRect.sizeDelta.y * (1f - dialogueRect.pivot.y));
            pos.y = dialogueTop + portraitBottomMargin;
            return pos;
        }
    }
     
}
