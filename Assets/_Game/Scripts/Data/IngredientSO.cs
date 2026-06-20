using UnityEngine;

namespace GemCafe.Data
{
    [CreateAssetMenu(menuName = "GemCafe/Ingredient", fileName = "Ingredient")]
    public class IngredientSO : ScriptableObject
    {
        public string id;
        public string displayName;
        public Sprite icon;
        public IngredientCategory category;
        public Taste taste;
    }
}