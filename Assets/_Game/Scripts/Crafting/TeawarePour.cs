using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GemCafe.Crafting
{
    public class TeawarePour : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private CraftingController controller;
        [SerializeField] private Animator pourAnimator;
        [SerializeField] private string tiltTrigger = "Tilt";
        [SerializeField] private string waterTrigger = "Water";
        [SerializeField] private GameObject guideHint;
        [SerializeField] private float pourDuration = 1.2f;

        private bool _interactable;
        private Coroutine _pourRoutine;

        public void SetInteractable(bool value)
        {
            _interactable = value;
            if (guideHint != null)
            {
                guideHint.SetActive(value);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!_interactable || controller == null)
            {
                return;
            }

            controller.OnTeawareClicked();
        }

        public void PlayPour(Action onDone)
        {
            if (_pourRoutine != null)
            {
                StopCoroutine(_pourRoutine);
            }

            _pourRoutine = StartCoroutine(PourRoutine(onDone));
        }

        private IEnumerator PourRoutine(Action onDone)
        {
            if (pourAnimator != null)
            {
                if (!string.IsNullOrEmpty(tiltTrigger))
                {
                    pourAnimator.SetTrigger(tiltTrigger);
                }

                if (!string.IsNullOrEmpty(waterTrigger))
                {
                    pourAnimator.SetTrigger(waterTrigger);
                }
            }

            var wait = pourDuration > 0f ? pourDuration : 0f;
            if (wait > 0f)
            {
                yield return new WaitForSeconds(wait);
            }

            _pourRoutine = null;
            onDone?.Invoke();
        }
    }
}
