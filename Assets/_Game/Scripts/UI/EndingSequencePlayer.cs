using System.Collections.Generic;
using GemCafe.Core;
using UnityEngine;
using UnityEngine.UI;

namespace GemCafe.UI
{
    /// <summary>
    /// 3일차 종료 후 코인 확인(EndingCoinSummary)을 거쳐 GameState.Ending에 진입하면
    /// 획득한 코인 결과(EndingKind A/B/C)에 따라 엔딩 연출을 페이지 단위로 재생한다.
    /// Doc/엔딩.pdf의 엔딩 문구를 그대로 표시하며, 마지막 「다음」에서 로비로 복귀한다.
    /// 씬 배치/와이어링 없이 동작하도록 자체 오버레이 캔버스를 런타임에 생성하고,
    /// 게임 시작 시 DontDestroyOnLoad 컨트롤러로 자동 부트스트랩된다.
    /// </summary>
    public class EndingSequencePlayer : MonoBehaviour
    {
        private static bool _spawned;

        /// <summary>현재 살아있는 엔딩 연출 인스턴스(테스트 트리거용).</summary>
        public static EndingSequencePlayer Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _spawned = false;
            Instance = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (_spawned)
            {
                return;
            }

            _spawned = true;

            var go = new GameObject("[EndingSequencePlayer]");
            DontDestroyOnLoad(go);
            go.AddComponent<EndingSequencePlayer>();
        }

        private CanvasGroup _root;
        private Text _bodyText;
        private Button _nextButton;

        private readonly List<string> _pages = new List<string>();
        private int _index;
        private bool _uiBuilt;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnEnable()
        {
            EventBus.OnStateChanged += HandleState;
        }

        private void OnDisable()
        {
            EventBus.OnStateChanged -= HandleState;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void HandleState(GameState from, GameState to)
        {
            if (to != GameState.Ending)
            {
                SetVisible(false);
                return;
            }

            Play();
        }

        public void Play()
        {
            var gm = GameManager.Instance;
            var kind = gm != null ? gm.PendingEnding : EndingKind.B;
            var totalCoins = gm != null ? gm.PendingTotalCoins : 0;

            PlayEnding(kind, totalCoins);
        }

        /// <summary>
        /// GameManager 상태와 무관하게 지정한 엔딩을 즉시 재생한다(디버그/테스트용).
        /// </summary>
        public void PlayEnding(EndingKind kind, int totalCoins)
        {
            BuildPages(kind, totalCoins);
            _index = 0;

            EnsureUI();
            ShowCurrentPage();
            SetVisible(true);
        }

        /// <summary>
        /// 엔딩 연출을 테스트로 트리거한다. GameManager가 있으면 정식 경로(상태 전이)로,
        /// 없으면 오버레이를 직접 재생한다. 에디터 메뉴/단축키에서 호출한다.
        /// </summary>
        public static void TriggerTest(EndingKind kind, int totalCoins, int greatCoins)
        {
            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.SetEndingResult(kind, totalCoins, greatCoins);
                // 검증을 우회해 어떤 상태에서든 엔딩 연출로 진입(테스트 목적).
                gm.StateMachine.Restore(GameState.Ending);
                return;
            }

            if (Instance != null)
            {
                Instance.PlayEnding(kind, totalCoins);
            }
            else
            {
                Debug.LogWarning("EndingSequencePlayer.TriggerTest: 인스턴스가 아직 생성되지 않았습니다. 플레이 모드에서 시도하세요.");
            }
        }

        private void BuildPages(EndingKind kind, int totalCoins)
        {
            _pages.Clear();

            switch (kind)
            {
                case EndingKind.A:
                    // 진엔딩: 대성공(금) 코인이 3개인 경우
                    _pages.Add("엔딩 A\n\n모든 진실을 알게 된다\n\n마님 = 엄마\n손님 = 나의 죄");
                    break;

                case EndingKind.B:
                    // 노말 엔딩: 코인 종류 상관 없이 코인이 3개인 경우
                    _pages.Add("엔딩 B\n\n마님의 마지막 인사 받으며 배 탐\n손님들의 이야기, 묘하게 공감이 갔다...");
                    break;

                default:
                    // 배드엔딩: 뱃삯을 다 벌지 못한 경우(코인 0~2개)
                    _pages.Add("결국 뱃삯은 못 벌었네...");
                    if (totalCoins <= 0)
                    {
                        // 배드엔딩: 코인이 0개인 경우
                        _pages.Add("엔딩 C\n\n마님이 왜 기회를 줘도 이렇게 답답하게 구냐며 속상해 함\n카페에서 쫓겨나며 엔딩");
                    }
                    break;
            }

            if (_pages.Count == 0)
            {
                _pages.Add("엔딩");
            }
        }

