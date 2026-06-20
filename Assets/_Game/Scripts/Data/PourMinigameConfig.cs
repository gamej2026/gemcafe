using UnityEngine;

namespace GemCafe.Data
{
    [CreateAssetMenu(menuName = "GemCafe/PourMinigameConfig", fileName = "PourMinigameConfig")]
    public class PourMinigameConfig : ScriptableObject
    {
        public float fillSpeed = 0.6f;
        [Range(0f, 1f)] public float targetMin = 0.75f;
        [Range(0f, 1f)] public float targetMax = 0.95f;
        public bool overflowFail = true;
        public float confirmDelay = 0.4f;
    }
}
