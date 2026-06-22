using GemCafe.Core;
using UnityEngine;
using UnityEngine.UI;

namespace GemCafe.Crafting
{
    /// <summary>제조 미니게임에서 표시할 "여기를 누르세요" 안내 종류.</summary>
    public enum MinigameTouchPrompt
    {
        MixHold,
        PourHold
    }

    /// <summary>"지금!" 순간을 강조할 때 사용하는 안내 종류.</summary>
    public enum MinigameTouchAccent
    {
        MixInside,
        PourRelease
    }

    /// <summary>
    /// 미니게임에서 "지금(타이밍) / 어디를(위치)" 터치하면 되는지 알려 주는 런타임 터치 힌트.
    /// 전체 화면 <see cref="HoldInputArea"/> 위에 손가락 점 + 펄스 링 + 안내 문구를 띄워
    /// 입력을 받을 수 있는 순간과 위치를 시각적으로 알려 준다. 실제로 누르는 동안에는
    /// 누른 지점에 눌림 피드백(링)을 보여 모바일에서 터치가 인식됐음을 확인시켜 준다.
    /// 사용하는 원형 스프라이트는 코드로 생성하므로 별도 에셋/프리팹 배선이 필요 없다.
    /// </summary>
    public class TouchHoldHint : MonoBehaviour
    {
        private const float PulsePeriod = 1.1f;
        private const float FadeSpeed = 6f;

        private static readonly Color PromptColor = new Color(1f, 1f, 1f, 1f);
        private static readonly Color HighlightColor = new Color(0.35f, 1f, 0.5f, 1f);

        private static Sprite _circleSprite;
        private static Sprite _ringSprite;

        private RectTransform _root;
        private CanvasGroup _group;
        private Canvas _canvas;

        private RectTransform _promptRoot;
        private CanvasGroup _promptGroup;
        private RectTransform _pulseRing;
        private Image _pulseImage;
        private RectTransform _fingerDot;
        private Image _fingerImage;
        private Image _fingerCore;
        private Text _label;

        private RectTransform _pressRing;
        private CanvasGroup _pressGroup;
        private Image _pressImage;

        private Text _holdLabel;

        private bool _visible;
        private bool _highlight;
        private bool _holding;
        private string _baseMessage = string.Empty;
        private string _highlightMessage = string.Empty;
        private float _fade;
        private float _pressFade;
        private float _time;
        private Vector2 _pressLocalPos;

        public static TouchHoldHint Create(RectTransform parent)
        {
            var go = new GameObject("TouchHoldHint", typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            // 트랙/컵 등 다른 UI 위에 항상 보이도록 정렬 순서를 띄워 둔다.
            var canvas = go.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 70;

            var hint = go.AddComponent<TouchHoldHint>();
            hint.Build();
            return hint;
        }

        public void Show(MinigameTouchPrompt prompt)
        {
            _baseMessage = ResolvePrompt(prompt);
            _highlight = false;
            _visible = true;
            if (_label != null)
            {
                _label.text = _baseMessage;
            }
        }

        public void Hide()
        {
            _visible = false;
            _highlight = false;
            _holding = false;
        }

        /// <summary>성공 타이밍바 등 "지금!" 순간을 초록 강조 + 문구로 알린다.</summary>
        public void SetHighlight(bool on, MinigameTouchAccent accent)
        {
            _highlight = on;
            _highlightMessage = ResolveAccent(accent);
            if (_label == null)
            {
                return;
            }

            _label.text = on ? _highlightMessage : _baseMessage;
        }

        /// <summary>누름 상태와 화면 좌표(눌림 피드백 위치)를 전달받는다.</summary>
        public void SetPress(bool holding, Vector2 screenPos)
        {
            _holding = holding;
            if (!holding || _root == null)
            {
                return;
            }

            var cam = _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? _canvas.worldCamera
                : null;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, screenPos, cam, out var local))
            {
                _pressLocalPos = local;
            }
        }

        private static string ResolvePrompt(MinigameTouchPrompt prompt)
        {
            switch (prompt)
            {
                case MinigameTouchPrompt.PourHold:
                    return "꾹 눌러 차를 따르고, 가득 차면 손을 떼세요";
                default:
                    return "화면을 꾹 눌러 찻잎을 막대 안에 두세요";
            }
        }

        private static string ResolveAccent(MinigameTouchAccent accent)
        {
            switch (accent)
            {
                case MinigameTouchAccent.PourRelease:
                    return "지금 손을 떼세요!";
                default:
                    return "좋아요! 계속 유지하세요";
            }
        }

