using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using GemCafe.Core;
using GemCafe.Dialogue;
using UnityEngine;
using UnityEngine.UI;

namespace GemCafe.Ending
{
    /// <summary>
    /// Ending 씬에서 CSV(Resources/Endings/ending_dialogue.csv) 기반 엔딩 연출을 재생한다.
    /// GameManager.PendingEnding 종류에 맞는 비트를 순서대로 표시하며,
    /// 배경/CG/스탠딩 일러스트/대화창/화면 효과/BGM/SFX를 함께 제어한다.
    /// 마지막 비트 이후 로비로 복귀한다.
    /// 실제 아트/오디오 에셋이 없으면 플레이스홀더 색상/무음으로 대체한다.
    /// </summary>
    public class EndingDirector : MonoBehaviour
    {
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image cgImage;
        [SerializeField] private Image effectOverlay;
        [SerializeField] private DialogueView dialogueView;
        [SerializeField] private SpeakerView speakerView;
        [SerializeField] private float effectDuration = 0.45f;

        private static readonly Color SepiaColor = new Color(0.44f, 0.34f, 0.18f, 0.55f);
        private static readonly Color RedColor = new Color(0.55f, 0.05f, 0.05f, 0.4f);
        private static readonly Color BlackColor = new Color(0f, 0f, 0f, 1f);
        private static readonly Color FlashColor = Color.white;
        private static readonly Color PlaceholderBg = new Color(0.12f, 0.12f, 0.14f, 1f);
        private static readonly Color PlaceholderCg = new Color(0.18f, 0.16f, 0.2f, 1f);

        private List<EndingBeat> _beats = new List<EndingBeat>();
        private int _index = -1;
        private bool _finished;
        private int _lastAdvanceFrame = -1;
        private Coroutine _effectRoutine;
        private Coroutine _backgroundRoutine;
        private Image _fadeLayer;

        private void Start()
        {
            var kind = GameManager.Instance != null ? GameManager.Instance.PendingEnding : EndingKind.B;
            _beats = EndingCsvLoader.Load(kind);

            AudioManager.Instance?.PlayEndingBgm(kind);

            if (dialogueView != null)
            {
                dialogueView.BindNext(Advance);
                dialogueView.Show(false);
            }

            if (speakerView != null)
            {
                speakerView.Show(false);
            }

            if (cgImage != null)
            {
                cgImage.gameObject.SetActive(false);
            }

            if (effectOverlay != null)
            {
                SetOverlay(Color.clear);
            }

            if (_beats.Count == 0)
            {
                Debug.LogWarning("EndingDirector: 재생할 엔딩 비트가 없습니다. 즉시 종료합니다.");
                Finish();
                return;
            }

            _index = 0;
            ApplyBeat(_beats[0]);
        }

        private void Update()
        {
            if (_finished)
            {
                return;
            }

            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            {
                Advance();
            }
        }

        private void Advance()
        {
            if (_finished)
            {
                return;
            }

            // 같은 프레임에 버튼 클릭과 마우스 입력이 동시에 들어와도 한 번만 진행한다.
            if (Time.frameCount == _lastAdvanceFrame)
            {
                return;
            }

            _lastAdvanceFrame = Time.frameCount;

            // 타이핑 중이면 먼저 전체 문장을 즉시 표시한다.
            if (dialogueView != null && dialogueView.IsTyping)
            {
                dialogueView.CompleteTyping();
                return;
            }

            _index++;
            if (_index >= _beats.Count)
            {
                Finish();
                return;
            }

            ApplyBeat(_beats[_index]);
        }

        private void ApplyBeat(EndingBeat beat)
        {
            StopBackgroundRoutine();

            if (beat.effect == "fadein")
            {
                // 화면 효과 fadeIn: 배경 이미지를 서서히 나타나게 한다. (배경 이미지 칸: "경로, 페이드시간")
                ApplyEffect("none");
                ApplyFadeInBackground(beat.bgPath);
            }
            else if (beat.effect == "animate")
            {
                // 화면 효과 animate: 연속된 번호의 배경 이미지를 차례로 페이드인하며 보여준다.
                // (배경 이미지 칸: "경로시작~끝, 다음이미지 보이는시간, 다음이미지 페이드인시간")
                ApplyEffect("none");
                ApplyAnimateBackground(beat.bgPath);
            }
            else
            {
                ApplyEffect(beat.effect);
                ApplyBackground(beat.bgPath);
            }

            ApplyCg(beat.cgPath);
            ApplyAudio(beat.bgmPath, beat.sfxPath);
            ApplyDialogue(beat);
        }

