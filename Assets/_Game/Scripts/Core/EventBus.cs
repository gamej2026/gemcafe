using System;
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
        public static event Action OnPatienceDepleted;
        public static event Action<float> OnPatienceChanged;
        public static event Action<int> OnLivesChanged;
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

        public static void RaisePatienceDepleted()
        {
            OnPatienceDepleted?.Invoke();
        }

        public static void RaisePatienceChanged(float patienceNormalized)
        {
            OnPatienceChanged?.Invoke(patienceNormalized);
        }

        public static void RaiseLivesChanged(int lives)
        {
            OnLivesChanged?.Invoke(lives);
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
            OnPatienceDepleted = null;
            OnPatienceChanged = null;
            OnLivesChanged = null;
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