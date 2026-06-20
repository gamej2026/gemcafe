using UnityEngine;
using UnityEngine.UI;

namespace GemCafe.UI
{
    /// <summary>
    /// 레시피(재료/맛/효과) 내용을 다시 볼 수 있는 토글 팝업.
    /// 버튼을 한 번 누르면 보이고, 다시 누르면 닫힌다.
    /// 표시할 레시피 내용은 인스펙터에서 직접 수정할 수 있다.
    /// </summary>
    public class RecipePopup : MonoBehaviour
    {
        [SerializeField] private CanvasGroup root;
        [SerializeField] private Button toggleButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Image dim;
        [SerializeField] private Text contentText;
        [Tooltip("내용이 길 때 스크롤하기 위한 스크롤뷰. 열 때 맨 위로 이동시킨다.")]
        [SerializeField] private ScrollRect scrollRect;

        [TextArea(5, 20)]
        [SerializeField]
        private string recipeText =
            "염라 수염\t화한 시원함\t열을 식혀줌\n" +
            "도라지\t쓴맛\t속쓰림을 중화함\n" +
            "삼도천 강물\t신맛\t입안을 깔끔하게 해줌\n" +
            "호랑이 담뱃재\t오래된 향\t향수를 불러일으킴\n" +
            "곶감\t단맛\t활력을 불어줌\n" +
            "토끼 간\t부드러움\t뭉클한 감각을 줌\n" +
            "처녀귀신 머리카락\t감칠맛\t각성 효과를 줌";

        [SerializeField] private string emptyMessage = "레시피 내용이 없습니다.";

        public bool IsOpen { get; private set; }

        private void Awake()
        {
            if (toggleButton != null)
            {
                toggleButton.onClick.AddListener(Toggle);
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Close);
            }

            Close();
        }

        private void OnDestroy()
        {
            if (toggleButton != null)
            {
                toggleButton.onClick.RemoveListener(Toggle);
            }

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
                contentText.text = string.IsNullOrEmpty(recipeText) ? emptyMessage : recipeText;
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
