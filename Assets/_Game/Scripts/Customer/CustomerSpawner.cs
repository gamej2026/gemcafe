using System;
using System.Collections;
using GemCafe.Core;
using GemCafe.Data;
using UnityEngine;

namespace GemCafe.Customer
{
    public class CustomerSpawner : MonoBehaviour
    {
        [SerializeField] private UnityEngine.UI.Image customerImage;
        [SerializeField] private float fadeDuration = 0.5f;

        private Coroutine _fadeRoutine;

        public CustomerSO Current { get; private set; }

        public void Spawn(CustomerSO customer, Action onArrived = null)
        {
            Current = customer;

            AudioManager.Instance?.PlayCustomerBell();

            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.StateMachine.SetServiceSub(ServiceSubState.CustomerEnter);
            }

            if (_fadeRoutine != null)
            {
                StopCoroutine(_fadeRoutine);
                _fadeRoutine = null;
            }

            if (customerImage == null)
            {
                EventBus.RaiseCustomerArrived(customer);
                onArrived?.Invoke();
                return;
            }

            customerImage.sprite = customer != null ? customer.portrait : null;
            SetImageAlpha(0f);
            _fadeRoutine = StartCoroutine(FadeIn(customer, onArrived));
        }

        public void Clear()
        {
            Current = null;

            if (_fadeRoutine != null)
            {
                StopCoroutine(_fadeRoutine);
                _fadeRoutine = null;
            }

            SetImageAlpha(0f);
        }

        /// <summary>현재 표시 중인 손님 이미지를 다른 스프라이트로 교체한다(알파 유지). 대사별 감정 교체에 사용.</summary>
        public void SetPortraitSprite(Sprite sprite)
        {
            if (customerImage == null || sprite == null)
            {
                return;
            }

            customerImage.sprite = sprite;
        }

        public void FadeOutAndClear(Action onComplete = null)
        {
            if (_fadeRoutine != null)
            {
                StopCoroutine(_fadeRoutine);
                _fadeRoutine = null;
            }

            if (customerImage == null)
            {
                Current = null;
                onComplete?.Invoke();
                return;
            }

            _fadeRoutine = StartCoroutine(FadeOut(onComplete));
        }

        private IEnumerator FadeOut(Action onComplete)
        {
            float elapsed = 0f;
            float duration = fadeDuration > 0f ? fadeDuration : 0f;
            float startAlpha = customerImage != null ? customerImage.color.a : 1f;

            if (duration <= 0f)
            {
                SetImageAlpha(0f);
            }
            else
            {
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    var t = Mathf.Clamp01(elapsed / duration);
                    SetImageAlpha(Mathf.Lerp(startAlpha, 0f, t));
                    yield return null;
                }

                SetImageAlpha(0f);
            }

            _fadeRoutine = null;
            Current = null;
            onComplete?.Invoke();
        }

        private IEnumerator FadeIn(CustomerSO customer, Action onArrived)
        {
            float elapsed = 0f;
            float duration = fadeDuration > 0f ? fadeDuration : 0f;

            if (duration <= 0f)
            {
                SetImageAlpha(1f);
            }
            else
            {
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    var alpha = Mathf.Clamp01(elapsed / duration);
                    SetImageAlpha(alpha);
                    yield return null;
                }

                SetImageAlpha(1f);
            }

            _fadeRoutine = null;
            EventBus.RaiseCustomerArrived(customer);
            onArrived?.Invoke();
        }

        private void SetImageAlpha(float alpha)
        {
            if (customerImage == null)
            {
                return;
            }

            var color = customerImage.color;
            color.a = Mathf.Clamp01(alpha);
            customerImage.color = color;
        }
    }
}
