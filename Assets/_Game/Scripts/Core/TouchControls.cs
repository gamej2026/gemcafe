using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

namespace GemCafe.Core
{
    /// <summary>
    /// ëª¨ë°”?¼/?„°ì¹? ?™˜ê²?(WebGL ëª¨ë°”?¼ ë¸Œë¼?š°??? ?¬?•¨)?—?„œ ê²Œì„?„ ì§„í–‰?•  ?ˆ˜ ?ˆ?„ë¡?
    /// ?™”ë©? ?œ„?— ì¢?/?š° ?´?™ ë²„íŠ¼ê³? ?ƒ?˜¸?‘?š© ë²„íŠ¼?„ ?Ÿ°????„?œ¼ë¡? ?„?š´?‹¤.
    /// ?”¬(.unity)?„ ì§ì ‘ ?¸ì§‘í•˜ì§? ?•Š?„ë¡? <see cref="KoreanFontApplier"/>??? ?™?¼?•˜ê²?
    /// RuntimeInitializeOnLoadMethodë¡? ?˜¤ë²„ë ˆ?´ Canvas??? ë²„íŠ¼?„ ì½”ë“œë¡? ?ƒ?„±?•œ?‹¤.
    ///
    /// ?‚¤ë³´ë“œ/ë§ˆìš°?Š¤ ?…? ¥??? ê·¸ë??ë¡? ?œ ì§??˜ë©? ?´ ?˜¤ë²„ë ˆ?´?Š” ê·? ?œ„?— ?”?•´ì§„ë‹¤.
    /// - ?´?™: <see cref="Horizontal"/> ê°’ì„ PlayerMoverê°? ?‚¤ë³´ë“œ ì¶•ê³¼ ?•©?‚°?•œ?‹¤.
    /// - ?ƒ?˜¸?‘?š©: <see cref="ConsumeInteract"/>ë¥? Interactorê°? F?‚¤??? ORë¡? ë¬¶ëŠ”?‹¤.
    /// </summary>
    public static class TouchControls
    {
        /// <summary>?—?””?„°/PC?—?„œ?„ ê°•ì œë¡? ?˜¤ë²„ë ˆ?´ë¥? ?‘œ?‹œ?•˜? ¤ë©? trueë¡? ?‘”?‹¤(?…Œ?Š¤?Š¸?š©).</summary>
        private const bool ForceEnable = false;

        private static float _horizontal;
        private static bool _interactDown;
        private static Driver _driver;
        private static bool _moveButtonsVisible;

        /// <summary>?„°ì¹? ?´?™ ?…? ¥. -1(?™¼ìª?) ~ +1(?˜¤ë¥¸ìª½). ë²„íŠ¼?„ ?ˆ„ë¥´ê³  ?ˆ?Š” ?™?•ˆ ?œ ì§??œ?‹¤.</summary>
        public static float Horizontal => _horizontal;

        /// <summary>?˜¤ë²„ë ˆ?´ê°? ?˜„?¬ ?™œ?„±(?Š¤?°?¨)?¸ì§? ?—¬ë¶?.</summary>
        public static bool IsActive => _driver != null;

        /// <summary>ì¢?/?š° ?´?™ ë²„íŠ¼ ?‘œ?‹œ ?—¬ë¶?ë¥? ê°±ì‹ ?•œ?‹¤.</summary>
        public static void SetMoveButtonsVisible(bool visible)
        {
            _moveButtonsVisible = visible;
            if (_driver != null)
            {
                _driver.SetMoveButtonsVisible(visible);
            }
        }

        /// <summary>ì¢?/?š° ?´?™ ë²„íŠ¼?„ ? ?‹œ ?‘œ?‹œ?•œ ?’¤ ??™?œ¼ë¡? ?ˆ¨ê¸´ë‹¤.</summary>
        public static void ShowMoveButtonsTemporarily(float seconds)
        {
            if (_driver == null)
            {
                _moveButtonsVisible = seconds > 0f;
                return;
            }

            _driver.ShowMoveButtonsTemporarily(seconds);
        }

