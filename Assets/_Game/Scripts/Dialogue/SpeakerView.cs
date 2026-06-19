using UnityEngine;
using UnityEngine.UI;

namespace GemCafe.Dialogue
{
    public class SpeakerView : MonoBehaviour
    {
        [SerializeField] private Image leftPortrait;
        [SerializeField] private Image rightPortrait;
        [SerializeField] private Image backgroundDim;
        [SerializeField] private Color activeColor = Color.white;
        [SerializeField] private Color dimColor = new Color(0.4f, 0.4f, 0.4f, 1f);
        [SerializeField] private string leftSpeakerId;

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
            color.a = on ? 1f : 0f;
            backgroundDim.color = color;
        }
    }
}
