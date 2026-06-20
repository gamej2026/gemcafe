using GemCafe.Core;
using UnityEngine;

namespace GemCafe.UI
{
    public class HUD : MonoBehaviour
    {
        [SerializeField] private UnityEngine.UI.Image[] lifeIcons;
        [SerializeField] private UnityEngine.UI.Image patienceFill;
        [SerializeField] private GameObject[] lifeObjects;
        [SerializeField] private UnityEngine.UI.Text coinText;
        [SerializeField] private string coinFormat = "{0}";

        private void OnEnable()
        {
            EventBus.OnLivesChanged += HandleLives;
            EventBus.OnPatienceChanged += HandlePatience;
            EventBus.OnCoinsChanged += HandleCoins;
            RefreshLives();
        }

        private void Start()
        {
            RefreshLives();
            HandleCoins(0);
        }

        private void OnDisable()
        {
            EventBus.OnLivesChanged -= HandleLives;
            EventBus.OnPatienceChanged -= HandlePatience;
            EventBus.OnCoinsChanged -= HandleCoins;
        }

        private void RefreshLives()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.Lives == null)
            {
                // GameManager가 아직 초기화되지 않았으면 아이콘을 끄지 않고
                // Start 시점에 다시 갱신한다. (early OnEnable로 인한 오프 방지)
                return;
            }

            HandleLives(gm.Lives.Current);
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

        private void HandleCoins(int total)
        {
            if (coinText != null)
            {
                coinText.text = string.Format(coinFormat, total);
            }
        }
    }
}
