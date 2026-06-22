using System.Collections;
using GemCafe.Core;
using GemCafe.Player;
using UnityEngine;
using UnityEngine.UI;

namespace GemCafe.UI
{
    public class LobbyMenu : MonoBehaviour
    {
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private Button creditsButton;
        [SerializeField] private PopupManager popupManager;
        [SerializeField] private CreditsPopup creditsPopup;
        [SerializeField] private Image lobbyFadeOutImage;

        [Header("Game Start")]
        [Tooltip("게임 시작 시 활성화할 메인 카메라의 CameraFollow.")]
        [SerializeField] private CameraFollow cameraFollow;

        [Tooltip("게임 시작 시 숨길 로비 UI(타이틀, 버튼 등).")]
        [SerializeField] private GameObject[] lobbyUiToHide;

        [Tooltip("lobbyFadeOutImage 가 사라지는 데 걸리는 시간(초).")]
        [SerializeField] private float fadeOutDuration = 1f;

        [Tooltip("모바일 좌/우 이동 버튼을 게임 시작 직후 잠깐 보여줄 시간(초).")]
        [SerializeField] private float mobileMoveButtonsDuration = 2f;

        private bool _starting;

        private void Awake()
        {
            if (newGameButton != null)
            {
                newGameButton.onClick.AddListener(OnClickNewGame);
            }

            if (continueButton != null)
            {
                continueButton.onClick.AddListener(OnClickContinue);
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.AddListener(OnClickSettings);
            }

            if (quitButton != null)
            {
                quitButton.onClick.AddListener(OnClickQuit);
            }

            if (creditsButton != null)
            {
                creditsButton.onClick.AddListener(OnClickCredits);
            }
        }

        private void Start()
        {
            // 로비에서는 카메라가 주인공을 따라가지 않도록 비활성화한다.
            if (cameraFollow != null)
            {
                cameraFollow.enabled = false;
            }

            AudioManager.Instance?.PlayLobbyBgm();

            if (continueButton != null)
            {
                continueButton.interactable = SaveSystem.HasSave();
            }
        }

        private void OnDestroy()
        {
            if (newGameButton != null)
            {
                newGameButton.onClick.RemoveListener(OnClickNewGame);
            }

            if (continueButton != null)
            {
                continueButton.onClick.RemoveListener(OnClickContinue);
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.RemoveListener(OnClickSettings);
            }

            if (quitButton != null)
            {
                quitButton.onClick.RemoveListener(OnClickQuit);
            }

            if (creditsButton != null)
            {
                creditsButton.onClick.RemoveListener(OnClickCredits);
            }
        }

        private void OnClickNewGame()
        {
            if (_starting)
            {
                return;
            }

            _starting = true;
            AudioManager.Instance?.PlayClick();

            // 씬을 새로 로드하지 않고(스테이지가 이미 같은 씬에 있음) 게임 상태만 초기화한다.
            GameManager.Instance?.StartNewGame(false);

            // 게임이 시작되면 카메라가 주인공을 따라가도록 활성화한다.
            if (cameraFollow != null)
            {
                cameraFollow.enabled = true;
            }

            TouchControls.ShowMoveButtonsTemporarily(mobileMoveButtonsDuration);

            StartCoroutine(StartGameRoutine());
        }

        private IEnumerator StartGameRoutine()
        {
            // 로비 UI(타이틀/버튼)를 즉시 숨긴다.
            if (lobbyUiToHide != null)
            {
                foreach (var go in lobbyUiToHide)
                {
                    if (go != null)
                    {
                        go.SetActive(false);
                    }
                }
            }

            // lobbyFadeOutImage를 FadeOut(알파 0)시키며 게임 화면을 드러낸다.
            if (lobbyFadeOutImage != null)
            {
                var color = lobbyFadeOutImage.color;
                float startAlpha = color.a;
                lobbyFadeOutImage.raycastTarget = false;

                if (fadeOutDuration > 0f)
                {
                    float t = 0f;
                    while (t < fadeOutDuration)
                    {
                        t += Time.deltaTime;
                        float a = Mathf.Lerp(startAlpha, 0f, t / fadeOutDuration);
                        lobbyFadeOutImage.color = new Color(color.r, color.g, color.b, a);
                        yield return null;
                    }
                }

                lobbyFadeOutImage.color = new Color(color.r, color.g, color.b, 0f);
                lobbyFadeOutImage.gameObject.SetActive(false);
            }
        }

        private void OnClickContinue()
        {
            AudioManager.Instance?.PlayClick();
            GameManager.Instance?.ContinueGame();
        }

        private void OnClickSettings()
        {
            AudioManager.Instance?.PlayClick();
            popupManager?.Open(PopupType.Settings);
        }

        private void OnClickQuit()
        {
            AudioManager.Instance?.PlayClick();
            GameManager.Instance?.QuitGame();
        }

        private void OnClickCredits()
        {
            AudioManager.Instance?.PlayClick();
            creditsPopup?.Open();
        }
    }
}
