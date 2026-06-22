using UnityEngine;

namespace GemCafe.Data
{
    [CreateAssetMenu(menuName = "GemCafe/MixMinigameConfig", fileName = "MixMinigameConfig")]
    public class MixMinigameConfig : ScriptableObject
    {
        [Range(0f, 1f)] public float startProgress = 0.4f;
        public float barRiseAccel = 1800f;
        public float barGravity = 1200f;
        public float barMaxSpeed = 900f;
        public float barHeight = 220f;
        public float progressGainRate = 0.45f;
        public float progressLossRate = 0.35f;
        public float leafAmplitude = 180f;
        public float leafFrequency = 0.5f;
        public AnimationCurve leafPattern = AnimationCurve.Linear(0f, 0f, 1f, 0f);
    }
}
