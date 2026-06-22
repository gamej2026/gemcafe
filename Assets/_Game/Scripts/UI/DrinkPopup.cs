using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace GemCafe.UI
{
    public class DrinkPopup : MonoBehaviour
    {
        [SerializeField] private CanvasGroup root;
        [SerializeField] private Image drinkImage;
        [SerializeField] private GameObject sparkle;
        [SerializeField] private Text nameLabel;
        [SerializeField] private float autoAdvance = 1.5f;

        private Coroutine _routine;

        public void Show(Sprite drink, Action onDone = null)
        {
            Show(drink, string.Empty, onDone);
        }

        public void Show(Sprite drink, string drinkName, Action onDone = null)
        {
            if (drinkImage != null)
            {
                drinkImage.sprite = drink;
                drinkImage.enabled = drink != null;
            }

            if (nameLabel != null)
            {
                nameLabel.text = drinkName;
                nameLabel.enabled = !string.IsNullOrEmpty(drinkName);
            }

            if (sparkle != null)
            {
                sparkle.SetActive(true);
            }

            SetVisible(true);

            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }

            _routine = StartCoroutine(Routine(onDone));
        }

        private IEnumerator Routine(Action onDone)
        {
            var wait = autoAdvance > 0f ? autoAdvance : 0f;
            if (wait > 0f)
            {
                yield return new WaitForSeconds(wait);
            }

            SetVisible(false);
            _routine = null;
            onDone?.Invoke();
        }

        private void SetVisible(bool visible)
        {
            if (root == null)
            {
                return;
            }

            root.alpha = visible ? 1f : 0f;
            root.interactable = visible;
            root.blocksRaycasts = visible;
        }
    }
}
