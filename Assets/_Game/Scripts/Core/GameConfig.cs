using UnityEngine;

namespace GemCafe.Core
{
    [CreateAssetMenu(menuName = "GemCafe/GameConfig", fileName = "GameConfig")]
    public class GameConfig : ScriptableObject
    {
        [Header("Lives")]
        public int startingLives = 3;

        [Header("Player")]
        public float moveSpeed = 5f;

        [Header("Interaction")]
        public float interactRadius = 1.5f;

        [Header("Dialogue")]
        public float typingCps = 30f;

        [Header("Crafting")]
        public float traySlideDuration = 0.3f;

        [Header("Day")]
        public int customersPerDay = 3;
        public int totalDays = 3;

        [Header("Camera")]
        public float cameraLerp = 2f;
    }
}