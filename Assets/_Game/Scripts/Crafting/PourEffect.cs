using UnityEngine;

namespace GemCafe.Crafting
{
    /// <summary>
    /// Pour_Effect 오브젝트에 부착되어, 인스펙터에서 직렬화로 할당한 ParticleSystem을
    /// 재생/정지한다. 파티클 시스템이 할당되지 않았으면 아무 동작도 하지 않는다.
    /// </summary>
    public class PourEffect : MonoBehaviour
    {
        [SerializeField] private ParticleSystem effect;

        /// <summary>할당된 파티클 시스템이 있으면 처음부터 재생한다.</summary>
        public void Play()
        {
            if (effect == null)
            {
                return;
            }

            effect.Clear(true);
            effect.Play(true);
        }

        /// <summary>재생 중인 파티클을 멈추고 즉시 비운다.</summary>
        public void StopAndClear()
        {
            if (effect == null)
            {
                return;
            }

            effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
