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
    /// ì¹´í˜ ?Šœ?† ë¦¬ì–¼ ?—°ì¶? ê°ë….
    ///
    /// ?„¤ê³? ?š”?•½ (?š”êµ¬ì‚¬?•­ ????‘):
    /// - ?‹¤? œ Cafe ?”¬?„ Additive ë¡? ?„?›Œ "?‚´?•„?ˆ?Š” ë°°ê²½"?œ¼ë¡? ?‚¬?š©?•œ?‹¤. ?”°?¼?„œ Cafe ?”¬?´
    ///   ë°”ë?Œì–´?„ ?Šœ?† ë¦¬ì–¼?´ ??™?œ¼ë¡? ê·? ë³?ê²½ì„ ë°˜ì˜?•œ?‹¤. (Cafe ë³?ê²½ì—?„ ?Šœ?† ë¦¬ì–¼ ? •?ƒ ?‘?™)
    /// - ëª¨ë“  ?˜¤ë²„ë ˆ?´ UI(?–´?‘¡ê²? ê°?ë¦¬ê¸° + ê°•ì¡° ?”„? ˆ?„ + ????™”ì°?)?Š” ì½”ë“œë¡œë§Œ ?ƒ?„±?•˜ë¯?ë¡?
    ///   Cafe ?”¬ ?ì²´ëŠ” ? „??? ?ˆ˜? •?•˜ì§? ?•Š?Š”?‹¤. (Cafe ?”¬??? ?Šœ?† ë¦¬ì–¼ê³? ë¬´ê???•˜ê²? ? •?ƒ ?‘?™)
    /// - ì§„í–‰ ì¤‘ì—?Š” ?…? ¥?„ ë§‰ê³  ?‹¤? œ ?„œë¹„ìŠ¤(?†?‹˜ ?‘???/????¥)?Š” ? ˆ??? ?Œë¦¬ì?? ?•Š?Š”?‹¤.
    ///   <see cref="TutorialContext"/> ê°? DayManager ?˜ ??™ ?‹œ?‘/????¥?„ ì°¨ë‹¨?•œ?‹¤.
    ///   (?Šœ?† ë¦¬ì–¼ ê²°ê³¼ê°? ?‹¤? œ ?°?´?„°?— ?˜?–¥/????¥?˜ì§? ?•Š?Œ)
    /// - ê°•ì¡° ????ƒ??? ì»´í¬?„Œ?Š¸ "????…"?œ¼ë¡? ì°¾ëŠ”?‹¤(FindFirstObjectByType). Cafe ?˜ ê³„ì¸µ êµ¬ì¡°ê°?
    ///   ë°”ë?Œì–´?„ ?„êµ¬ë?? ê³„ì† ì°¾ì•„?‚¸?‹¤. ëª? ì°¾ìœ¼ë©? ê°•ì¡° ?ƒ?µ.
    /// - ?Šœ?† ë¦¬ì–¼?´ ??‚˜ë©? Cafe ?”¬?„ ?‹¨?¼ ë¡œë“œë¡? ?ƒˆë¡? ?„?›Œ ê¹¨ë—?•œ ?‹¤? œ ê²Œì„?„ ?‹œ?‘?•œ?‹¤.
    /// </summary>
    public class CafeTutorialDirector : MonoBehaviour
    {
        [Tooltip("Additive ë¡? ë°°ê²½?— ?„?š¸ ?‹¤? œ ì¹´í˜ ?”¬ ?´ë¦?.")]
        [SerializeField] private string cafeSceneName = "Cafe";
        [Tooltip("?Šœ?† ë¦¬ì–¼ ????‚¬ CSV ?˜ Resources ê²½ë¡œ(?™•?¥? ? œ?™¸).")]
        [SerializeField] private string csvResourcePath = "Cafe/cafe_tutorial";
        [SerializeField] private Sprite _popupBgSprite;
        [Tooltip("ë°°ê²½(Cafe)?„ ?–´?‘¡ê²? ê°?ë¦¬ëŠ” ? •?„. 1?— ê°?ê¹Œìš¸?ˆ˜ë¡? ?–´?‘¡?‹¤.")]
        [Range(0f, 1f)] [SerializeField] private float dimAlpha = 0.72f;

        // ê°•ì¡° ????ƒ?´ ì°? ? œì¡? ?„êµ¬ì¼ ?•Œ, ?‹¤? œ ? œì¡? ?™”ë©´ì„ ë°°ê²½?œ¼ë¡? ?—´?–´ ?„êµ¬ê?? ë³´ì´?„ë¡? ?•œ?‹¤.
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

        // TalkDialog ?Œ¨?„ (?•˜?‹¨ ê³ ì • ????™”ì°?)
        private RectTransform _dialogPanelRect;

        // PositionedPopup ?Œ¨?„ (?™”ë©? ?„?˜ ?œ„ì¹? ?Œ?—…)
        private RectTransform _popupPanelRect;
        private Image _popupPortrait;
        private Text _popupSpeakerText;
        private Text _popupBodyText;
        private Text _popupHintText;

        private bool _craftOpened;

        // ?˜„?¬ ?Š¤?°?˜?–´ ????™” ?™?•ˆ ?œ ì§? ì¤‘ì¸ ?”„ë¦¬íŒ¹ ?¸?Š¤?„´?Š¤??? ê·? Resources ?‚¤.
        private GameObject _spawnedInstance;
        private string _spawnedKey = string.Empty;

        private const string DefaultHint = "?´ë¦? / ?Š¤?˜?´?Š¤ë¡? ê³„ì† ?–¶";

        private void Awake()
        {
            // ?´ ?‹œ? ë¶??„° DayManager ?˜ ??™ ?„œë¹„ìŠ¤/????¥?´ ì°¨ë‹¨?œ?‹¤.
            TutorialContext.Begin();
        }

        private void Start()
        {
            BuildOverlay();
            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            // 1) ?‹¤? œ Cafe ?”¬?„ Additive ë¡? ë¡œë“œ (ë°°ê²½ + ê°•ì¡° ?•µì»?).
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

            // Cafe ?˜ Awake/Start(ê³? ?°?Š¸ ? ?š©)ê°? ?•œ ë²? ?Œ?„ë¡? ?•œ ?”„? ˆ?„ ???ê¸?.
            yield return null;

            var lines = CafeTutorialCsvLoader.Load(csvResourcePath);
            if (lines == null || lines.Count == 0)
            {
                FinishTutorial();
                yield break;
            }

            // 2) ????‚¬ë¥? ?ˆœ?„œ???ë¡? ?¬?ƒ. ê°? ì¤„ì?? ?´ë¦?/?Š¤?˜?´?Š¤ë¡? ì§„í–‰.
            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                ShowLine(line);

                // ?´ ì¤„ì´ ì§?? •?•œ ?Š¤?° ?”„ë¦¬íŒ¹?„ ë°˜ì˜(?—†?œ¼ë©? ì§ì „ ?Š¤?°?„ ê·¸ë??ë¡? ?œ ì§?).
                yield return ApplySpawnPrefab(line.spawnPrefab);

                yield return WaitForAdvance();

                // ë¯¸ë‹ˆê²Œì„ ?•ˆ?‚´ ì¤?: ?‹¤? œ ë¯¸ë‹ˆê²Œì„?„ ?„?›Œ ?”Œ? ˆ?´?–´ê°? ì§ì ‘ ?•´ë³´ê²Œ ?•œ ?’¤ ?‹¤?Œ ì¤„ë¡œ ì§„í–‰.
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

            // ?Šœ?† ë¦¬ì–¼?´ ??‚˜ë©? ?‚¨?•„ ?ˆ?Š” ?Š¤?° ?”„ë¦¬íŒ¹?„ (?Š¸?œˆ?´ ?ˆ?œ¼ë©? ??‚œ ?’¤) ? œê±?.
            yield return DespawnPrefab();

            FinishTutorial();
        }

        private void ShowLine(TutorialLine line)
        {
            bool isPopup = line.uiType == TutorialUiType.PositionedPopup;

            // ?™œ?„± ?Œ¨?„ ? „?™˜.
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
                // PositionedPopup: ì§?? • ?œ„ì¹˜ì— ?Œ?—…?„ ë°°ì¹˜?•œ?‹¤.
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
                // TalkDialog: ?•˜?‹¨ ê³ ì • ????™”ì°?.
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

            // ì°? ? œì¡? ?„êµ¬ë?? ê°•ì¡°?•´?•¼ ?•˜ë©? ?‹¤? œ ? œì¡? ?™”ë©´ì„ ë°°ê²½?œ¼ë¡? ?—°?‹¤(?…? ¥??? ë§‰í?? ?ˆ?Œ).
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
            // ì§ì „ ?™”ë©?(?˜ˆ: cafe_dialog ë§ˆì??ë§? ?´ë¦?)?˜ ?…? ¥?´ ì¦‰ì‹œ ?‹¤?Œ ì¤„ë¡œ ?„˜?–´ê°?ì§? ?•Š?„ë¡? ?•½ê°? ???ê¸?.
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
                // targetRecipe ?—†?´ ?‹œê°ì  ë°°ê²½?œ¼ë¡œë§Œ ? œì¡? ?™”ë©´ì„ ?—°?‹¤. ?…? ¥?´ ë§‰í?? ?ˆ?–´
                // ?‹¤? œ ? œì¡?/?Œ? •/????¥??? ?¼?–´?‚˜ì§? ?•Š?Š”?‹¤.
                crafting.BeginCraft(null);
            }
        }

        // ---------- ?Š¤?° ?”„ë¦¬íŒ¹ (????™” ?™?•ˆ ?œ ì§??˜?Š” ?”„ë¦¬íŒ¹) ----------

        /// <summary>
        /// ?•œ ì¤„ì´ ì§?? •?•œ ?Š¤?° ?”„ë¦¬íŒ¹?„ ë°˜ì˜?•œ?‹¤.
        /// - ê°™ì?? ?”„ë¦¬íŒ¹ ?‚¤ê°? ?´ë¯? ?–  ?ˆ?œ¼ë©?: ê·¸ë??ë¡? ?œ ì§?(?—¬?Ÿ¬ ì¤„ì— ê±¸ì³ ?œ ì§??•˜? ¤ë©? ê°™ì?? ê°’ì„ ë°˜ë³µ ì§?? •).
        /// - ê·? ?™¸(ë¹? ê°? ?¬?•¨, ?‹¤ë¥? ê°?)?´ë©?: ì§ì „ ?Š¤?°?„ ? œê±°í•œ?‹¤. ?´?•Œ ?‚¬?¼ì§??Š” ?Š¸?œˆ?´ ?ˆ?œ¼ë©?
        ///   ?Š¸?œˆ?„ ë°±ê·¸?¼?š´?“œë¡? ?¬?ƒ?•˜ê³? ??‚œ ?’¤ ?ŒŒê´´í•˜ë¯?ë¡? ????™” ì§„í–‰?„ ë§‰ì?? ?•Š?Š”?‹¤.
        /// - ?ƒˆ ê°’ì´ ë¹„ì–´ ?ˆì§? ?•Š?œ¼ë©? ?ƒˆ ?”„ë¦¬íŒ¹?„ ?Š¤?°?•œ?‹¤.
        /// ì¦?, ?Š¤?° ?”„ë¦¬íŒ¹??? ??‹ ?„ ì§?? •?•œ ì¤?(????™”) ?™?•ˆ ?œ ì§??˜ê³?, ????™”ê°? ?„˜?–´ê°?ë©? ?‚¬?¼ì§„ë‹¤.
        /// </summary>
        private IEnumerator ApplySpawnPrefab(string resourcePath)
        {
            string desired = string.IsNullOrWhiteSpace(resourcePath) ? string.Empty : resourcePath;

            // ê°™ì?? ?”„ë¦¬íŒ¹?´ ?´ë¯? ?–  ?ˆ?œ¼ë©? ?œ ì§?.
            if (_spawnedInstance != null && _spawnedKey == desired)
            {
                yield break;
            }

            // ì§ì „ ?Š¤?°??? (?‚¬?¼ì§??Š” ?Š¸?œˆ?´ ?ˆ?œ¼ë©? ??‚œ ?’¤) ë°±ê·¸?¼?š´?“œë¡? ? œê±°í•œ?‹¤.
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
                Debug.LogWarning($"CafeTutorialDirector: ?Š¤?° ?”„ë¦¬íŒ¹ '{desired}' ë¥? ì°¾ì„ ?ˆ˜ ?—†?Šµ?‹ˆ?‹¤.");
                yield break;
            }

            _spawnedInstance = Instantiate(prefab);
            _spawnedKey = desired;
        }

        /// <summary>
        /// ?˜„?¬ ?Š¤?°?œ ?”„ë¦¬íŒ¹?„ ? œê±°í•œ?‹¤(?Šœ?† ë¦¬ì–¼ ì¢…ë£Œ ?‹œ). <see cref="ITutorialSpawnDisappear"/> ê°? ?ˆ?œ¼ë©?
        /// ?‚¬?¼ì§??Š” ?Š¸?œˆ?„ ?¬?ƒ?•˜ê³? ??‚  ?•Œê¹Œì?? ê¸°ë‹¤ë¦? ?’¤ ?ŒŒê´´í•œ?‹¤.
        /// </summary>
        private IEnumerator DespawnPrefab()
        {
            var instance = _spawnedInstance;
            _spawnedInstance = null;
            _spawnedKey = string.Empty;

            yield return FadeOutAndDestroy(instance);
        }

        /// <summary>
        /// ?¸?Š¤?„´?Š¤?— ?‚¬?¼ì§??Š” ?Š¸?œˆ?´ ?ˆ?œ¼ë©? ?ê¹Œì?? ?¬?ƒ?•œ ?’¤ ?ŒŒê´´í•œ?‹¤. ?—†?œ¼ë©? ì¦‰ì‹œ ?ŒŒê´´í•œ?‹¤.
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

        // ---------- ë¯¸ë‹ˆê²Œì„ ì²´í—˜ (waitMiniGame*talkUI ?•¡?…˜) ----------

        /// <summary>
        /// ?‚¬ë°œì— ?¬ë£Œê?? 3ê°? ì±„ì›Œì§? ?•Œê¹Œì?? ???ê¸°í•œ?‹¤.
        /// ???ê¸? ì¤‘ì—?Š” ?˜¤ë²„ë ˆ?´ ?…? ¥ ì°¨ë‹¨?„ ?•´? œ?•´ ?”Œ? ˆ?´?–´ê°? ?‹¤? œë¡? ?¬ë£Œë?? ?“œ?˜ê·¸í•  ?ˆ˜ ?ˆê²? ?•œ?‹¤.
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
                // êµ¬ì„± ë³?ê²½ìœ¼ë¡? BowlReceiver ë¥? ëª? ì°¾ìœ¼ë©? ?†Œ?”„?Š¸?½?„ ?”¼?•˜ê¸? ?œ„?•´ ê·¸ëƒ¥ ?†µê³?.
                yield break;
            }

            SetInteractiveMode(true);
            SetHint("?‚¬ë°œì— ?¬ë£? 3ê°œë?? ëª¨ë‘ ?‹´?•„ì£¼ì„¸?š”.");

            while (bowl != null && bowl.Contents.Count < 3)
            {
                yield return null;
            }

            SetInteractiveMode(false);
            SetHint(DefaultHint);
        }

        private enum MinigameKind { Mix, Pour }

        /// <summary>
        /// ?‹¤? œ ë¯¸ë‹ˆê²Œì„(Mix/Pour)?„ ?„?›Œ ?”Œ? ˆ?´?–´ê°? ì§ì ‘ ì¡°ì‘?•´ë³´ê²Œ ?•œ?‹¤.
        /// ë¯¸ë‹ˆê²Œì„?´ ?„±ê³?/?‹¤?Œ¨ë¡? ??‚˜ê±°ë‚˜ ?Š¤?˜?´?Š¤/?—”?„°/Esc ë¡? ê±´ë„ˆ?›°ë©? ?‹¤?Œ ì¤„ë¡œ ì§„í–‰.
        /// ì¢Œí´ë¦???? ë¯¸ë‹ˆê²Œì„ ì¡°ì‘?— ?“°?´ë¯?ë¡? ê±´ë„ˆ?›°ê¸°ì—?Š” ?“°ì§? ?•Š?Š”?‹¤.
        /// ????¥/?‹¤?„œë¹„ìŠ¤ ?ë¦„ì„ ???ì§? ?•Šê³? ë¯¸ë‹ˆê²Œì„ë§? ?…ë¦? ?‹¤?–‰?•˜ë¯?ë¡? ?°?´?„°?— ?˜?–¥?´ ?—†?‹¤.
        /// </summary>
        private IEnumerator PlayMinigame(MinigameKind kind)
        {
            // ? œì¡? ?™”ë©?(ë°°ê²½)?„ ?•„ì§? ?•ˆ ?—´?—ˆ?‹¤ë©? ?—´?–´ ë¯¸ë‹ˆê²Œì„ UI ?˜ ?‹œê°? ë§¥ë½?„ ë§Œë“ ?‹¤.
            if (!_craftOpened)
            {
                OpenCraftingBackdrop();
                // ?™”ë©? ? „?™˜/? ˆ?´?•„?›ƒ?´ ? ?š©?˜?„ë¡? ?•œ?‘ ?”„? ˆ?„ ???ê¸?.
                yield return null;
                yield return null;
            }

            var mix = kind == MinigameKind.Mix ? Object.FindFirstObjectByType<MixMinigame>() : null;
            var pour = kind == MinigameKind.Pour ? Object.FindFirstObjectByType<PourMinigame>() : null;

            if (mix == null && pour == null)
            {
                // ë¯¸ë‹ˆê²Œì„?„ ì°¾ì?? ëª»í•˜ë©?(Cafe êµ¬ì„± ë³?ê²? ?“±) ê·¸ëƒ¥ ê±´ë„ˆ?›´?‹¤. (?†Œ?”„?Š¸?½ ë°©ì??)
                yield break;
            }

            bool finished = false;
            System.Action onDone = () => finished = true;

            // ë¯¸ë‹ˆê²Œì„ ?™?•ˆ?—?Š” ?˜¤ë²„ë ˆ?´ ?…? ¥ ì°¨ë‹¨?„ ????–´ ?”Œ? ˆ?´?–´ê°? ì§ì ‘ ì¡°ì‘?•  ?ˆ˜ ?ˆê²? ?•œ?‹¤.
            SetInteractiveMode(true);
            SetHint("ì§ì ‘ ?•´ë³´ì„¸?š”!  (ê±´ë„ˆ?›°ê¸?: ?Š¤?˜?´?Š¤)");

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

        // ë¯¸ë‹ˆê²Œì„ ì¡°ì‘?„ ?œ„?•´ ?˜¤ë²„ë ˆ?´?˜ ?…? ¥ ì°¨ë‹¨?„ ?¼?‹œ? ?œ¼ë¡? ???ê³? ë°°ê²½?„ ë°íŒ?‹¤.
        private void SetInteractiveMode(bool interactive)
        {
            if (_overlayGroup != null)
            {
                // false ë©? ?˜¤ë²„ë ˆ?´ ? „ì²´ê?? ? ˆ?´ìºìŠ¤?Š¸ë¥? ë¬´ì‹œ -> ë¯¸ë‹ˆê²Œì„?œ¼ë¡? ?…? ¥ ? „?‹¬.
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

        // ---------- ê°•ì¡°(?•˜?´?¼?´?Š¸) ----------

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
                    // book / seat / ë¹? ê°? ?“±??? ê°•ì¡° ????ƒ?´ ?—†?œ¼ë¯?ë¡? ?ƒ?µ.
                    return null;
            }
        }

        private static RectTransform RectOf(Component component)
        {
            return component != null ? component.transform as RectTransform : null;
        }

        // ---------- ì¢…ë£Œ ----------

        private void FinishTutorial()
        {
            TutorialContext.End();

            var gm = GameManager.Instance;
            if (gm != null && gm.Router != null)
            {
                // ?‹¨?¼ ë¡œë“œë¡? ê¹¨ë—?•œ ?‹¤? œ Cafe ë¥? ?„?š´?‹¤ -> ? •?ƒ ?„œë¹„ìŠ¤/????¥ ?¬ê°?.
                gm.Router.Load(SceneRouter.SceneCafe);
            }
            else
            {
                // ?•ˆ? „ë§?: ?¼?š°?„°ê°? ?—†?œ¼ë©? ì§ì ‘ ?‹¨?¼ ë¡œë“œ.
                SceneManager.LoadScene(cafeSceneName, LoadSceneMode.Single);
            }
        }

        // ---------- ?˜¤ë²„ë ˆ?´ UI ?ƒ?„± (ì½”ë“œ ? „?š©, Cafe ?”¬ ë¯¸ìˆ˜? •) ----------

        private void BuildOverlay()
        {
            var canvasGo = new GameObject("TutorialOverlay", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
            canvasGo.transform.SetParent(transform, false);

            _overlayGroup = canvasGo.GetComponent<CanvasGroup>();

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000; // Cafe UI ?œ„, SceneRouter ?˜?´?” ?•„?˜.

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            _overlayRect = canvasGo.GetComponent<RectTransform>();

            // 1) ?–´?‘¡ê²? ê°?ë¦¬ëŠ” ? „ì²? ?™”ë©? ?´ë¯¸ì?? (raycastTarget=true ë¡? Cafe ?…? ¥ ì°¨ë‹¨).
            _dim = CreateImage("Dim", _overlayRect, new Color(0f, 0f, 0f, dimAlpha));
            Stretch(_dim.rectTransform);
            _dim.raycastTarget = true;

            // 2) ê°•ì¡° ?”„? ˆ?„ (?…? ¥ ë¹„ì°¨?‹¨). ì²˜ìŒ?—” ?ˆ¨ê¹?.
            var highlightImg = CreateImage("Highlight", _overlayRect, new Color(1f, 0.92f, 0.32f, 0.22f));
            highlightImg.raycastTarget = false;
            _highlight = highlightImg.rectTransform;
            _highlight.anchorMin = new Vector2(0.5f, 0.5f);
            _highlight.anchorMax = new Vector2(0.5f, 0.5f);
            _highlight.pivot = new Vector2(0.5f, 0.5f);
            _highlight.sizeDelta = new Vector2(160f, 160f);
            _highlight.gameObject.SetActive(false);

            // 3) ????™”ì°? ?Œ¨?„ (?•˜?‹¨) ??? TalkDialog ????…?— ?‚¬?š©.
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

            // ?™”? ?´ë¦?.
            _speakerText = CreateText("Speaker", panelRect, 34, TextAnchor.UpperLeft, new Color(1f, 0.88f, 0.55f, 1f));
            var spRect = _speakerText.rectTransform;
            spRect.anchorMin = new Vector2(0f, 1f);
            spRect.anchorMax = new Vector2(1f, 1f);
            spRect.pivot = new Vector2(0.5f, 1f);
            spRect.sizeDelta = new Vector2(-220f, 48f);
            spRect.anchoredPosition = new Vector2(0f, -16f);
            _speakerText.fontStyle = FontStyle.Bold;

            // ë³¸ë¬¸ ????‚¬.
            _bodyText = CreateText("Body", panelRect, 46, TextAnchor.UpperLeft, Color.white);
            var bodyRect = _bodyText.rectTransform;
            bodyRect.anchorMin = new Vector2(0f, 0f);
            bodyRect.anchorMax = new Vector2(1f, 1f);
            bodyRect.offsetMin = new Vector2(210f, 24f);
            bodyRect.offsetMax = new Vector2(-28f, -72f);

            // ì§„í–‰ ?•ˆ?‚´.
            var hintText = CreateText("Hint", panelRect, 24, TextAnchor.LowerRight, new Color(1f, 1f, 1f, 0.7f));
            hintText.text = "?´ë¦? / ?Š¤?˜?´?Š¤ë¡? ê³„ì† \u25B6";
            var hintRect = hintText.rectTransform;
            hintRect.anchorMin = new Vector2(0f, 0f);
            hintRect.anchorMax = new Vector2(1f, 0f);
            hintRect.pivot = new Vector2(0.5f, 0f);
            hintRect.sizeDelta = new Vector2(-28f, 36f);
            hintRect.anchoredPosition = new Vector2(0f, 12f);
            _hintText = hintText;
            _hint = hintText.gameObject;

            // 4) PositionedPopup ?Œ¨?„ ??? ?™”ë©? ?„?˜ ?œ„ì¹? ?Œ?—…. ì²˜ìŒ?—” ?ˆ¨ê¹?.
            var popupImg = CreateImage("PopupPanel", _overlayRect, Color.white);
            if (_popupBgSprite != null) { popupImg.sprite = _popupBgSprite; popupImg.type = Image.Type.Sliced; }
            _popupPanelRect = popupImg.rectTransform;
            // ì´ˆê¸° ?•µì»¤ëŠ” ?™”ë©? ì¤‘ì•™. PositionPopupPanel() ?´ ë§? ShowLine ?—?„œ ê°±ì‹ ?•œ?‹¤.
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

            // ?Œ?—… ?™”? ?´ë¦?.
            _popupSpeakerText = CreateText("PopupSpeaker", _popupPanelRect, 30, TextAnchor.UpperLeft, new Color(1f, 0.88f, 0.55f, 1f));
            _popupSpeakerText.fontStyle = FontStyle.Bold;
            var popupSpRect = _popupSpeakerText.rectTransform;
            popupSpRect.anchorMin = new Vector2(0f, 1f);
            popupSpRect.anchorMax = new Vector2(1f, 1f);
            popupSpRect.pivot = new Vector2(0.5f, 1f);
            popupSpRect.sizeDelta = new Vector2(-130f, 40f);
            popupSpRect.anchoredPosition = new Vector2(0f, -12f);

            // ?Œ?—… ë³¸ë¬¸ ????‚¬.
            _popupBodyText = CreateText("Body", _popupPanelRect, 30, TextAnchor.UpperLeft, Color.white);
            var popupBodyRect = _popupBodyText.rectTransform;
            popupBodyRect.anchorMin = new Vector2(0f, 0f);
            popupBodyRect.anchorMax = new Vector2(1f, 1f);
            popupBodyRect.offsetMin = new Vector2(124f, 32f);
            popupBodyRect.offsetMax = new Vector2(-20f, -56f);

            // ?Œ?—… ì§„í–‰ ?•ˆ?‚´.
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

            // ?•œê¸??´ ë³´ì´?„ë¡? ?„ë² ë””?“œ ?•œê¸? ?°?Š¸ë¥? ì¦‰ì‹œ ? ?š©(?´?›„ KoreanFontApplier ?„ ?¬? ?š©).
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