        private void Build()
        {
            _root = (RectTransform)transform;
            _canvas = GetComponentInParent<Canvas>();

            _group = gameObject.AddComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.interactable = false;
            _group.blocksRaycasts = false;

            var circle = GetCircleSprite();
            var ring = GetRingSprite();

            // 안내 묶음 (화면 중앙 고정)
            _promptRoot = NewRect("Prompt", _root);
            _promptRoot.anchorMin = new Vector2(0.5f, 0.5f);
            _promptRoot.anchorMax = new Vector2(0.5f, 0.5f);
            _promptRoot.pivot = new Vector2(0.5f, 0.5f);
            _promptRoot.anchoredPosition = Vector2.zero;
            _promptRoot.sizeDelta = new Vector2(640f, 380f);
            _promptGroup = _promptRoot.gameObject.AddComponent<CanvasGroup>();
            _promptGroup.blocksRaycasts = false;

            _pulseRing = NewRect("Pulse", _promptRoot);
            _pulseRing.anchorMin = _pulseRing.anchorMax = new Vector2(0.5f, 0.5f);
            _pulseRing.pivot = new Vector2(0.5f, 0.5f);
            _pulseRing.anchoredPosition = Vector2.zero;
            _pulseRing.sizeDelta = new Vector2(210f, 210f);
            _pulseImage = AddImage(_pulseRing, ring);

            _fingerDot = NewRect("Finger", _promptRoot);
            _fingerDot.anchorMin = _fingerDot.anchorMax = new Vector2(0.5f, 0.5f);
            _fingerDot.pivot = new Vector2(0.5f, 0.5f);
            _fingerDot.anchoredPosition = Vector2.zero;
            _fingerDot.sizeDelta = new Vector2(116f, 116f);
            _fingerImage = AddImage(_fingerDot, circle);
            _fingerImage.color = new Color(1f, 1f, 1f, 0.35f);

            var coreRect = NewRect("FingerCore", _fingerDot);
            coreRect.anchorMin = coreRect.anchorMax = new Vector2(0.5f, 0.5f);
            coreRect.pivot = new Vector2(0.5f, 0.5f);
            coreRect.anchoredPosition = Vector2.zero;
            coreRect.sizeDelta = new Vector2(64f, 64f);
            _fingerCore = AddImage(coreRect, circle);

            var labelRect = NewRect("Label", _promptRoot);
            labelRect.anchorMin = new Vector2(0.5f, 0.5f);
            labelRect.anchorMax = new Vector2(0.5f, 0.5f);
            labelRect.pivot = new Vector2(0.5f, 0.5f);
            labelRect.anchoredPosition = new Vector2(0f, 130f);
            labelRect.sizeDelta = new Vector2(760f, 90f);
            _label = labelRect.gameObject.AddComponent<Text>();
            _label.alignment = TextAnchor.MiddleCenter;
            _label.fontSize = 44;
            _label.fontStyle = FontStyle.Bold;
            _label.horizontalOverflow = HorizontalWrapMode.Overflow;
            _label.verticalOverflow = VerticalWrapMode.Overflow;
            _label.raycastTarget = false;
            _label.color = Color.white;
            _label.font = KoreanFontApplier.Font != null
                ? KoreanFontApplier.Font
                : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var shadow = _label.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.65f);
            shadow.effectDistance = new Vector2(2f, -2f);

            var holdLabelRect = NewRect("HoldLabel", _promptRoot);
            holdLabelRect.anchorMin = new Vector2(0.5f, 0.5f);
            holdLabelRect.anchorMax = new Vector2(0.5f, 0.5f);
            holdLabelRect.pivot = new Vector2(0.5f, 0.5f);
            holdLabelRect.anchoredPosition = new Vector2(0f, -100f);
            holdLabelRect.sizeDelta = new Vector2(400f, 80f);
            _holdLabel = holdLabelRect.gameObject.AddComponent<Text>();
            _holdLabel.text = "Hold";
            _holdLabel.alignment = TextAnchor.MiddleCenter;
            _holdLabel.fontSize = 58;
            _holdLabel.fontStyle = FontStyle.Bold;
            _holdLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
            _holdLabel.verticalOverflow = VerticalWrapMode.Overflow;
            _holdLabel.raycastTarget = false;
            _holdLabel.color = Color.white;
            _holdLabel.font = KoreanFontApplier.Font != null
                ? KoreanFontApplier.Font
                : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var holdShadow = _holdLabel.gameObject.AddComponent<Shadow>();
            holdShadow.effectColor = new Color(0f, 0f, 0f, 0.65f);
            holdShadow.effectDistance = new Vector2(2f, -2f);

