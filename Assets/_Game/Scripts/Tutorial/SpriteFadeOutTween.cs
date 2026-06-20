using System.Collections;
using UnityEngine;

namespace GemCafe.Tutorial
{
    /// <summary>
    /// 튜토리얼 등에서 스폰된 프리팹이 화면에서 사라질 때 재생할 "사라지는 트윈"의 계약.
    /// 스폰 측(<see cref="CafeTutorialDirector"/>)은 이 인터페이스가 있으면 트윈을 재생하고
    /// 완료될 때까지 기다린 뒤 오브젝트를 제거한다.
    /// </summary>
    public interface ITutorialSpawnDisappear
    {
        /// <summary>사라지는 트윈을 재생한다. 트윈이 끝나면 코루틴이 종료된다.</summary>
        IEnumerator PlayDisappear();
    }

    /// <summary>
    /// 자신과 자식의 모든 <see cref="SpriteRenderer"/> 알파를 0으로 줄여 부드럽게 사라지는 트윈.
    /// 코루틴 기반이라 외부 트윈 라이브러리에 의존하지 않는다.
    /// </summary>
    [DisallowMultipleComponent]
    public class SpriteFadeOutTween : MonoBehaviour, ITutorialSpawnDisappear
    {
        [Tooltip("알파가 0이 될 때까지 걸리는 시간(초).")]
        [SerializeField] private float duration = 0.6f;

        private bool _playing;

        /// <summary>트윈이 재생 중인지 여부.</summary>
        public bool IsPlaying => _playing;

        /// <summary>
        /// 모든 SpriteRenderer 의 알파를 시작값에서 0까지 부드럽게(smoothstep) 낮춘다.
        /// 이미 재생 중이면 중복 실행하지 않는다.
        /// </summary>
        public IEnumerator PlayDisappear()
        {
            if (_playing)
            {
                yield break;
            }

            _playing = true;

            var renderers = GetComponentsInChildren<SpriteRenderer>(true);
            var startAlphas = new float[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                startAlphas[i] = renderers[i] != null ? renderers[i].color.a : 0f;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
                // smoothstep: 시작/끝이 완만한 1->0 감쇠.
                float remaining = 1f - Mathf.SmoothStep(0f, 1f, t);

                for (int i = 0; i < renderers.Length; i++)
                {
                    var sr = renderers[i];
                    if (sr == null)
                    {
                        continue;
                    }

                    var c = sr.color;
                    c.a = startAlphas[i] * remaining;
                    sr.color = c;
                }

                yield return null;
            }

            // 마지막 프레임에서 확실히 0으로 마무리.
            for (int i = 0; i < renderers.Length; i++)
            {
                var sr = renderers[i];
                if (sr == null)
                {
                    continue;
                }

                var c = sr.color;
                c.a = 0f;
                sr.color = c;
            }

            _playing = false;
        }
    }
}