        /// <summary>
        /// ?ƒ?˜¸?‘?š© ë²„íŠ¼?´ ?´ë²? ?”„? ˆ?„?— ?ˆŒ? ¸?œ¼ë©? trueë¥? ë°˜í™˜?•˜ê³? ?”Œ?˜ê·¸ë?? ?†Œë¹„í•œ?‹¤.
        /// ?‚¤ë³´ë“œ F?‚¤??? ?™?¼?•˜ê²? 1?”„? ˆ?„ ?Š¸ë¦¬ê±°ë¡? ?™?‘?•œ?‹¤.
        /// </summary>
        public static bool ConsumeInteract()
        {
            if (!_interactDown)
            {
                return false;
            }

            _interactDown = false;
            return true;
        }

        /// <summary>
        /// ê·¼ì²˜?— ?ƒ?˜¸?‘?š© ê°??Š¥?•œ ????ƒ?´ ?ˆ?Š”ì§??— ?”°?¼ ?ƒ?˜¸?‘?š© ë²„íŠ¼ ?‘œ?‹œ ?—¬ë¶?ë¥? ê°±ì‹ ?•œ?‹¤.
        /// (Interactorê°? keyPromptUIë¥? ?† ê¸??•˜?Š” ?‹œ? ?— ?•¨ê»? ?˜¸ì¶œí•œ?‹¤.)
        /// </summary>
        public static void SetInteractAvailable(bool available)
        {
            if (_driver != null)
            {
                _driver.SetInteractAvailable(available);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (!ShouldEnable())
            {
                return;
            }

            if (_driver != null)
            {
                return;
            }

            var go = new GameObject("[TouchControls]");
            Object.DontDestroyOnLoad(go);
            _driver = go.AddComponent<Driver>();
            _driver.Build();
            _driver.SetMoveButtonsVisible(_moveButtonsVisible);
        }

        private static bool ShouldEnable()
        {
            if (ForceEnable)
            {
                return true;
            }

            return Application.isMobilePlatform || Input.touchSupported;
        }

        private static void SetHorizontal(float value)
        {
            _horizontal = value;
        }

        private static void TriggerInteract()
        {
            _interactDown = true;
        }

        /// <summary>
        /// ?˜¤ë²„ë ˆ?´ Canvas??? ë²„íŠ¼?„ ?ƒ?„±/ê´?ë¦¬í•˜?Š” ?Ÿ°????„ ?“œ?¼?´ë²?.
        /// ????™” ì¤‘ì—?Š” ?´?™/?ƒ?˜¸?‘?š© ?…? ¥?´ ? ê¸°ë??ë¡? ?˜¤ë²„ë ˆ?´ë¥? ?ˆ¨ê¸´ë‹¤.
        /// </summary>
        private sealed class Driver : MonoBehaviour
        {
            private GameObject _interactButton;
            private GameObject _moveLeftButton;
            private GameObject _moveRightButton;
            private GameObject _root;
            private Coroutine _hideMoveButtonsRoutine;

            public void Build()
            {
                _root = new GameObject("Overlay");
                _root.transform.SetParent(transform, false);

                var canvasGo = _root;
                var canvas = canvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 5000;

                var scaler = canvasGo.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;

                canvasGo.AddComponent<GraphicRaycaster>();

                EnsureEventSystem();

                _moveLeftButton = CreateButton("MoveLeft", "<", new Vector2(0f, 0f), new Vector2(60f, 60f),
                    new Color(0.15f, 0.15f, 0.18f, 0.55f), HoldButton.Mode.MoveLeft);
                _moveRightButton = CreateButton("MoveRight", ">", new Vector2(0f, 0f), new Vector2(330f, 60f),
                    new Color(0.15f, 0.15f, 0.18f, 0.55f), HoldButton.Mode.MoveRight);
                _interactButton = CreateButton("Interact", "?–‰?™", new Vector2(1f, 0f), new Vector2(-60f, 60f),
                    new Color(0.85f, 0.55f, 0.15f, 0.7f), HoldButton.Mode.Interact);

                if (_interactButton != null)
                {
                    _interactButton.SetActive(false);
                }

                SetMoveButtonsVisible(_moveButtonsVisible);
            }

            public void SetInteractAvailable(bool available)
            {
                if (_interactButton != null)
                {
                    _interactButton.SetActive(available);
                }
            }

            public void SetMoveButtonsVisible(bool visible)
            {
                _moveButtonsVisible = visible;
                if (!visible)
                {
                    SetHorizontal(0f);
                }

                if (_moveLeftButton != null)
                {
                    _moveLeftButton.SetActive(visible);
                }

                if (_moveRightButton != null)
                {
                    _moveRightButton.SetActive(visible);
                }
            }

            public void ShowMoveButtonsTemporarily(float seconds)
            {
                if (_hideMoveButtonsRoutine != null)
                {
                    StopCoroutine(_hideMoveButtonsRoutine);
                    _hideMoveButtonsRoutine = null;
                }

                if (seconds <= 0f)
                {
                    SetMoveButtonsVisible(false);
                    return;
                }

                SetMoveButtonsVisible(true);
                _hideMoveButtonsRoutine = StartCoroutine(HideMoveButtonsAfterDelay(seconds));
            }

            private IEnumerator HideMoveButtonsAfterDelay(float seconds)
            {
                yield return new WaitForSeconds(seconds);
                _hideMoveButtonsRoutine = null;
                SetMoveButtonsVisible(false);
            }

            private void OnEnable()
            {
                EventBus.OnDialogueStarted += HandleDialogueStarted;
                EventBus.OnDialogueEnded += HandleDialogueEnded;
            }

            private void OnDisable()
            {
                EventBus.OnDialogueStarted -= HandleDialogueStarted;
                EventBus.OnDialogueEnded -= HandleDialogueEnded;
            }

            private void HandleDialogueStarted()
            {
                SetHorizontal(0f);
                if (_root != null)
                {
                    _root.SetActive(false);
                }
            }

            private void HandleDialogueEnded()
            {
                if (_root != null)
                {
                    _root.SetActive(true);
                }

                SetMoveButtonsVisible(_moveButtonsVisible);
            }

            private GameObject CreateButton(string name, string label, Vector2 anchor, Vector2 anchoredPosition,
                Color background, HoldButton.Mode mode)
            {
                var go = new GameObject(name, typeof(RectTransform));
                go.transform.SetParent(_root.transform, false);

                var rect = go.GetComponent<RectTransform>();
                rect.anchorMin = anchor;
                rect.anchorMax = anchor;
                rect.pivot = anchor;
                rect.sizeDelta = new Vector2(220f, 220f);
                rect.anchoredPosition = anchoredPosition;

                var image = go.AddComponent<Image>();
                image.color = background;

                var hold = go.AddComponent<HoldButton>();
                hold.mode = mode;

                CreateLabel(go.transform, label);
                return go;
            }

            private void CreateLabel(Transform parent, string label)
            {
                var go = new GameObject("Label", typeof(RectTransform));
                go.transform.SetParent(parent, false);

                var rect = go.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                var text = go.AddComponent<Text>();
                text.text = label;
                text.alignment = TextAnchor.MiddleCenter;
                text.fontSize = 96;
                text.color = Color.white;
                text.raycastTarget = false;

                var font = KoreanFontApplier.Font;
                text.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            private static void EnsureEventSystem()
            {
                if (Object.FindObjectOfType<EventSystem>() != null)
                {
                    return;
                }

                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }
        }

        /// <summary>
        /// ?ˆ„ë¥´ëŠ” ?™?•ˆ ?´?™ ?…? ¥?„ ?œ ì§??•˜ê±°ë‚˜, ?ˆ„ë¥´ëŠ” ?ˆœê°? ?ƒ?˜¸?‘?š©?„ ?Š¸ë¦¬ê±°?•˜?Š” ë²„íŠ¼.
        /// </summary>
        private sealed class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
        {
            public enum Mode
            {
                MoveLeft,
                MoveRight,
                Interact
            }

            public Mode mode;

            public void OnPointerDown(PointerEventData eventData)
            {
                switch (mode)
                {
                    case Mode.MoveLeft:
                        SetHorizontal(-1f);
                        break;
                    case Mode.MoveRight:
                        SetHorizontal(1f);
                        break;
                    case Mode.Interact:
                        TriggerInteract();
                        break;
                }
            }

            public void OnPointerUp(PointerEventData eventData)
            {
                if (mode == Mode.MoveLeft && Mathf.Approximately(Horizontal, -1f))
                {
                    SetHorizontal(0f);
                }
                else if (mode == Mode.MoveRight && Mathf.Approximately(Horizontal, 1f))
                {
                    SetHorizontal(0f);
                }
            }
        }
    }
}