            // 누름 지점 피드백
            _pressRing = NewRect("Press", _root);
            _pressRing.anchorMin = _pressRing.anchorMax = new Vector2(0.5f, 0.5f);
            _pressRing.pivot = new Vector2(0.5f, 0.5f);
            _pressRing.sizeDelta = new Vector2(170f, 170f);
            _pressImage = AddImage(_pressRing, ring);
            _pressGroup = _pressRing.gameObject.AddComponent<CanvasGroup>();
            _pressGroup.alpha = 0f;
            _pressGroup.blocksRaycasts = false;
        }

        private void Update()
        {
            _time += Time.unscaledDeltaTime;
            UpdatePromptCenter();

            _fade = Mathf.MoveTowards(_fade, _visible ? 1f : 0f, FadeSpeed * Time.unscaledDeltaTime);
            if (_group != null)
            {
                _group.alpha = _fade;
            }

            if (_fade <= 0.001f)
            {
                return;
            }

            var accent = _highlight ? HighlightColor : PromptColor;

            // 누르기 전에는 펄스로 위치를 강조하고, 누르는 중에는 안내를 약하게 줄인다.
            float promptTarget = !_holding || _highlight ? 1f : 0.25f;
            if (_promptGroup != null)
            {
                _promptGroup.alpha = Mathf.MoveTowards(_promptGroup.alpha, promptTarget, FadeSpeed * Time.unscaledDeltaTime);
            }

            float cycle = Mathf.Repeat(_time / PulsePeriod, 1f);
            if (_pulseRing != null)
            {
                _pulseRing.localScale = Vector3.one * Mathf.Lerp(0.65f, 1.7f, cycle);
            }
            if (_pulseImage != null)
            {
                var c = accent;
                c.a = (1f - cycle) * 0.85f;
                _pulseImage.color = c;
            }

            if (_fingerDot != null)
            {
                float bobSpeed = _highlight ? 9f : 3.5f;
                float bobAmp = _highlight ? 0.12f : 0.06f;
                _fingerDot.localScale = Vector3.one * (1f + Mathf.Sin(_time * bobSpeed) * bobAmp);
            }
            if (_fingerImage != null)
            {
                var c = accent;
                c.a = 0.35f;
                _fingerImage.color = c;
            }
            if (_fingerCore != null)
            {
                _fingerCore.color = accent;
            }
            if (_label != null)
            {
                _label.color = _highlight ? HighlightColor : Color.white;
            }
            if (_holdLabel != null)
            {
                _holdLabel.color = _highlight ? HighlightColor : Color.white;
            }

            // 누름 지점 피드백
            _pressFade = Mathf.MoveTowards(_pressFade, _holding ? 1f : 0f, FadeSpeed * Time.unscaledDeltaTime);
            if (_pressGroup != null)
            {
                _pressGroup.alpha = _pressFade;
            }
            if (_pressRing != null)
            {
                _pressRing.anchoredPosition = _pressLocalPos;
                _pressRing.localScale = Vector3.one * (1f + Mathf.Sin(_time * 7f) * 0.12f);
            }
            if (_pressImage != null)
            {
                var c = accent;
                c.a = 0.9f;
                _pressImage.color = c;
            }
        }

        private void UpdatePromptCenter()
        {
            if (_root == null || _promptRoot == null)
            {
                return;
            }

            var cam = _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? _canvas.worldCamera
                : null;
            var center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, center, cam, out var localCenter))
            {
                _promptRoot.anchoredPosition = localCenter;
            }
        }

        private static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        private static Image AddImage(RectTransform rect, Sprite sprite)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            return image;
        }

        private static Sprite GetCircleSprite()
        {
            if (_circleSprite == null)
            {
                _circleSprite = BuildCircleSprite(false);
            }

            return _circleSprite;
        }

        private static Sprite GetRingSprite()
        {
            if (_ringSprite == null)
            {
                _ringSprite = BuildCircleSprite(true);
            }

            return _ringSprite;
        }

        private static Sprite BuildCircleSprite(bool ring)
        {
            const int size = 128;
            const float feather = 1.5f;

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                name = ring ? "TouchHoldHint_Ring" : "TouchHoldHint_Circle"
            };

            var pixels = new Color32[size * size];
            float half = size * 0.5f;
            float outer = half - 2f;
            float ringRadius = outer * 0.82f;
            float ringHalf = outer * 0.16f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - half;
                    float dy = y + 0.5f - half;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);

                    float a = ring
                        ? Mathf.Clamp01((ringHalf - Mathf.Abs(d - ringRadius)) / feather)
                        : Mathf.Clamp01((outer - d) / feather);

                    byte alpha = (byte)Mathf.RoundToInt(Mathf.Clamp01(a) * 255f);
                    pixels[y * size + x] = new Color32(255, 255, 255, alpha);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();

            return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
