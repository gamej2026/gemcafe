using GemCafe.Core;
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
            GameManager.Instance?.StartNewGame();
        }

        private void OnClickContinue()
        {
            GameManager.Instance?.ContinueGame();
        }

        private void OnClickSettings()
        {
            popupManager?.Open(PopupType.Settings);
        }

        private void OnClickQuit()
        {
            GameManager.Instance?.QuitGame();
        }

        private void OnClickCredits()
        {
            creditsPopup?.Open();
        }
    }
}
