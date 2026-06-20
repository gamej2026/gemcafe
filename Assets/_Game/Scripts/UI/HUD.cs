using System.Collections.Generic;
using GemCafe.Core;
using UnityEngine;

namespace GemCafe.UI
{
    public class HUD : MonoBehaviour
    {
        [SerializeField] private GameObject[] lifeObjects;
        [SerializeField] private UnityEngine.UI.Image[] coinSlots;
        [SerializeField] private Sprite normalCoinSprite;
        [SerializeField] private Sprite goldCoinSprite;
        [SerializeField] private Color normalCoinColor = new Color(0.78f, 0.80f, 0.85f, 1f);
        [SerializeField] private Color goldCoinColor = new Color(1f, 0.84f, 0.25f, 1f);
        [SerializeField] private Color emptySlotColor = new Color(0.2f, 0.2f, 0.22f, 0.5f);

        private void OnEnable()
        {
            EventBus.OnLivesChanged += HandleLives;
            EventBus.OnCoinSlotsChanged += HandleCoinSlots;
            RefreshLives();
        }

        private void Start()
        {
            RefreshLives();
            HandleCoinSlots(null);
        }

        private void OnDisable()
        {
            EventBus.OnLivesChanged -= HandleLives;
            EventBus.OnCoinSlotsChanged -= HandleCoinSlots;
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

        private void HandleCoinSlots(IReadOnlyList<CoinType> coins)
        {
            if (coinSlots == null)
            {
                return;
            }

            for (int i = 0; i < coinSlots.Length; i++)
            {
                var slot = coinSlots[i];
                if (slot == null)
                {
                    continue;
                }

                if (coins != null && i < coins.Count)
                {
                    var isGold = coins[i] == CoinType.Gold;
                    var sprite = isGold ? goldCoinSprite : normalCoinSprite;
                    if (sprite != null)
                    {
                        slot.sprite = sprite;
                    }
                    slot.color = isGold ? goldCoinColor : normalCoinColor;
                }
                else
                {
                    slot.color = emptySlotColor;
                }
            }
        }
    }
}
