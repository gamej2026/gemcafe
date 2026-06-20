using System;
using System.Collections;
using GemCafe.Core;
using UnityEngine;

namespace GemCafe.UI
{
    public class ServeSequence : MonoBehaviour
    {
        [SerializeField] private Animator serveAnimator;
        [SerializeField] private string offerTrigger = "Offer";
        [SerializeField] private string drinkTrigger = "Drink";
        [SerializeField] private float stepDuration = 1f;

        private Coroutine _routine;

        public void Play(Action onDone = null)
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }

            _routine = StartCoroutine(Routine(onDone));
        }

        private IEnumerator Routine(Action onDone)
        {
            if (serveAnimator != null && !string.IsNullOrEmpty(offerTrigger))
            {
                serveAnimator.SetTrigger(offerTrigger);
            }

            AudioManager.Instance?.PlayOffer();

            var step = stepDuration > 0f ? stepDuration : 0f;
            if (step > 0f)
            {
                yield return new WaitForSeconds(step);
            }

            if (serveAnimator != null && !string.IsNullOrEmpty(drinkTrigger))
            {
                serveAnimator.SetTrigger(drinkTrigger);
            }

            AudioManager.Instance?.PlayDrink();

            if (step > 0f)
            {
                yield return new WaitForSeconds(step);
            }

            _routine = null;
            onDone?.Invoke();
        }
    }
}
