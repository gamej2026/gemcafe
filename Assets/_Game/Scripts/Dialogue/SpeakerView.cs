using UnityEngine;
using UnityEngine.UI;

namespace GemCafe.Dialogue
{
    public class SpeakerView : MonoBehaviour
    {
        [SerializeField] private Image leftPortrait;
        [SerializeField] private Image rightPortrait;
        [SerializeField] private Image backgroundDim;
        [SerializeField] private float dimAlpha = 0.3f;   
        [SerializeField] private Color activeColor = Color.white;
        [SerializeField] private Color dimColor = new Color(0.4f, 0.4f, 0.4f, 1f);
        [SerializeField] private string leftSpeakerId;

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

            target.sprite = sprite;
            target.gameObject.SetActive(sprite != null);
        }

        // 화자 ID에 맞는 쪽(주인공=왼쪽, 그 외=오른쪽)에 일러스트를 표시한다.
        // sprite가 null이면 기존 일러스트를 유지하기 위해 아무 것도 하지 않는다.
        public void SetSpeakerPortrait(string speakerId, Sprite sprite)
        {
            if (sprite == null)
            {
                return;
            }

            bool isLeft = string.Equals(speakerId, leftSpeakerId, System.StringComparison.Ordinal);
            SetPortrait(isLeft, sprite);
        }

        public void Highlight(string activeSpeakerId)
        {
            bool isLeftActive = string.Equals(activeSpeakerId, leftSpeakerId, System.StringComparison.Ordinal);

            if (leftPortrait != null)
            {
                leftPortrait.color = isLeftActive ? activeColor : dimColor;
            }

            if (rightPortrait != null)
            {
                rightPortrait.color = isLeftActive ? dimColor : activeColor;
            }
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
    }
     
}
