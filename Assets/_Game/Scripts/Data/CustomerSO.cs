using UnityEngine;

namespace GemCafe.Data
{
    [CreateAssetMenu(menuName = "GemCafe/Customer", fileName = "Customer")]
    public class CustomerSO : ScriptableObject
    {
        public string id;
        public Sprite portrait;
        public DialogueLine[] orderDialogue;
        public RecipeSO targetRecipe;
        public float patience;
        [Range(1, 3)] public int day;
    }
}