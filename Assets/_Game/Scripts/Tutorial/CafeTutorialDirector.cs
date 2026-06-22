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
    public class CafeTutorialDirector : MonoBehaviour
    {
        [SerializeField] private string cafeSceneName = "Cafe";
        [SerializeField] private string csvResourcePath = "Cafe/cafe_tutorial";
        [SerializeField] private Sprite _popupBgSprite;
        [Range(0f, 1f)] [SerializeField] private float dimAlpha = 0.72f;

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

        private RectTransform _dialogPanelRect;

        private RectTransform _popupPanelRect;
        private Image _popupPortrait;
        private Text _popupSpeakerText;
        private Text _popupBodyText;
        private Text _popupHintText;

        private bool _craftOpened;

        private GameObject _spawnedInstance;
        private string _spawnedKey = string.Empty;

        private const string DefaultHint = "클릭 / 스페이스로 계속";

        private void Awake()
        {
            TutorialContext.Begin();
        }

        private void Start()
        {
            BuildOverlay();
            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
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

            yield return null;

            var lines = CafeTutorialCsvLoader.Load(csvResourcePath);
            if (lines == null || lines.Count == 0)
            {
                FinishTutorial();
                yield break;
            }

            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                ShowLine(line);

                yield return ApplySpawnPrefab(line.spawnPrefab);

                yield return WaitForAdvance();

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

            yield return DespawnPrefab();

            FinishTutorial();
        }

        private void ShowLine(TutorialLine line)
        {
            bool isPopup = line.uiType == TutorialUiType.PositionedPopup;

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
                crafting.BeginCraft(null);
            }
        }


        private IEnumerator ApplySpawnPrefab(string resourcePath)
        {
            string desired = string.IsNullOrWhiteSpace(resourcePath) ? string.Empty : resourcePath;

            if (_spawnedInstance != null && _spawnedKey == desired)
            {
                yield break;
            }

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
                Debug.LogWarning($"CafeTutorialDirector: 스폰 프리팹을 찾을 수 없습니다: {desired}");
                yield break;
            }

            _spawnedInstance = Instantiate(prefab);
            _spawnedKey = desired;
        }

        private IEnumerator DespawnPrefab()
        {
            var instance = _spawnedInstance;
            _spawnedInstance = null;
            _spawnedKey = string.Empty;

            yield return FadeOutAndDestroy(instance);
        }

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

        private IEnumerator PlayMinigame(MinigameKind kind)
        {
            if (!_craftOpened)
            {
                OpenCraftingBackdrop();
                yield return null;
                yield return null;
            }

            var mix = kind == MinigameKind.Mix ? Object.FindFirstObjectByType<MixMinigame>() : null;
            var pour = kind == MinigameKind.Pour ? Object.FindFirstObjectByType<PourMinigame>() : null;

            if (mix == null && pour == null)
            {
                yield break;
            }

            bool finished = false;
            System.Action onDone = () => finished = true;

            SetInteractiveMode(true);
            SetHint("직접 해보세요! (건너뛰기: 스페이스)");

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

        private void SetInteractiveMode(bool interactive)
        {
            if (_overlayGroup != null)
            {
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
                    return null;
            }
        }

        private static RectTransform RectOf(Component component)
        {
            return component != null ? component.transform as RectTransform : null;
        }


        private void FinishTutorial()
        {
            TutorialContext.End();

            var gm = GameManager.Instance;
            if (gm != null && gm.Router != null)
            {
                gm.Router.Load(SceneRouter.SceneCafe);
            }
            else
            {
                SceneManager.LoadScene(cafeSceneName, LoadSceneMode.Single);
            }
        }


        private void BuildOverlay()
        {
            var canvasGo = new GameObject("TutorialOverlay", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
            canvasGo.transform.SetParent(transform, false);

            _overlayGroup = canvasGo.GetComponent<CanvasGroup>();

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            _overlayRect = canvasGo.GetComponent<RectTransform>();

            _dim = CreateImage("Dim", _overlayRect, new Color(0f, 0f, 0f, dimAlpha));
            Stretch(_dim.rectTransform);
            _dim.raycastTarget = true;

            var highlightImg = CreateImage("Highlight", _overlayRect, new Color(1f, 0.92f, 0.32f, 0.22f));
            highlightImg.raycastTarget = false;
            _highlight = highlightImg.rectTransform;
            _highlight.anchorMin = new Vector2(0.5f, 0.5f);
            _highlight.anchorMax = new Vector2(0.5f, 0.5f);
            _highlight.pivot = new Vector2(0.5f, 0.5f);
            _highlight.sizeDelta = new Vector2(160f, 160f);
            _highlight.gameObject.SetActive(false);

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

            _speakerText = CreateText("Speaker", panelRect, 34, TextAnchor.UpperLeft, new Color(1f, 0.88f, 0.55f, 1f));
            var spRect = _speakerText.rectTransform;
            spRect.anchorMin = new Vector2(0f, 1f);
            spRect.anchorMax = new Vector2(1f, 1f);
            spRect.pivot = new Vector2(0.5f, 1f);
            spRect.sizeDelta = new Vector2(-220f, 48f);
            spRect.anchoredPosition = new Vector2(0f, -16f);
            _speakerText.fontStyle = FontStyle.Bold;

            _bodyText = CreateText("Body", panelRect, 46, TextAnchor.UpperLeft, Color.white);
            var bodyRect = _bodyText.rectTransform;
            bodyRect.anchorMin = new Vector2(0f, 0f);
            bodyRect.anchorMax = new Vector2(1f, 1f);
            bodyRect.offsetMin = new Vector2(210f, 24f);
            bodyRect.offsetMax = new Vector2(-28f, -72f);

            var hintText = CreateText("Hint", panelRect, 24, TextAnchor.LowerRight, new Color(1f, 1f, 1f, 0.7f));
            hintText.text = "클릭 / 스페이스로 계속";
            var hintRect = hintText.rectTransform;
            hintRect.anchorMin = new Vector2(0f, 0f);
            hintRect.anchorMax = new Vector2(1f, 0f);
            hintRect.pivot = new Vector2(0.5f, 0f);
            hintRect.sizeDelta = new Vector2(-28f, 36f);
            hintRect.anchoredPosition = new Vector2(0f, 12f);
            _hintText = hintText;
            _hint = hintText.gameObject;

            var popupImg = CreateImage("PopupPanel", _overlayRect, Color.white);
            if (_popupBgSprite != null) { popupImg.sprite = _popupBgSprite; popupImg.type = Image.Type.Sliced; }
            _popupPanelRect = popupImg.rectTransform;
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

            _popupSpeakerText = CreateText("PopupSpeaker", _popupPanelRect, 30, TextAnchor.UpperLeft, new Color(1f, 0.88f, 0.55f, 1f));
            _popupSpeakerText.fontStyle = FontStyle.Bold;
            var popupSpRect = _popupSpeakerText.rectTransform;
            popupSpRect.anchorMin = new Vector2(0f, 1f);
            popupSpRect.anchorMax = new Vector2(1f, 1f);
            popupSpRect.pivot = new Vector2(0.5f, 1f);
            popupSpRect.sizeDelta = new Vector2(-130f, 40f);
            popupSpRect.anchoredPosition = new Vector2(0f, -12f);

            _popupBodyText = CreateText("Body", _popupPanelRect, 30, TextAnchor.UpperLeft, Color.white);
            var popupBodyRect = _popupBodyText.rectTransform;
            popupBodyRect.anchorMin = new Vector2(0f, 0f);
            popupBodyRect.anchorMax = new Vector2(1f, 1f);
            popupBodyRect.offsetMin = new Vector2(124f, 32f);
            popupBodyRect.offsetMax = new Vector2(-20f, -56f);

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
