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

        /// <summary>
        /// 그릇에 담긴 재료 중 목표 레시피가 요구하는 재료가 몇 종류 일치하는지 센다.
        /// </summary>
        public static int CountIngredientMatches(IReadOnlyList<IngredientSO> bowl, RecipeSO target)
        {
            if (target == null || target.ingredients == null)
            {
                return 0;
            }

            var bowlSet = new HashSet<IngredientSO>();
            if (bowl != null)
            {
                for (int i = 0; i < bowl.Count; i++)
                {
                    if (bowl[i] != null)
                    {
                        bowlSet.Add(bowl[i]);
                    }
                }
            }

            var counted = new HashSet<IngredientSO>();
            int matches = 0;
            for (int i = 0; i < target.ingredients.Length; i++)
            {
                var ingredient = target.ingredients[i];
                if (ingredient != null && counted.Add(ingredient) && bowlSet.Contains(ingredient))
                {
                    matches++;
                }
            }

            return matches;
        }

        /// <summary>
        /// 목표 레시피가 요구하는 재료의 종류 수.
        /// </summary>
        public static int CountRequiredIngredients(RecipeSO target)
        {
            if (target == null || target.ingredients == null)
            {
                return 0;
            }

            var counted = new HashSet<IngredientSO>();
            for (int i = 0; i < target.ingredients.Length; i++)
            {
                var ingredient = target.ingredients[i];
                if (ingredient != null)
                {
                    counted.Add(ingredient);
                }
            }

            return counted.Count;
        }

        /// <summary>
        /// 재료 일치 수와 미니게임 성공 횟수를 합산해 최종 판정한다.
        /// - 재료 전체 일치 + 미니게임 1개 이상 성공 → 대성공(GreatSuccess)
        /// - 재료 전체 일치 + 미니게임 모두 실패     → 성공(Success)
        /// - 재료 일부 일치 + 미니게임 1개 이상 성공 → 성공(Success)
        /// - 재료 일부 일치 + 미니게임 모두 실패     → 실패(Fail)
        /// - 재료 일치 없음                          → 실패(Fail)
        /// </summary>
        public static DrinkResult Evaluate(IReadOnlyList<IngredientSO> bowl, RecipeSO target, int minigameSuccessCount)
        {
            int required = CountRequiredIngredients(target);
            if (required <= 0)
            {
                return DrinkResult.Fail;
            }

            int matches = CountIngredientMatches(bowl, target);
            if (matches <= 0)
            {
                return DrinkResult.Fail;
            }

            bool fullMatch = matches >= required;
            bool minigamePassed = minigameSuccessCount >= 1;

            if (fullMatch)
            {
                return minigamePassed ? DrinkResult.GreatSuccess : DrinkResult.Success;
            }

            return minigamePassed ? DrinkResult.Success : DrinkResult.Fail;
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