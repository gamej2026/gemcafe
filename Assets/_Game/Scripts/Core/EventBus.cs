using System;
using System.Collections.Generic;
using GemCafe.Data;
using UnityEngine;

namespace GemCafe.Core
{
    public static class EventBus
    {
        public static event Action OnDialogueStarted;
        public static event Action OnDialogueEnded;
        public static event Action<CustomerSO> OnCustomerArrived;
        public static event Action OnCraftStarted;
        public static event Action<IngredientSO> OnIngredientAdded;
        public static event Action<RecipeSO> OnDrinkCompleted;
        public static event Action<DrinkResult> OnDrinkResult;
        public static event Action<int> OnLivesChanged;
        public static event Action<int> OnCoinsChanged;
        public static event Action<IReadOnlyList<CoinType>> OnCoinSlotsChanged;
        public static event Action<int> OnDayCompleted;
        public static event Action<GameState, GameState> OnStateChanged;

        public static void RaiseDialogueStarted()
        {
            OnDialogueStarted?.Invoke();
        }

        public static void RaiseDialogueEnded()
        {
            OnDialogueEnded?.Invoke();
        }

        public static void RaiseCustomerArrived(CustomerSO customer)
        {
            OnCustomerArrived?.Invoke(customer);
        }

        public static void RaiseCraftStarted()
        {
            OnCraftStarted?.Invoke();
        }

        public static void RaiseIngredientAdded(IngredientSO ingredient)
        {
            OnIngredientAdded?.Invoke(ingredient);
        }

        public static void RaiseDrinkCompleted(RecipeSO recipe)
        {
            OnDrinkCompleted?.Invoke(recipe);
        }

        public static void RaiseDrinkResult(DrinkResult result)
        {
            OnDrinkResult?.Invoke(result);
        }

        public static void RaiseLivesChanged(int lives)
        {
            OnLivesChanged?.Invoke(lives);
        }

        public static void RaiseCoinsChanged(int total)
        {
            OnCoinsChanged?.Invoke(total);
        }

        public static void RaiseCoinSlotsChanged(IReadOnlyList<CoinType> slots)
        {
            OnCoinSlotsChanged?.Invoke(slots);
        }

        public static void RaiseDayCompleted(int day)
        {
            OnDayCompleted?.Invoke(day);
        }

        public static void RaiseStateChanged(GameState from, GameState to)
        {
            OnStateChanged?.Invoke(from, to);
        }

        public static void ClearAll()
        {
            OnDialogueStarted = null;
            OnDialogueEnded = null;
            OnCustomerArrived = null;
            OnCraftStarted = null;
            OnIngredientAdded = null;
            OnDrinkCompleted = null;
            OnDrinkResult = null;
            OnLivesChanged = null;
            OnCoinsChanged = null;
            OnCoinSlotsChanged = null;
            OnDayCompleted = null;
            OnStateChanged = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            ClearAll();
        }
    }
}