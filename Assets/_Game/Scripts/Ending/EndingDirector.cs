using System.Collections;
using System.Collections.Generic;
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

        private void Start()
        {
            var kind = GameManager.Instance != null ? GameManager.Instance.PendingEnding : EndingKind.B;
            _beats = EndingCsvLoader.Load(kind);

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
            ApplyEffect(beat.effect);
            ApplyBackground(beat.bgPath);
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

            gm.Router?.Load(SceneRouter.SceneLobby);
        }
    }
}
