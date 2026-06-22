using GemCafe.Core;
using GemCafe.Customer;
using GemCafe.Data;
using UnityEngine;
using UnityEngine.UI;

namespace GemCafe.UI
{
    /// <summary>
    /// 직전 손님과의 '일반' 분기 대화를 카카오톡형 말풍선으로 다시 볼 수 있는 토글 팝업.
    /// 손님 대사는 왼쪽, 점원(직원) 대사는 오른쪽 말풍선으로 표시하고, 길면 스크롤된다.
    /// 나레이션 줄은 표시하지 않는다.
    /// </summary>
    public class OrderRecallPopup : MonoBehaviour
    {
        private const string NarrationSpeaker = "나레이션";

        [SerializeField] private CanvasGroup root;
        [SerializeField] private Button toggleButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Image dim;

        [Header("대화 로그 (말풍선)")]
        [Tooltip("말풍선이 쌓이는 스크롤 Content(RectTransform).")]
        [SerializeField] private RectTransform messageRoot;
        [Tooltip("로그 스크롤뷰. 열 때 맨 위로 이동시키는 데 사용.")]
        [SerializeField] private ScrollRect scrollRect;
        [Tooltip("일반 분기 대사를 가져올 대화 테이블.")]
        [SerializeField] private CafeMainDialogTable dialogTable;
        [Tooltip("말풍선 배경 스프라이트(9-slice 권장).")]
        [SerializeField] private Sprite bubbleSprite;

        [Header("스타일")]
        [SerializeField] private float maxBubbleWidth = 440f;
        [SerializeField] private int messageFontSize = 24;
        [SerializeField] private int nameFontSize = 18;
        [SerializeField] private Color customerBubbleColor = new Color(0.93f, 0.93f, 0.95f, 1f);
        [SerializeField] private Color clerkBubbleColor = new Color(1f, 0.90f, 0.32f, 1f);
        [SerializeField] private Color bubbleTextColor = new Color(0.12f, 0.12f, 0.14f, 1f);
        [SerializeField] private Color nameColor = new Color(0.82f, 0.82f, 0.86f, 1f);

        [SerializeField] private string emptyMessage = "아직 손님과 나눈 대화가 없습니다.";

        public bool IsOpen { get; private set; }

        /// <summary>튜토리얼 강조용: 주문 회상 토글 버튼의 RectTransform.</summary>
        public RectTransform ToggleRect => toggleButton != null ? toggleButton.transform as RectTransform : null;

        private int _day = -1;
        private Font _font;