        private void ShowCurrentPage()
        {
            if (_bodyText != null && _index >= 0 && _index < _pages.Count)
            {
                _bodyText.text = _pages[_index];
            }
        }

        private void HandleNext()
        {
            _index++;

            if (_index < _pages.Count)
            {
                ShowCurrentPage();
                return;
            }

            Finish();
        }

        private void Finish()
        {
            SetVisible(false);

            var gm = GameManager.Instance;
            if (gm == null)
            {
                return;
            }

            if (gm.StateMachine.TryTransition(GameState.Lobby))
            {
                gm.Router?.Load(SceneRouter.SceneLobby);
            }
        }

        private void SetVisible(bool visible)
        {
            if (_root == null)
            {
                return;
            }

            _root.alpha = visible ? 1f : 0f;
            _root.interactable = visible;
            _root.blocksRaycasts = visible;
        }

        private void EnsureUI()
        {
            if (_uiBuilt)
            {
                return;
            }

            _uiBuilt = true;

            var canvasGo = new GameObject("EndingCanvas");
            canvasGo.transform.SetParent(transform, false);

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            _root = canvasGo.AddComponent<CanvasGroup>();
            _root.alpha = 0f;
            _root.interactable = false;
            _root.blocksRaycasts = false;

            // 배경 패널 (엔딩 편지 톤: 밝은 배경)
            var bg = CreateChild("Background", canvasGo.transform);
            StretchFull(bg);
            var bgImage = bg.gameObject.AddComponent<Image>();
            bgImage.color = new Color(0.96f, 0.96f, 0.96f, 1f);

            // 본문 텍스트
            var body = CreateChild("Body", bg);
            body.anchorMin = new Vector2(0.12f, 0.2f);
            body.anchorMax = new Vector2(0.88f, 0.85f);
            body.offsetMin = Vector2.zero;
            body.offsetMax = Vector2.zero;

            _bodyText = body.gameObject.AddComponent<Text>();
            _bodyText.alignment = TextAnchor.MiddleLeft;
            _bodyText.fontSize = 44;
            _bodyText.color = new Color(0.25f, 0.25f, 0.25f, 1f);
            _bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _bodyText.verticalOverflow = VerticalWrapMode.Overflow;
            _bodyText.supportRichText = false;
            ApplyKoreanFont(_bodyText);

            // 「다음」 버튼 (우측 하단)
            var buttonGo = CreateChild("NextButton", bg);
            buttonGo.anchorMin = new Vector2(1f, 0f);
            buttonGo.anchorMax = new Vector2(1f, 0f);
            buttonGo.pivot = new Vector2(1f, 0f);
            buttonGo.anchoredPosition = new Vector2(-80f, 60f);
            buttonGo.sizeDelta = new Vector2(220f, 90f);

            var buttonImage = buttonGo.gameObject.AddComponent<Image>();
            buttonImage.color = new Color(0.85f, 0.85f, 0.85f, 1f);

            _nextButton = buttonGo.gameObject.AddComponent<Button>();
            _nextButton.targetGraphic = buttonImage;
            _nextButton.onClick.AddListener(HandleNext);

            var label = CreateChild("Label", buttonGo);
            StretchFull(label);
            var labelText = label.gameObject.AddComponent<Text>();
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.fontSize = 36;
            labelText.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            labelText.text = "다음";
            ApplyKoreanFont(labelText);
        }

        private static RectTransform CreateChild(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void ApplyKoreanFont(Text text)
        {
            var font = KoreanFontApplier.Font;
            if (font != null)
            {
                text.font = font;
            }
            else
            {
                text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
        }
    }
}
