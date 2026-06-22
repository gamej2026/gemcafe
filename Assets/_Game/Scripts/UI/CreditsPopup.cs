using UnityEngine;
using UnityEngine.UI;

namespace GemCafe.UI
{
    /// <summary>
    /// 제작진(크레딧) 정보를 보여주는 팝업.
    /// 표시할 크레딧 내용은 인스펙터에서 직접 수정할 수 있다.
    /// </summary>
    public class CreditsPopup : MonoBehaviour
    {
        [SerializeField] private CanvasGroup root;
        [SerializeField] private Button closeButton;
        [SerializeField] private Image dim;
        [SerializeField] private Text contentText;
        [Tooltip("내용이 길 때 스크롤하기 위한 스크롤뷰. 열 때 맨 위로 이동시킨다.")]
        [SerializeField] private ScrollRect scrollRect;

        [TextArea(10, 30)]
        [SerializeField]
        private string creditsText =
            "나루도원 (Naru Dowon)\n" +
            "A Game by Team 젬카페 (ZemCafe)\n" +
            "\n" +
            "기획 (Game Design)\n" +
            "오은영\n" +
            "이하윤\n" +
            "\n" +
            "아트 (Art & Animation)\n" +
            "배정훈\n" +
            "김유진\n" +
            "권준성\n" +
            "김현지\n" +
            "\n" +
            "프로그래밍 (Programming)\n" +
            "이하윤\n" +
            "박찬성\n" +
            "선인재\n" +
            "\n" +
            "\n" +
            "Thanks to\n" +
            "ChatGPT\n" +
            "Gemini\n" +
            "Claude\n" +
            "\n" +
            "\n" +
            "Special Thanks to\n" +
            "\n" +
            "ZEMPIE";

        [SerializeField] private string emptyMessage = "크레딧 내용이 없습니다.";

        public bool IsOpen { get; private set; }

        private void Awake()
        {
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Close);
            }

            Close();
        }

        private void OnDestroy()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Close);
            }
        }

        public void Toggle()
        {
            if (IsOpen)
            {
                Close();
            }
            else
            {
                Open();
            }
        }

        public void Open()
        {
            if (contentText != null)
            {
                contentText.text = string.IsNullOrEmpty(creditsText) ? emptyMessage : creditsText;
            }

            SetVisible(true);
            IsOpen = true;

            if (scrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                scrollRect.verticalNormalizedPosition = 1f;
            }
        }

        public void Close()
        {
            SetVisible(false);
            IsOpen = false;
        }

        private void SetVisible(bool visible)
        {
            if (root != null)
            {
                root.alpha = visible ? 1f : 0f;
                root.interactable = visible;
                root.blocksRaycasts = visible;
            }

            if (dim != null)
            {
                dim.enabled = visible;
            }
        }
    }
}