        private void Awake()
        {
            _font = KoreanFontApplier.Font ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

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

        private void OnEnable()
        {
            EventBus.OnCustomerArrived += HandleCustomerArrived;
        }

        private void OnDisable()
        {
            EventBus.OnCustomerArrived -= HandleCustomerArrived;
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
            Rebuild();
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

        private void HandleCustomerArrived(CustomerSO customer)
        {
            _day = customer != null ? customer.day : -1;

            if (IsOpen)
            {
                Rebuild();
            }
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

        private void Rebuild()
        {
            if (messageRoot == null)
            {
                return;
            }

            if (_font == null)
            {
                _font = KoreanFontApplier.Font ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            for (int i = messageRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(messageRoot.GetChild(i).gameObject);
            }

            var lines = dialogTable != null
                ? dialogTable.GetLines(_day, CafeMainDialogTable.BranchNormal)
                : null;

            int shown = 0;
            if (lines != null)
            {
                for (int i = 0; i < lines.Count; i++)
                {
                    var line = lines[i];
                    if (string.IsNullOrEmpty(line.text))
                    {
                        continue;
                    }

                    string speaker = line.speaker != null ? line.speaker.Trim() : string.Empty;
                    if (!line.isCustomerLine && speaker == NarrationSpeaker)
                    {
                        continue;
                    }

                    CreateBubble(speaker, line.text, line.isCustomerLine);
                    shown++;
                }
            }

            if (shown == 0)
            {
                CreateEmptyLabel();
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(messageRoot);
        }

        private void CreateEmptyLabel()
        {
            var go = new GameObject("Empty", typeof(RectTransform));
            go.transform.SetParent(messageRoot, false);
            var text = go.AddComponent<Text>();
            text.font = _font;
            text.fontSize = messageFontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = nameColor;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.text = emptyMessage;

            var fitter = go.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private void CreateBubble(string speaker, string message, bool isCustomer)
        {
            float paddingX = 44f;
            float maxMsgWidth = Mathf.Max(80f, maxBubbleWidth - paddingX);

            // Row: 한 줄을 가로로 채우고 말풍선을 좌/우로 정렬한다.
            var rowGo = new GameObject(isCustomer ? "Row_Customer" : "Row_Clerk", typeof(RectTransform));
            rowGo.transform.SetParent(messageRoot, false);
            var rowLayout = rowGo.AddComponent<HorizontalLayoutGroup>();
            rowLayout.childAlignment = isCustomer ? TextAnchor.UpperLeft : TextAnchor.UpperRight;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;

            // Column: 이름표 + 말풍선을 세로로 쌓는다. 폭을 측정값으로 고정한다.
            var colGo = new GameObject("Bubble", typeof(RectTransform));
            colGo.transform.SetParent(rowGo.transform, false);
            var colLayout = colGo.AddComponent<VerticalLayoutGroup>();
            colLayout.childAlignment = isCustomer ? TextAnchor.UpperLeft : TextAnchor.UpperRight;
            colLayout.childControlWidth = true;
            colLayout.childControlHeight = true;
            colLayout.childForceExpandWidth = true;
            colLayout.childForceExpandHeight = false;
            colLayout.spacing = 4f;

            // 이름표
            var nameGo = new GameObject("Name", typeof(RectTransform));
            nameGo.transform.SetParent(colGo.transform, false);
            var nameText = nameGo.AddComponent<Text>();
            nameText.font = _font;
            nameText.fontSize = nameFontSize;
            nameText.color = nameColor;
            nameText.alignment = isCustomer ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight;
            nameText.horizontalOverflow = HorizontalWrapMode.Overflow;
            nameText.verticalOverflow = VerticalWrapMode.Overflow;
            nameText.text = speaker;
            var nameFitter = nameGo.AddComponent<ContentSizeFitter>();
            nameFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // 말풍선 배경 + 메시지
            var bubbleGo = new GameObject("Balloon", typeof(RectTransform));
            bubbleGo.transform.SetParent(colGo.transform, false);
            var bubbleImage = bubbleGo.AddComponent<Image>();
            bubbleImage.sprite = bubbleSprite;
            bubbleImage.type = bubbleSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            bubbleImage.color = isCustomer ? customerBubbleColor : clerkBubbleColor;

            var bubbleLayout = bubbleGo.AddComponent<HorizontalLayoutGroup>();
            bubbleLayout.padding = new RectOffset(22, 22, 14, 14);
            bubbleLayout.childControlWidth = true;
            bubbleLayout.childControlHeight = true;
            bubbleLayout.childForceExpandWidth = true;
            bubbleLayout.childForceExpandHeight = false;

            var msgGo = new GameObject("Message", typeof(RectTransform));
            msgGo.transform.SetParent(bubbleGo.transform, false);
            var msgText = msgGo.AddComponent<Text>();
            msgText.font = _font;
            msgText.fontSize = messageFontSize;
            msgText.color = bubbleTextColor;
            msgText.horizontalOverflow = HorizontalWrapMode.Overflow;
            msgText.verticalOverflow = VerticalWrapMode.Overflow;
            msgText.text = message;

            // 한 줄 폭을 측정해 최대폭까지만 늘리고, 넘치면 줄바꿈한다.
            float oneLineWidth = msgText.preferredWidth;
            float msgWidth = Mathf.Min(oneLineWidth, maxMsgWidth);
            if (oneLineWidth > maxMsgWidth)
            {
                msgText.horizontalOverflow = HorizontalWrapMode.Wrap;
            }

            var colElement = colGo.AddComponent<LayoutElement>();
            colElement.preferredWidth = msgWidth + paddingX;
        }
    }
}
