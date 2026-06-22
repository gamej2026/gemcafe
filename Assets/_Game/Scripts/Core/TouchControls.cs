using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GemCafe.Core
{
    /// <summary>
    /// 모바일/터치 환경(WebGL 모바일 브라우저 포함)에서 게임을 진행할 수 있도록
    /// 화면 위에 좌/우 이동 버튼과 상호작용 버튼을 런타임으로 띄운다.
    /// 씬(.unity)을 직접 편집하지 않도록 <see cref="KoreanFontApplier"/>와 동일하게
    /// RuntimeInitializeOnLoadMethod로 오버레이 Canvas와 버튼을 코드로 생성한다.
    ///
    /// 키보드/마우스 입력은 그대로 유지되며 이 오버레이는 그 위에 더해진다.
    /// - 이동: <see cref="Horizontal"/> 값을 PlayerMover가 키보드 축과 합산한다.
    /// - 상호작용: <see cref="ConsumeInteract"/>를 Interactor가 F키와 OR로 묶는다.
    /// </summary>
    public static class TouchControls
    {
        /// <summary>에디터/PC에서도 강제로 오버레이를 표시하려면 true로 둔다(테스트용).</summary>
        private const bool ForceEnable = false;

        private static float _horizontal;
        private static bool _interactDown;
        private static Driver _driver;

        /// <summary>터치 이동 입력. -1(왼쪽) ~ +1(오른쪽). 버튼을 누르고 있는 동안 유지된다.</summary>
        public static float Horizontal => _horizontal;

        /// <summary>오버레이가 현재 활성(스폰됨)인지 여부.</summary>
        public static bool IsActive => _driver != null;

        /// <summary>
        /// 상호작용 버튼이 이번 프레임에 눌렸으면 true를 반환하고 플래그를 소비한다.
        /// 키보드 F키와 동일하게 1프레임 트리거로 동작한다.
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
        /// 근처에 상호작용 가능한 대상이 있는지에 따라 상호작용 버튼 표시 여부를 갱신한다.
        /// (Interactor가 keyPromptUI를 토글하는 시점에 함께 호출한다.)
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
        /// 오버레이 Canvas와 버튼을 생성/관리하는 런타임 드라이버.
        /// 대화 중에는 이동/상호작용 입력이 잠기므로 오버레이를 숨긴다.
        /// </summary>
        private sealed class Driver : MonoBehaviour
        {
            private GameObject _interactButton;
            private GameObject _root;

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

                CreateButton("MoveLeft", "<", new Vector2(0f, 0f), new Vector2(60f, 60f),
                    new Color(0.15f, 0.15f, 0.18f, 0.55f), HoldButton.Mode.MoveLeft);
                CreateButton("MoveRight", ">", new Vector2(0f, 0f), new Vector2(330f, 60f),
                    new Color(0.15f, 0.15f, 0.18f, 0.55f), HoldButton.Mode.MoveRight);
                _interactButton = CreateButton("Interact", "행동", new Vector2(1f, 0f), new Vector2(-60f, 60f),
                    new Color(0.85f, 0.55f, 0.15f, 0.7f), HoldButton.Mode.Interact);

                if (_interactButton != null)
                {
                    _interactButton.SetActive(false);
                }
            }

            public void SetInteractAvailable(bool available)
            {
                if (_interactButton != null)
                {
                    _interactButton.SetActive(available);
                }
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
        /// 누르는 동안 이동 입력을 유지하거나, 누르는 순간 상호작용을 트리거하는 버튼.
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
