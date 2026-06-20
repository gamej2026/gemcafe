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

        public static DrinkResult Evaluate(IReadOnlyList<IngredientSO> bowl, RecipeSO target)
        {
            if (target == null)
            {
                return DrinkResult.Fail;
            }

            var tastes = new HashSet<Taste>();
            if (bowl != null)
            {
                for (int i = 0; i < bowl.Count; i++)
                {
                    if (bowl[i] != null)
                    {
                        tastes.Add(bowl[i].taste);
                    }
                }
            }

            if (!tastes.Contains(target.coreTaste))
            {
                return DrinkResult.Fail;
            }

            int matchCount = 0;
            if (target.subTastes != null)
            {
                var counted = new HashSet<Taste>();
                for (int i = 0; i < target.subTastes.Length; i++)
                {
                    var sub = target.subTastes[i];
                    if (counted.Add(sub) && tastes.Contains(sub))
                    {
                        matchCount++;
                    }
                }
            }

            if (matchCount >= 2)
            {
                return DrinkResult.GreatSuccess;
            }

            return DrinkResult.Success;
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