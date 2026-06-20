using UnityEngine;

namespace GemCafe.Data
{
    [CreateAssetMenu(menuName = "GemCafe/Recipe", fileName = "Recipe")]
    public class RecipeSO : ScriptableObject
    {
        public string id;
        public string drinkName;
        public IngredientSO[] ingredients;
        public Taste coreTaste;
        public Taste[] subTastes;
    }
}