        private void ApplyBackground(string path)
        {
            if (backgroundImage == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(path))
            {
                // 배경 지정이 없으면 기존 배경을 유지한다.
                return;
            }

            var sprite = Resources.Load<Sprite>(path);
            if (sprite != null)
            {
                backgroundImage.sprite = sprite;
                backgroundImage.color = Color.white;
            }
            else
            {
                backgroundImage.sprite = null;
                backgroundImage.color = PlaceholderBg;
            }
        }

        // 화면 효과 fadeIn: 이전 배경은 그대로 두고, 새 배경 이미지를 위 레이어에서 알파 0 → 1 로
        // 서서히 나타나게 한다. 페이드 인이 끝나면 새 이미지를 배경으로 확정하고 이전 이미지는 사라진다.
        private void ApplyFadeInBackground(string raw)
        {
            if (backgroundImage == null)
            {
                return;
            }

            ParseFadeIn(raw, out string path, out float duration);

            var sprite = string.IsNullOrEmpty(path) ? null : Resources.Load<Sprite>(path);
            _backgroundRoutine = StartCoroutine(RunFadeIn(sprite, duration));
        }

        private IEnumerator RunFadeIn(Sprite sprite, float duration)
        {
            yield return FadeInLayerRoutine(sprite, duration);
            _backgroundRoutine = null;
        }

        // 이전 배경(backgroundImage)은 유지한 채, 위 레이어(_fadeLayer)에서 새 이미지를 알파 0 → 1 로
        // 페이드 인한다. 완료되면 새 이미지를 backgroundImage 로 확정하고 레이어를 숨겨 이전 이미지를 없앤다.
        private IEnumerator FadeInLayerRoutine(Sprite sprite, float duration)
        {
            Color target = sprite != null ? Color.white : PlaceholderBg;

            var layer = EnsureFadeLayer();
            if (layer == null)
            {
                backgroundImage.sprite = sprite;
                backgroundImage.color = target;
                yield break;
            }

            // 새 이미지를 위 레이어에 올리고 투명(알파 0)에서 시작한다. 이전 이미지는 backgroundImage 에 그대로 보인다.
            layer.gameObject.SetActive(true);
            layer.sprite = sprite;
            layer.color = new Color(target.r, target.g, target.b, 0f);

            if (duration > 0f)
            {
                float elapsed = 0f;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float a = Mathf.Clamp01(elapsed / duration);
                    layer.color = new Color(target.r, target.g, target.b, a);
                    yield return null;
                }
            }

            layer.color = target;

            // 페이드 인 완료: 새 이미지를 배경으로 확정하고 이전 이미지를 사라지게 한다.
            backgroundImage.sprite = sprite;
            backgroundImage.color = target;
            layer.gameObject.SetActive(false);
            layer.sprite = null;
        }

