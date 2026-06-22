using UnityEngine;

namespace GemCafe.Data
{
    [System.Serializable]
    public struct DialogueLine
    {
        public string speakerId;
        [TextArea] public string text;
        public Sprite portrait;
    }
}