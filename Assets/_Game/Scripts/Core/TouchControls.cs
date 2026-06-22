using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GemCafe.Core
{
    /// <summary>
    /// 모바일/터치 환경(WebGL 모바일 브라우저 포함)에서 화면 오버레이 입력을 제공한다.
    /// 좌/우 이동 버튼 입력은 Horizontal 값으로 합산되고, 상호작용 버튼은 1프레임 트리거로 소비된다.
    /// </summary>
    public static class TouchControls
    {
        private static float _horizontal;
        private static bool _interactDown;
        private static Driver _driver;
        private static bool _moveButtonsVisible;

#if UNITY_EDITOR
        private static bool _forceEnableInEditor;
        private static bool _showMoveButtonsOnStartInEditor;
#endif

        /// <summary>터치 이동 입력. -1(왼쪽) ~ +1(오른쪽).</summary>
        public static float Horizontal => _horizontal;

        /// <summary>오버레이가 현재 생성되어 동작 중인지 여부.</summary>
        public static bool IsActive => _driver != null;

#if UNITY_EDITOR
        public static void ConfigureEditorOverrides(bool forceEnableInEditor, bool showMoveButtonsOnStart)
        {
            _forceEnableInEditor = forceEnableInEditor;
            _showMoveButtonsOnStartInEditor = showMoveButtonsOnStart;

            if (!Application.isPlaying)
            {
                return;
            }

            if (showMoveButtonsOnStart)
            {
                _moveButtonsVisible = true;
            }

            EnsureBootstrapped();
            if (_driver != null)
            {
                _driver.SetMoveButtonsVisible(_moveButtonsVisible);
            }
        }

        public static void ClearEditorOverrides()
        {
            _forceEnableInEditor = false;
            _showMoveButtonsOnStartInEditor = false;
        }
#endif

        /// <summary>좌/우 이동 버튼 표시 여부를 갱신한다.</summary>
        public static void SetMoveButtonsVisible(bool visible)
        {
            _moveButtonsVisible = visible;
            EnsureBootstrapped();

            if (_driver != null)
            {
                _driver.SetMoveButtonsVisible(visible);
            }
        }

        /// <summary>좌/우 이동 버튼을 잠시 표시한 뒤 자동으로 숨긴다.</summary>
        public static void ShowMoveButtonsTemporarily(float seconds)
        {
            EnsureBootstrapped();

            if (_driver == null)
            {
                _moveButtonsVisible = seconds > 0f;
                return;
            }

            _driver.ShowMoveButtonsTemporarily(seconds);
        }

        /// <summary>
        /// 상호작용 버튼이 이번 프레임에 눌렸으면 true를 반환하고 플래그를 소비한다.
        /// </summary>
        public static bool ConsumeInteract()
        {
            EnsureBootstrapped();

            if (!_interactDown)
            {
                return false;
            }

            _interactDown = false;
            return true;
        }

        /// <summary>상호작용 가능 여부에 따라 상호작용 버튼 표시를 갱신한다.</summary>
        public static void SetInteractAvailable(bool available)
        {
            EnsureBootstrapped();

            if (_driver != null)
            {
                _driver.SetInteractAvailable(available);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            EnsureBootstrapped();
        }

        private static void EnsureBootstrapped()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (_driver != null)
            {
                return;
            }

            if (!ShouldEnable())
            {
                return;
            }

            var go = new GameObject("[TouchControls]");
            Object.DontDestroyOnLoad(go);
            _driver = go.AddComponent<Driver>();
            _driver.Build();

#if UNITY_EDITOR
            if (_showMoveButtonsOnStartInEditor)
            {
                _moveButtonsVisible = true;
            }
#endif

            _driver.SetMoveButtonsVisible(_moveButtonsVisible);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _horizontal = 0f;
            _interactDown = false;
            _driver = null;
            _moveButtonsVisible = false;
#if UNITY_EDITOR
            _forceEnableInEditor = false;
            _showMoveButtonsOnStartInEditor = false;
#endif
        }

        private static bool ShouldEnable()
        {
#if UNITY_EDITOR
            if (_forceEnableInEditor)
            {
                return true;
            }
#endif

            // 일부 플랫폼/브라우저에서는 터치 지원 값이 늦게 반영될 수 있어 touchCount도 함께 본다.
            return Application.isMobilePlatform || Input.touchSupported || Input.touchCount > 0;
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
        /// 오버레이 Canvas와 버튼을 생성/관리하는 런타임 드라이버.
        /// 대화 중에는 오버레이를 숨기고, 종료 시 다시 복원한다.
        /// </summary>
        private sealed class Driver : MonoBehaviour
        {
            private GameObject _interactButton;
            private GameObject _moveLeftButton;
            private GameObject _moveRightButton;
            private GameObject _root;
            private Coroutine _hideMoveButtonsRoutine;
            private int _activeMovePressCount;
            private bool _pendingHideAfterRelease;

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
                _interactButton = CreateButton("Interact", "행동", new Vector2(1f, 0f), new Vector2(-60f, 60f),
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

                _pendingHideAfterRelease = false;

                if (seconds <= 0f)
                {
                    SetMoveButtonsVisible(false);
                    return;
                }

                SetMoveButtonsVisible(true);
                _hideMoveButtonsRoutine = StartCoroutine(HideMoveButtonsAfterDelay(seconds));
            }

            public void NotifyMoveButtonPress(bool pressed)
            {
                if (pressed)
                {
                    _activeMovePressCount++;
                    return;
                }

                _activeMovePressCount = Mathf.Max(0, _activeMovePressCount - 1);
                if (_activeMovePressCount == 0 && _pendingHideAfterRelease)
                {
                    _pendingHideAfterRelease = false;
                    SetMoveButtonsVisible(false);
                }
            }

            private IEnumerator HideMoveButtonsAfterDelay(float seconds)
            {
                yield return new WaitForSeconds(seconds);
                _hideMoveButtonsRoutine = null;

                if (_activeMovePressCount > 0)
                {
                    // 누르고 있는 동안 강제로 숨기면 버튼이 사라진 것처럼 보이므로 손을 뗄 때까지 지연한다.
                    _pendingHideAfterRelease = true;
                    yield break;
                }

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
                hold.Initialize(mode, this);

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
        /// 누르고 있는 동안 이동 입력을 유지하거나, 누르는 순간 상호작용을 트리거하는 버튼.
        /// </summary>
        private sealed class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
        {
            public enum Mode
            {
                MoveLeft,
                MoveRight,
                Interact
            }

            private Mode _mode;
            private Driver _driver;
            private bool _pressed;

            public void Initialize(Mode mode, Driver driver)
            {
                _mode = mode;
                _driver = driver;
            }

            public void OnPointerDown(PointerEventData eventData)
            {
                if (_pressed)
                {
                    return;
                }

                _pressed = true;

                switch (_mode)
                {
                    case Mode.MoveLeft:
                        SetHorizontal(-1f);
                        _driver?.NotifyMoveButtonPress(true);
                        break;
                    case Mode.MoveRight:
                        SetHorizontal(1f);
                        _driver?.NotifyMoveButtonPress(true);
                        break;
                    case Mode.Interact:
                        TriggerInteract();
                        break;
                }
            }

            public void OnPointerUp(PointerEventData eventData)
            {
                Release();
            }

            private void OnDisable()
            {
                // 비활성화로 PointerUp을 못 받는 경우 입력이 고착되지 않도록 정리한다.
                Release();
            }

            private void Release()
            {
                if (!_pressed)
                {
                    return;
                }

                _pressed = false;

                if (_mode == Mode.MoveLeft)
                {
                    if (Mathf.Approximately(Horizontal, -1f))
                    {
                        SetHorizontal(0f);
                    }

                    _driver?.NotifyMoveButtonPress(false);
                }
                else if (_mode == Mode.MoveRight)
                {
                    if (Mathf.Approximately(Horizontal, 1f))
                    {
                        SetHorizontal(0f);
                    }

                    _driver?.NotifyMoveButtonPress(false);
                }
            }
        }
    }
}
