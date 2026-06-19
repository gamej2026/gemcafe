using GemCafe.Core;
using UnityEngine;

namespace GemCafe.UI
{
    public class HUD : MonoBehaviour
    {
        [SerializeField] private UnityEngine.UI.Image[] lifeIcons;
        [SerializeField] private UnityEngine.UI.Image patienceFill;
        [SerializeField] private GameObject[] lifeObjects;

        private void OnEnable()
        {
            EventBus.OnLivesChanged += HandleLives;
            EventBus.OnPatienceChanged += HandlePatience;

            var gm = GameManager.Instance;
            HandleLives(gm != null && gm.Lives != null ? gm.Lives.Current : 0);
        }

        private void OnDisable()
        {
            EventBus.OnLivesChanged -= HandleLives;
            EventBus.OnPatienceChanged -= HandlePatience;
        }

        private void HandleLives(int lives)
        {
            if (lifeIcons != null)
            {
                for (int i = 0; i < lifeIcons.Length; i++)
                {
                    if (lifeIcons[i] != null)
                    {
                        lifeIcons[i].enabled = i < lives;
                    }
                }
            }

            if (lifeObjects != null)
            {
                for (int i = 0; i < lifeObjects.Length; i++)
                {
                    if (lifeObjects[i] != null)
                    {
                        lifeObjects[i].SetActive(i < lives);
                    }
                }
            }
        }

        private void HandlePatience(float ratio)
        {
            if (patienceFill != null)
            {
                patienceFill.fillAmount = Mathf.Clamp01(ratio);
            }
        }
    }
}