        // backgroundImage 바로 위에 전체 화면 페이드용 레이어를 1회 생성한다.
        private Image EnsureFadeLayer()
        {
            if (_fadeLayer != null)
            {
                return _fadeLayer;
            }

            if (backgroundImage == null)
            {
                return null;
            }

            var go = new GameObject("FadeInLayer", typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(backgroundImage.transform.parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.SetSiblingIndex(backgroundImage.transform.GetSiblingIndex() + 1);

            var img = go.AddComponent<Image>();
            img.raycastTarget = false;
            img.color = new Color(1f, 1f, 1f, 0f);
            go.SetActive(false);

            _fadeLayer = img;
            return _fadeLayer;
        }

        // 화면 효과 animate: 연속된 번호의 배경 이미지를 차례로 페이드인하며 보여준다.
        private void ApplyAnimateBackground(string raw)
        {
            if (backgroundImage == null)
            {
                return;
            }

            ParseAnimate(raw, out string prefix, out int start, out int end, out float stepDelay, out float fadeDuration);
            _backgroundRoutine = StartCoroutine(AnimateBackgroundRoutine(prefix, start, end, stepDelay, fadeDuration));
        }

        private IEnumerator AnimateBackgroundRoutine(string prefix, int start, int end, float stepDelay, float fadeDuration)
        {
            int step = start <= end ? 1 : -1;
            for (int n = start; ; n += step)
            {
                var sprite = Resources.Load<Sprite>(prefix + n.ToString(CultureInfo.InvariantCulture));
                yield return FadeInLayerRoutine(sprite, fadeDuration);

                if (n == end)
                {
                    break;
                }

                if (stepDelay > 0f)
                {
                    yield return new WaitForSeconds(stepDelay);
                }
            }

            _backgroundRoutine = null;
        }

        private void StopBackgroundRoutine()
        {
            if (_backgroundRoutine != null)
            {
                StopCoroutine(_backgroundRoutine);
                _backgroundRoutine = null;
            }

            // 진행 중이던 페이드를 즉시 확정하고 레이어를 정리한다.
            if (_fadeLayer != null && _fadeLayer.gameObject.activeSelf)
            {
                if (backgroundImage != null)
                {
                    backgroundImage.sprite = _fadeLayer.sprite;
                    backgroundImage.color = _fadeLayer.sprite != null ? Color.white : PlaceholderBg;
                }

                _fadeLayer.gameObject.SetActive(false);
                _fadeLayer.sprite = null;
            }
        }

        // "이미지경로, 페이드시간" 형식을 파싱한다. 예: "Endings/RealEnding5, 1.2"
        private static void ParseFadeIn(string raw, out string path, out float duration)
        {
            path = string.Empty;
            duration = 1f;

            if (string.IsNullOrEmpty(raw))
            {
                return;
            }

            var parts = raw.Split(',');
            path = parts[0].Trim();
            if (parts.Length > 1 && float.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float d))
            {
                duration = d;
            }
        }

        // "이미지경로시작번호~끝번호, 보이는시간, 페이드인시간" 형식을 파싱한다. 예: "Endings/RealEnding6~8,0.8,0.2"
        private static void ParseAnimate(string raw, out string prefix, out int start, out int end, out float stepDelay, out float fadeDuration)
        {
            prefix = string.Empty;
            start = 0;
            end = 0;
            stepDelay = 0.8f;
            fadeDuration = 0.2f;

            if (string.IsNullOrEmpty(raw))
            {
                return;
            }

            var parts = raw.Split(',');
            var rangePart = parts[0].Trim();

            string leftPath = rangePart;
            int tilde = rangePart.IndexOf('~');
            if (tilde >= 0)
            {
                leftPath = rangePart.Substring(0, tilde).Trim();
                int.TryParse(rangePart.Substring(tilde + 1).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out end);
            }

            // leftPath 끝의 숫자를 시작 번호로, 그 앞을 공통 경로(prefix)로 분리한다.
            int idx = leftPath.Length;
            while (idx > 0 && char.IsDigit(leftPath[idx - 1]))
            {
                idx--;
            }

            prefix = leftPath.Substring(0, idx);
            int.TryParse(leftPath.Substring(idx), NumberStyles.Integer, CultureInfo.InvariantCulture, out start);

            if (tilde < 0)
            {
                end = start;
            }

            if (parts.Length > 1 && float.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float sd))
            {
                stepDelay = sd;
            }

            if (parts.Length > 2 && float.TryParse(parts[2].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float fd))
            {
                fadeDuration = fd;
            }
        }

        private void ApplyCg(string path)
        {
            if (cgImage == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(path))
            {
                cgImage.gameObject.SetActive(false);
                return;
            }

            cgImage.gameObject.SetActive(true);
            var sprite = Resources.Load<Sprite>(path);
            if (sprite != null)
            {
                cgImage.sprite = sprite;
                cgImage.color = Color.white;
            }
            else
            {
                cgImage.sprite = null;
                cgImage.color = PlaceholderCg;
            }
        }

