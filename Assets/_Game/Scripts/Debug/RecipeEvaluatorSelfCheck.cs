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
            var water = ScriptableObject.CreateInstance<IngredientSO>();
            var syrup = ScriptableObject.CreateInstance<IngredientSO>();
            var wrong = ScriptableObject.CreateInstance<IngredientSO>();

            var recipe = ScriptableObject.CreateInstance<RecipeSO>();
            recipe.ingredients = new[] { water, syrup };

            // 재료 전체 일치(2/2)
            Check(recipe, new[] { water, syrup }, 1, DrinkResult.GreatSuccess, "전체일치+미니1성공");
            Check(recipe, new[] { water, syrup }, 2, DrinkResult.GreatSuccess, "전체일치+미니2성공");
            Check(recipe, new[] { water, syrup }, 0, DrinkResult.Success, "전체일치+미니0성공");
            // 재료 일부 일치(1/2)
            Check(recipe, new[] { water }, 1, DrinkResult.Success, "일부일치+미니1성공");
            Check(recipe, new[] { water }, 0, DrinkResult.Fail, "일부일치+미니0성공");
            // 재료 일치 없음
            Check(recipe, new[] { wrong }, 2, DrinkResult.Fail, "일치없음");

            Object.DestroyImmediate(recipe);
            Object.DestroyImmediate(water);
            Object.DestroyImmediate(syrup);
            Object.DestroyImmediate(wrong);
        }

        private static void Check(RecipeSO recipe, IngredientSO[] bowlItems, int minigameSuccessCount, DrinkResult expected, string label)
        {
            var bowl = new List<IngredientSO>(bowlItems);

            var actual = RecipeEvaluator.Evaluate(bowl, recipe, minigameSuccessCount);
            if (actual == expected)
            {
                Debug.Log($"[SelfCheck] PASS {label} => {actual}");
            }
            else
            {
                Debug.LogError($"[SelfCheck] FAIL {label} => {actual} (expected {expected})");
            }
        }
    }
}
#endif
