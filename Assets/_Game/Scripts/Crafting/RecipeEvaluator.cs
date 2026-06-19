using System.Collections.Generic;
using GemCafe.Data;

namespace GemCafe.Crafting
{
    public static class RecipeEvaluator
    {
        public static bool Matches(IReadOnlyList<IngredientSO> bowl, RecipeSO target)
        {
            var bowlCounts = BuildCounts(bowl);
            var targetCounts = BuildCounts(target != null ? target.ingredients : null);

            if (bowlCounts.Count != targetCounts.Count)
            {
                return false;
            }

            foreach (var pair in bowlCounts)
            {
                if (!targetCounts.TryGetValue(pair.Key, out var targetCount) || targetCount != pair.Value)
                {
                    return false;
                }
            }

            return true;
        }

        private static Dictionary<IngredientSO, int> BuildCounts(IReadOnlyList<IngredientSO> source)
        {
            var counts = new Dictionary<IngredientSO, int>();
            if (source == null)
            {
                return counts;
            }

            for (int i = 0; i < source.Count; i++)
            {
                var ingredient = source[i];
                if (ingredient == null)
                {
                    continue;
                }

                if (counts.TryGetValue(ingredient, out var current))
                {
                    counts[ingredient] = current + 1;
                }
                else
                {
                    counts[ingredient] = 1;
                }
            }

            return counts;
        }
    }
}