        private void ApplyAudio(string bgmPath, string sfxPath)
        {
            var audio = AudioManager.Instance;
            if (audio == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(bgmPath))
            {
                var clip = Resources.Load<AudioClip>(bgmPath);
                if (clip != null)
                {
                    audio.PlayBgm(clip);
                }
            }

            if (!string.IsNullOrEmpty(sfxPath))
            {
                var clip = Resources.Load<AudioClip>(sfxPath);
                if (clip != null)
                {
                    audio.PlaySfx(clip);
                }
            }
        }

        private void ApplyDialogue(EndingBeat beat)
        {
            bool hasText = beat.HasText;
            bool hasSpeaker = !string.IsNullOrEmpty(beat.speakerId);

            if (speakerView != null)
            {
                if (hasSpeaker)
                {
                    speakerView.Show(true);
                    speakerView.SetPartnerSide(beat.partnerOnRight);
                    var portrait = string.IsNullOrEmpty(beat.portraitPath)
                        ? null
                        : Resources.Load<Sprite>(beat.portraitPath);
                    speakerView.SetSpeakerPortrait(beat.speakerId, portrait);
                    speakerView.Highlight(beat.speakerId);
                    speakerView.SetBackgroundDim(false);
                }
                else
                {
                    speakerView.Show(false);
                }
            }

            if (dialogueView == null)
            {
                return;
            }

            if (hasText)
            {
                dialogueView.Show(true);
                dialogueView.SetLine(beat.speakerId, beat.text, ResolveTypingCps());
            }
            else
            {
                dialogueView.Show(false);
            }
        }

        private float ResolveTypingCps()
        {
            if (GameManager.Instance != null && GameManager.Instance.Config != null)
            {
                return GameManager.Instance.Config.typingCps;
            }

            return 30f;
        }

        private void ApplyEffect(string effect)
        {
            if (effectOverlay == null)
            {
                return;
            }

            if (_effectRoutine != null)
            {
                StopCoroutine(_effectRoutine);
                _effectRoutine = null;
            }

            switch (effect)
            {
                case "fade":
                    // 검은 화면에서 밝아지는 페이드 인.
                    _effectRoutine = StartCoroutine(LerpOverlay(BlackColor, Color.clear));
                    break;
                case "flash":
                    // 흰 섬광 후 사라짐.
                    _effectRoutine = StartCoroutine(LerpOverlay(FlashColor, Color.clear));
                    break;
                case "sepia":
                    SetOverlay(SepiaColor);
                    break;
                case "red_filter":
                    SetOverlay(RedColor);
                    break;
                case "blackout":
                    _effectRoutine = StartCoroutine(LerpOverlay(GetOverlay(), BlackColor));
                    break;
                default:
                    SetOverlay(Color.clear);
                    break;
            }
        }

        private IEnumerator LerpOverlay(Color from, Color to)
        {
            SetOverlay(from);

            float time = effectDuration > 0f ? effectDuration : 0f;
            if (time <= 0f)
            {
                SetOverlay(to);
                _effectRoutine = null;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < time)
            {
                elapsed += Time.deltaTime;
                SetOverlay(Color.Lerp(from, to, Mathf.Clamp01(elapsed / time)));
                yield return null;
            }

            SetOverlay(to);
            _effectRoutine = null;
        }

        private Color GetOverlay()
        {
            return effectOverlay != null ? effectOverlay.color : Color.clear;
        }

        private void SetOverlay(Color color)
        {
            if (effectOverlay == null)
            {
                return;
            }

            effectOverlay.color = color;
            // 알파가 0이면 클릭을 가로채지 않도록 raycast 비활성.
            effectOverlay.raycastTarget = color.a > 0.001f && color.a >= 0.99f;
        }

        private void Finish()
        {
            if (_finished)
            {
                return;
            }

            _finished = true;

            var gm = GameManager.Instance;
            if (gm == null)
            {
                return;
            }

            if (gm.StateMachine.Current != GameState.Lobby)
            {
                if (gm.StateMachine.CanTransition(GameState.Lobby))
                {
                    gm.StateMachine.TryTransition(GameState.Lobby);
                }
                else
                {
                    gm.StateMachine.Restore(GameState.Lobby);
                }
            }

            gm.Router?.Load(SceneRouter.SceneLobbyAndIntro);
        }
    }
}
