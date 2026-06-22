using System;
using System.Collections;
using UnityEngine;

namespace GemCafe.Crafting
{
    public class MixAnimation : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private string triggerName = "Mix";
        [SerializeField] private float fallbackDuration = 1f;

        private Coroutine _playRoutine;

        public void Play(Action onComplete = null)
        {
            if (_playRoutine != null)
            {
                StopCoroutine(_playRoutine);
                _playRoutine = null;
            }

            if (animator != null)
            {
                animator.SetTrigger(triggerName);
            }

            _playRoutine = StartCoroutine(PlayRoutine(onComplete));
        }

        private IEnumerator PlayRoutine(Action onComplete)
        {
            var wait = fallbackDuration > 0f ? fallbackDuration : 0f;
            if (wait > 0f)
            {
                yield return new WaitForSeconds(wait);
            }

            _playRoutine = null;
            onComplete?.Invoke();
        }
    }
}
