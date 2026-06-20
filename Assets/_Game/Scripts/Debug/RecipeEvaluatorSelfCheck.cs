#if UNITY_EDITOR
using System.Collections.Generic;
using GemCafe.Data;
using UnityEditor;
using UnityEngine;

namespace GemCafe.Crafting
{
    public static class RecipeEvaluatorSelfCheck
    {
        [MenuItem("GemCafe/Run RecipeEvaluator SelfCheck")]
        public static void Run()
        {
            var recipe = ScriptableObject.CreateInstance<RecipeSO>();
            recipe.coreTaste = Taste.Sweet;
            recipe.subTastes = new[] { Taste.Sour, Taste.Spicy };

            Check(recipe, new[] { Taste.Sweet, Taste.Spicy, Taste.Salty }, DrinkResult.Success, "단맛,매운맛,짠맛");
            Check(recipe, new[] { Taste.Spicy, Taste.Sour, Taste.Salty }, DrinkResult.Fail, "매운맛,신맛,짠맛");
            Check(recipe, new[] { Taste.Sweet, Taste.Sour, Taste.Umami }, DrinkResult.Success, "단맛,신맛,감칠맛");
            Check(recipe, new[] { Taste.Sweet, Taste.Sour, Taste.Spicy }, DrinkResult.GreatSuccess, "단맛,신맛,매운맛");

            Object.DestroyImmediate(recipe);
        }

        private static void Check(RecipeSO recipe, Taste[] tastes, DrinkResult expected, string label)
        {
            var bowl = new List<IngredientSO>();
            for (int i = 0; i < tastes.Length; i++)
            {
                var ing = ScriptableObject.CreateInstance<IngredientSO>();
                ing.taste = tastes[i];
                bowl.Add(ing);
            }

            var actual = RecipeEvaluator.Evaluate(bowl, recipe);
            if (actual == expected)
            {
                Debug.Log($"[SelfCheck] PASS {label} => {actual}");
            }
            else
            {
                Debug.LogError($"[SelfCheck] FAIL {label} => {actual} (expected {expected})");
            }

            for (int i = 0; i < bowl.Count; i++)
            {
                Object.DestroyImmediate(bowl[i]);
            }
        }
    }
}
#endif
