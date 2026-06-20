using System.Collections;
using System.Collections.Generic;
using GemCafe.Core;
using GemCafe.Crafting;
using GemCafe.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GemCafe.Tutorial
{
    /// <summary>
    /// 카페 튜토리얼 연출 감독.
    ///
    /// 설계 요약 (요구사항 대응):
    /// - 실제 Cafe 씬을 Additive 로 띄워 "살아있는 배경"으로 사용한다. 따라서 Cafe 씬이
    ///   바뀌어도 튜토리얼이 자동으로 그 변경을 반영한다. (Cafe 변경에도 튜토리얼 정상 작동)
    /// - 모든 오버레이 UI(어둡게 가리기 + 강조 프레임 + 대화창)는 코드로만 생성하므로
    ///   Cafe 씬 자체는 전혀 수정하지 않는다. (Cafe 씬은 튜토리얼과 무관하게 정상 작동)
    /// - 진행 중에는 입력을 막고 실제 서비스(손님 응대/저장)는 절대 돌리지 않는다.
    ///   <see cref="TutorialContext"/> 가 DayManager 의 자동 시작/저장을 차단한다.
    ///   (튜토리얼 결과가 실제 데이터에 영향/저장되지 않음)
    /// - 강조 대상은 컴포넌트 "타입"으로 찾는다(FindFirstObjectByType). Cafe 의 계층 구조가
    ///   바뀌어도 도구를 계속 찾아낸다. 못 찾으면 강조 생략.
    /// - 튜토리얼이 끝나면 Cafe 씬을 단일 로드로 새로 띄워 깨끗한 실제 게임을 시작한다.
    /// </summary>
    public class CafeTutorialDirector : MonoBehaviour
    {
        [Tooltip("Additive 로 배경에 띄울 실제 카페 씬 이름.")]
        [SerializeField] private string cafeSceneName = "Cafe";
        [Tooltip("튜토리얼 대사 CSV 의 Resources 경로(확장자 제외).")]
        [SerializeField] private string csvResourcePath = "Cafe/cafe_tutorial";
        [Tooltip("배경(Cafe)을 어둡게 가리는 정도. 1에 가까울수록 어둡다.")]
        [Range(0f, 1f)] [SerializeField] private float dimAlpha = 0.72f;

        // 강조 대상이 차 제조 도구일 때, 실제 제조 화면을 배경으로 열어 도구가 보이도록 한다.
        private static readonly HashSet<string> CraftHighlights = new HashSet<string>
        {
            "tray", "bowl", "pestle", "teaware"
        };

        private RectTransform _overlayRect;
        private CanvasGroup _overlayGroup;
        private Image _dim;
        private Image _speakerPortrait;
        private Text _speakerText;
        private Text _bodyText;
        private GameObject _hint;
        private Text _hintText;
        private RectTransform _highlight;

        // TalkDialog 패널 (하단 고정 대화창)
        private RectTransform _dialogPanelRect;

        // PositionedPopup 패널 (화면 임의 위치 팝업)
        private RectTransform _popupPanelRect;
        private Image _popupPortrait;
        private Text _popupSpeakerText;
        private Text _popupBodyText;
        private Text _popupHintText;

        private bool _craftOpened;

        // 현재 스폰되어 대화 동안 유지 중인 프리팹 인스턴스와 그 Resources 키.
        private GameObject _spawnedInstance;
        private string _spawnedKey = string.Empty;

        private const string DefaultHint = "클릭 / 스페이스로 계속 ▶";

        private void Awake()
        {
            // 이 시점부터 DayManager 의 자동 서비스/저장이 차단된다.
            TutorialContext.Begin();
        }

        private void Start()
        {
            BuildOverlay();
            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            // 1) 실제 Cafe 씬을 Additive 로 로드 (배경 + 강조 앵커).
            if (!string.IsNullOrEmpty(cafeSceneName))
            {
                var existing = SceneManager.GetSceneByName(cafeSceneName);
                if (!existing.isLoaded)
                {
                    var op = SceneManager.LoadSceneAsync(cafeSceneName, LoadSceneMode.Additive);
                    while (op != null && !op.isDone)
                    {
                        yield return null;
                    }
                }
            }

            // Cafe 의 Awake/Start(과 폰트 적용)가 한 번 돌도록 한 프레임 대기.
            yield return null;

            var lines = CafeTutorialCsvLoader.Load(csvResourcePath);
            if (lines == null || lines.Count == 0)
            {
                FinishTutorial();
                yield break;
            }

            // 2) 대사를 순서대로 재생. 각 줄은 클릭/스페이스로 진행.
            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                ShowLine(line);

                // 이 줄이 지정한 스폰 프리팹을 반영(없으면 직전 스폰을 그대로 유지).
                yield return ApplySpawnPrefab(line.spawnPrefab);

                yield return WaitForAdvance();

                // 미니게임 안내 줄: 실제 미니게임을 띄워 플레이어가 직접 해보게 한 뒤 다음 줄로 진행.
                if (line.action == "waitminigame1talkui")
                {
                    yield return PlayMinigame(MinigameKind.Mix);
                }
                else if (line.action == "waitminigame2talkui")
                {
                    yield return PlayMinigame(MinigameKind.Pour);
                }
                else if (line.action == "waitforbowlfilled")
                {
                    yield return WaitForBowlFilled();
                }
                else if (line.action == "end")
                {
                    break;
                }
            }

            // 튜토리얼이 끝나면 남아 있는 스폰 프리팹을 (트윈이 있으면 끝난 뒤) 제거.
            yield return DespawnPrefab();

            FinishTutorial();
        }

        private void ShowLine(TutorialLine line)
        {
            bool isPopup = line.uiType == TutorialUiType.PositionedPopup;

            // 활성 패널 전환.
            if (_dialogPanelRect != null)
            {
                _dialogPanelRect.gameObject.SetActive(!isPopup);
            }

            if (_popupPanelRect != null)
            {
                _popupPanelRect.gameObject.SetActive(isPopup);
            }

            if (isPopup)
            {
                // PositionedPopup: 지정 위치에 팝업을 배치한다.
                PositionPopupPanel(line.popupAnchor);

                bool hasSpeaker = !string.IsNullOrWhiteSpace(line.speaker);
                if (_popupSpeakerText != null)
                {
                    _popupSpeakerText.text = hasSpeaker ? line.speaker : string.Empty;
                    _popupSpeakerText.gameObject.SetActive(hasSpeaker);
                }

                if (_popupBodyText != null)
                {
                    _popupBodyText.text = line.text;
                }

                ApplySpeakerPortrait(_popupPortrait, line);
            }
            else
            {
                // TalkDialog: 하단 고정 대화창.
                if (_speakerText != null)
                {
                    bool hasSpeaker = !string.IsNullOrWhiteSpace(line.speaker);
                    _speakerText.text = hasSpeaker ? line.speaker : string.Empty;
                    _speakerText.gameObject.SetActive(hasSpeaker);
                }

                if (_bodyText != null)
                {
                    _bodyText.text = line.text;
                }

                ApplySpeakerPortrait(_speakerPortrait, line);
            }

            // 차 제조 도구를 강조해야 하면 실제 제조 화면을 배경으로 연다(입력은 막혀 있음).
            if (!_craftOpened && CraftHighlights.Contains(line.highlight))
            {
                OpenCraftingBackdrop();
            }

            ApplyHighlight(line.highlight);
        }

        private void PositionPopupPanel(Vector2 anchor)
        {
            if (_popupPanelRect == null)
            {
                return;
            }

            _popupPanelRect.anchorMin = anchor;
            _popupPanelRect.anchorMax = anchor;
            _popupPanelRect.pivot = new Vector2(0.5f, 0.5f);
            _popupPanelRect.anchoredPosition = Vector2.zero;
        }

        private IEnumerator WaitForAdvance()
        {
            // 직전 화면(예: cafe_dialog 마지막 클릭)의 입력이 즉시 다음 줄로 넘어가지 않도록 약간 대기.
            float guard = 0.2f;
            while (guard > 0f)
            {
                guard -= Time.unscaledDeltaTime;
                RepositionHighlight();
                yield return null;
            }

            while (true)
            {
                RepositionHighlight();

                if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
                {
                    yield break;
                }

                yield return null;
            }
        }

        private void OpenCraftingBackdrop()
        {
            _craftOpened = true;
            var crafting = Object.FindFirstObjectByType<CraftingController>();
            if (crafting != null)
            {
                // targetRecipe 없이 시각적 배경으로만 제조 화면을 연다. 입력이 막혀 있어
                // 실제 제조/판정/저장은 일어나지 않는다.
                crafting.BeginCraft(null);
            }
        }

        // ---------- 스폰 프리팹 (대화 동안 유지되는 프리팹) ----------

        /// <summary>
        /// 한 줄이 지정한 스폰 프리팹을 반영한다.
        /// - 같은 프리팹 키가 이미 떠 있으면: 그대로 유지(여러 줄에 걸쳐 유지하려면 같은 값을 반복 지정).
        /// - 그 외(빈 값 포함, 다른 값)이면: 직전 스폰을 제거한다. 이때 사라지는 트윈이 있으면
        ///   트윈을 백그라운드로 재생하고 끝난 뒤 파괴하므로 대화 진행을 막지 않는다.
        /// - 새 값이 비어 있지 않으면 새 프리팹을 스폰한다.
        /// 즉, 스폰 프리팹은 자신을 지정한 줄(대화) 동안 유지되고, 대화가 넘어가면 사라진다.
        /// </summary>
        private IEnumerator ApplySpawnPrefab(string resourcePath)
        {
            string desired = string.IsNullOrWhiteSpace(resourcePath) ? string.Empty : resourcePath;

            // 같은 프리팹이 이미 떠 있으면 유지.
            if (_spawnedInstance != null && _spawnedKey == desired)
            {
                yield break;
            }

            // 직전 스폰은 (사라지는 트윈이 있으면 끝난 뒤) 백그라운드로 제거한다.
            if (_spawnedInstance != null)
            {
                StartCoroutine(FadeOutAndDestroy(_spawnedInstance));
            }

            _spawnedInstance = null;
            _spawnedKey = string.Empty;

            if (desired.Length == 0)
            {
                yield break;
            }

            var prefab = Resources.Load<GameObject>(desired);
            if (prefab == null)
            {
                Debug.LogWarning($"CafeTutorialDirector: 스폰 프리팹 '{desired}' 를 찾을 수 없습니다.");
                yield break;
            }

            _spawnedInstance = Instantiate(prefab);
            _spawnedKey = desired;
        }

        /// <summary>
        /// 현재 스폰된 프리팹을 제거한다(튜토리얼 종료 시). <see cref="ITutorialSpawnDisappear"/> 가 있으면
        /// 사라지는 트윈을 재생하고 끝날 때까지 기다린 뒤 파괴한다.
        /// </summary>
        private IEnumerator DespawnPrefab()
        {
            var instance = _spawnedInstance;
            _spawnedInstance = null;
            _spawnedKey = string.Empty;

            yield return FadeOutAndDestroy(instance);
        }

        /// <summary>
        /// 인스턴스에 사라지는 트윈이 있으면 끝까지 재생한 뒤 파괴한다. 없으면 즉시 파괴한다.
        /// </summary>
        private static IEnumerator FadeOutAndDestroy(GameObject instance)
        {
            if (instance == null)
            {
                yield break;
            }

            var disappear = instance.GetComponent<ITutorialSpawnDisappear>()
                ?? instance.GetComponentInChildren<ITutorialSpawnDisappear>(true);

            if (disappear != null)
            {
                yield return disappear.PlayDisappear();
            }

            if (instance != null)
            {
                Destroy(instance);
            }
        }

        // ---------- 미니게임 체험 (waitMiniGame*talkUI 액션) ----------

        /// <summary>
        /// 사발에 재료가 3개 채워질 때까지 대기한다.
        /// 대기 중에는 오버레이 입력 차단을 해제해 플레이어가 실제로 재료를 드래그할 수 있게 한다.
        /// </summary>
        private IEnumerator WaitForBowlFilled()
        {
            if (!_craftOpened)
            {
                OpenCraftingBackdrop();
                yield return null;
                yield return null;
            }

            var bowl = Object.FindFirstObjectByType<BowlReceiver>();
            if (bowl == null)
            {
                // 구성 변경으로 BowlReceiver 를 못 찾으면 소프트락을 피하기 위해 그냥 통과.
                yield break;
            }

            SetInteractiveMode(true);
            SetHint("사발에 재료 3개를 모두 담아주세요.");

            while (bowl != null && bowl.Contents.Count < 3)
            {
                yield return null;
            }

            SetInteractiveMode(false);
            SetHint(DefaultHint);
        }

        private enum MinigameKind { Mix, Pour }

        /// <summary>
        /// 실제 미니게임(Mix/Pour)을 띄워 플레이어가 직접 조작해보게 한다.
        /// 미니게임이 성공/실패로 끝나거나 스페이스/엔터/Esc 로 건너뛰면 다음 줄로 진행.
        /// 좌클릭은 미니게임 조작에 쓰이므로 건너뛰기에는 쓰지 않는다.
        /// 저장/실서비스 흐름을 타지 않고 미니게임만 독립 실행하므로 데이터에 영향이 없다.
        /// </summary>
        private IEnumerator PlayMinigame(MinigameKind kind)
        {
            // 제조 화면(배경)을 아직 안 열었다면 열어 미니게임 UI 의 시각 맥락을 만든다.
            if (!_craftOpened)
            {
                OpenCraftingBackdrop();
                // 화면 전환/레이아웃이 적용되도록 한두 프레임 대기.
                yield return null;
                yield return null;
            }

            var mix = kind == MinigameKind.Mix ? Object.FindFirstObjectByType<MixMinigame>() : null;
            var pour = kind == MinigameKind.Pour ? Object.FindFirstObjectByType<PourMinigame>() : null;

            if (mix == null && pour == null)
            {
                // 미니게임을 찾지 못하면(Cafe 구성 변경 등) 그냥 건너뛴다. (소프트락 방지)
                yield break;
            }

            bool finished = false;
            System.Action onDone = () => finished = true;

            // 미니게임 동안에는 오버레이 입력 차단을 풀어 플레이어가 직접 조작할 수 있게 한다.
            SetInteractiveMode(true);
            SetHint("직접 해보세요!  (건너뛰기: 스페이스)");

            if (mix != null)
            {
                mix.Begin(onDone, onDone);
            }
            else
            {
                pour.Begin(onDone, onDone);
            }

            while (!finished)
            {
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Escape))
                {
                    if (mix != null)
                    {
                        mix.Cancel();
                    }
                    else
                    {
                        pour.Cancel();
                    }

                    break;
                }

                bool running = mix != null ? mix.IsRunning : pour.IsRunning;
                if (!running)
                {
                    break;
                }

                yield return null;
            }

            SetInteractiveMode(false);
            SetHint(DefaultHint);
        }

        // 미니게임 조작을 위해 오버레이의 입력 차단을 일시적으로 풀고 배경을 밝힌다.
        private void SetInteractiveMode(bool interactive)
        {
            if (_overlayGroup != null)
            {
                // false 면 오버레이 전체가 레이캐스트를 무시 -> 미니게임으로 입력 전달.
                _overlayGroup.blocksRaycasts = !interactive;
            }

            if (_dim != null)
            {
                var c = _dim.color;
                c.a = interactive ? Mathf.Min(dimAlpha, 0.2f) : dimAlpha;
                _dim.color = c;
            }

            if (interactive && _highlight != null)
            {
                _highlight.gameObject.SetActive(false);
            }
        }

        private void SetHint(string text)
        {
            if (_hintText != null)
            {
                _hintText.text = text;
            }

            if (_popupHintText != null)
            {
                _popupHintText.text = text;
            }
        }

        // ---------- 강조(하이라이트) ----------

        private string _activeHighlight = string.Empty;

        private void ApplyHighlight(string keyword)
        {
            _activeHighlight = keyword ?? string.Empty;
            RepositionHighlight();
        }

        private void RepositionHighlight()
        {
            if (_highlight == null)
            {
                return;
            }

            var target = ResolveHighlight(_activeHighlight);
            if (target == null)
            {
                _highlight.gameObject.SetActive(false);
                return;
            }

            var canvas = target.GetComponentInParent<Canvas>();
            Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                ? canvas.worldCamera
                : null;

            var corners = new Vector3[4];
            target.GetWorldCorners(corners);

            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);

            for (int i = 0; i < 4; i++)
            {
                Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, corners[i]);
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_overlayRect, screen, null, out Vector2 local))
                {
                    continue;
                }

                min = Vector2.Min(min, local);
                max = Vector2.Max(max, local);
            }

            if (min.x > max.x || min.y > max.y)
            {
                _highlight.gameObject.SetActive(false);
                return;
            }

            const float padding = 16f;
            _highlight.gameObject.SetActive(true);
            _highlight.anchoredPosition = (min + max) * 0.5f;
            _highlight.sizeDelta = (max - min) + new Vector2(padding * 2f, padding * 2f);
        }

        private RectTransform ResolveHighlight(string keyword)
        {
            switch (keyword)
            {
                case "tray":
                    return RectOf(Object.FindFirstObjectByType<TrayController>());
                case "bowl":
                    return RectOf(Object.FindFirstObjectByType<BowlReceiver>());
                case "pestle":
                    return RectOf(Object.FindFirstObjectByType<PestleMixer>());
                case "teaware":
                    return RectOf(Object.FindFirstObjectByType<TeawarePour>());
                case "recall":
                    var popup = Object.FindFirstObjectByType<OrderRecallPopup>();
                    return popup != null ? popup.ToggleRect : null;
                default:
                    // book / seat / 빈 값 등은 강조 대상이 없으므로 생략.
                    return null;
            }
        }

        private static RectTransform RectOf(Component component)
        {
            return component != null ? component.transform as RectTransform : null;
        }

        // ---------- 종료 ----------

        private void FinishTutorial()
        {
            TutorialContext.End();

            var gm = GameManager.Instance;
            if (gm != null && gm.Router != null)
            {
                // 단일 로드로 깨끗한 실제 Cafe 를 띄운다 -> 정상 서비스/저장 재개.
                gm.Router.Load(SceneRouter.SceneCafe);
            }
            else
            {
                // 안전망: 라우터가 없으면 직접 단일 로드.
                SceneManager.LoadScene(cafeSceneName, LoadSceneMode.Single);
            }
        }

        // ---------- 오버레이 UI 생성 (코드 전용, Cafe 씬 미수정) ----------

        private void BuildOverlay()
        {
            var canvasGo = new GameObject("TutorialOverlay", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
            canvasGo.transform.SetParent(transform, false);

            _overlayGroup = canvasGo.GetComponent<CanvasGroup>();

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000; // Cafe UI 위, SceneRouter 페이더 아래.

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            _overlayRect = canvasGo.GetComponent<RectTransform>();

            // 1) 어둡게 가리는 전체 화면 이미지 (raycastTarget=true 로 Cafe 입력 차단).
            _dim = CreateImage("Dim", _overlayRect, new Color(0f, 0f, 0f, dimAlpha));
            Stretch(_dim.rectTransform);
            _dim.raycastTarget = true;

            // 2) 강조 프레임 (입력 비차단). 처음엔 숨김.
            var highlightImg = CreateImage("Highlight", _overlayRect, new Color(1f, 0.92f, 0.32f, 0.22f));
            highlightImg.raycastTarget = false;
            _highlight = highlightImg.rectTransform;
            _highlight.anchorMin = new Vector2(0.5f, 0.5f);
            _highlight.anchorMax = new Vector2(0.5f, 0.5f);
            _highlight.pivot = new Vector2(0.5f, 0.5f);
            _highlight.sizeDelta = new Vector2(160f, 160f);
            _highlight.gameObject.SetActive(false);

            // 3) 대화창 패널 (하단) — TalkDialog 타입에 사용.
            var panel = CreateImage("DialoguePanel", _overlayRect, new Color(0.08f, 0.06f, 0.05f, 0.88f));
            var panelRect = panel.rectTransform;
            panelRect.anchorMin = new Vector2(0.08f, 0.04f);
            panelRect.anchorMax = new Vector2(0.92f, 0.30f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            panel.raycastTarget = true;
            _dialogPanelRect = panelRect;

            _speakerPortrait = CreateImage("SpeakerPortrait", panelRect, Color.white);
            _speakerPortrait.raycastTarget = false;
            var portraitRect = _speakerPortrait.rectTransform;
            portraitRect.anchorMin = new Vector2(0f, 0f);
            portraitRect.anchorMax = new Vector2(0f, 0f);
            portraitRect.pivot = new Vector2(0f, 0f);
            portraitRect.anchoredPosition = new Vector2(20f, 293f);
            portraitRect.sizeDelta = new Vector2(170f, 220f);
            _speakerPortrait.preserveAspect = true;
            _speakerPortrait.gameObject.SetActive(false);

            // 화자 이름.
            _speakerText = CreateText("Speaker", panelRect, 34, TextAnchor.UpperLeft, new Color(1f, 0.88f, 0.55f, 1f));
            var spRect = _speakerText.rectTransform;
            spRect.anchorMin = new Vector2(0f, 1f);
            spRect.anchorMax = new Vector2(1f, 1f);
            spRect.pivot = new Vector2(0.5f, 1f);
            spRect.sizeDelta = new Vector2(-220f, 48f);
            spRect.anchoredPosition = new Vector2(0f, -16f);
            _speakerText.fontStyle = FontStyle.Bold;

            // 본문 대사.
            _bodyText = CreateText("Body", panelRect, 30, TextAnchor.UpperLeft, Color.white);
            var bodyRect = _bodyText.rectTransform;
            bodyRect.anchorMin = new Vector2(0f, 0f);
            bodyRect.anchorMax = new Vector2(1f, 1f);
            bodyRect.offsetMin = new Vector2(210f, 24f);
            bodyRect.offsetMax = new Vector2(-28f, -72f);

            // 진행 안내.
            var hintText = CreateText("Hint", panelRect, 24, TextAnchor.LowerRight, new Color(1f, 1f, 1f, 0.7f));
            hintText.text = "클릭 / 스페이스로 계속 \u25B6";
            var hintRect = hintText.rectTransform;
            hintRect.anchorMin = new Vector2(0f, 0f);
            hintRect.anchorMax = new Vector2(1f, 0f);
            hintRect.pivot = new Vector2(0.5f, 0f);
            hintRect.sizeDelta = new Vector2(-28f, 36f);
            hintRect.anchoredPosition = new Vector2(0f, 12f);
            _hintText = hintText;
            _hint = hintText.gameObject;

            // 4) PositionedPopup 패널 — 화면 임의 위치 팝업. 처음엔 숨김.
            var popupImg = CreateImage("PopupPanel", _overlayRect, new Color(0.08f, 0.06f, 0.05f, 0.92f));
            _popupPanelRect = popupImg.rectTransform;
            // 초기 앵커는 화면 중앙. PositionPopupPanel() 이 매 ShowLine 에서 갱신한다.
            _popupPanelRect.anchorMin = new Vector2(0.5f, 0.5f);
            _popupPanelRect.anchorMax = new Vector2(0.5f, 0.5f);
            _popupPanelRect.pivot = new Vector2(0.5f, 0.5f);
            _popupPanelRect.sizeDelta = new Vector2(520f, 180f);
            _popupPanelRect.anchoredPosition = Vector2.zero;
            popupImg.raycastTarget = true;

            _popupPortrait = CreateImage("PopupPortrait", _popupPanelRect, Color.white);
            _popupPortrait.raycastTarget = false;
            var popupPortraitRect = _popupPortrait.rectTransform;
            popupPortraitRect.anchorMin = new Vector2(0f, 0f);
            popupPortraitRect.anchorMax = new Vector2(0f, 0f);
            popupPortraitRect.pivot = new Vector2(0f, 0f);
            popupPortraitRect.anchoredPosition = new Vector2(16f, 16f);
            popupPortraitRect.sizeDelta = new Vector2(96f, 128f);
            _popupPortrait.preserveAspect = true;
            _popupPortrait.gameObject.SetActive(false);

            // 팝업 화자 이름.
            _popupSpeakerText = CreateText("PopupSpeaker", _popupPanelRect, 30, TextAnchor.UpperLeft, new Color(1f, 0.88f, 0.55f, 1f));
            _popupSpeakerText.fontStyle = FontStyle.Bold;
            var popupSpRect = _popupSpeakerText.rectTransform;
            popupSpRect.anchorMin = new Vector2(0f, 1f);
            popupSpRect.anchorMax = new Vector2(1f, 1f);
            popupSpRect.pivot = new Vector2(0.5f, 1f);
            popupSpRect.sizeDelta = new Vector2(-130f, 40f);
            popupSpRect.anchoredPosition = new Vector2(0f, -12f);

            // 팝업 본문 대사.
            _popupBodyText = CreateText("PopupBody", _popupPanelRect, 28, TextAnchor.UpperLeft, Color.white);
            var popupBodyRect = _popupBodyText.rectTransform;
            popupBodyRect.anchorMin = new Vector2(0f, 0f);
            popupBodyRect.anchorMax = new Vector2(1f, 1f);
            popupBodyRect.offsetMin = new Vector2(124f, 32f);
            popupBodyRect.offsetMax = new Vector2(-20f, -56f);

            // 팝업 진행 안내.
            _popupHintText = CreateText("PopupHint", _popupPanelRect, 22, TextAnchor.LowerRight, new Color(1f, 1f, 1f, 0.7f));
            _popupHintText.text = DefaultHint;
            var popupHintRect = _popupHintText.rectTransform;
            popupHintRect.anchorMin = new Vector2(0f, 0f);
            popupHintRect.anchorMax = new Vector2(1f, 0f);
            popupHintRect.pivot = new Vector2(0.5f, 0f);
            popupHintRect.sizeDelta = new Vector2(-20f, 32f);
            popupHintRect.anchoredPosition = new Vector2(0f, 8f);

            _popupPanelRect.gameObject.SetActive(false);
        }

        private static void ApplySpeakerPortrait(Image target, TutorialLine line)
        {
            if (target == null)
            {
                return;
            }

            bool visible = !string.IsNullOrWhiteSpace(line.speaker) && line.illust != null;
            target.gameObject.SetActive(visible);
            if (!visible)
            {
                target.sprite = null;
                return;
            }

            target.sprite = line.illust;
            target.SetNativeSize();
        }

        private static Image CreateImage(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = color;
            return img;
        }

        private static Text CreateText(string name, Transform parent, int fontSize, TextAnchor anchor, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;

            // 한글이 보이도록 임베디드 한글 폰트를 즉시 적용(이후 KoreanFontApplier 도 재적용).
            var korean = KoreanFontApplier.Font;
            if (korean != null)
            {
                text.font = korean;
            }

            return text;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
