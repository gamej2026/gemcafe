using GemCafe.Core;
using GemCafe.Data;
using GemCafe.UI;
using UnityEngine;

namespace GemCafe.Crafting
{
    public enum CraftStage
    {
        None,
        IngredientSelect,
        MixMinigame,
        PourPrep,
        PourMinigame,
        DrinkComplete,
        Done
    }

    public class CraftingController : MonoBehaviour
    {
        [SerializeField] private TrayController tray;
        [SerializeField] private BowlReceiver bowl;
        [SerializeField] private PestleMixer pestle;
        [SerializeField] private MixMinigame mixMinigame;
        [SerializeField] private PourMinigame pourMinigame;
        [SerializeField] private TeawarePour teaware;
        [SerializeField] private DrinkPopup drinkPopup;
        [SerializeField] private ServeSequence serveSequence;
        [SerializeField] private ScreenTransition dualView;

        private RecipeSO _targetRecipe;
        private DrinkResult _pendingResult;
        private bool _resultRaised;

        public CraftStage CurrentStage { get; private set; } = CraftStage.None;

        private void OnEnable()
        {
            EventBus.OnIngredientAdded += HandleIngredientAdded;
            EventBus.OnDrinkCompleted += HandleDrinkCompleted;
        }

        private void OnDisable()
        {
            EventBus.OnIngredientAdded -= HandleIngredientAdded;
            EventBus.OnDrinkCompleted -= HandleDrinkCompleted;
        }

        public void BeginCraft(RecipeSO targetRecipe)
        {
            _targetRecipe = targetRecipe;
            _pendingResult = DrinkResult.Fail;
            _resultRaised = false;
            CurrentStage = CraftStage.IngredientSelect;

            if (bowl != null)
            {
                bowl.Clear();
            }

            if (tray != null)
            {
                tray.ResetIngredients();
            }

            if (pestle != null)
            {
                pestle.SetInteractable(false);
            }

            if (teaware != null)
            {
                teaware.SetInteractable(false);
            }

            if (mixMinigame != null)
            {
                mixMinigame.Cancel();
            }

            if (pourMinigame != null)
            {
                pourMinigame.Cancel();
            }

            if (dualView != null)
            {
                dualView.SwitchTo(CafeView.Craft);
            }

            if (tray != null)
            {
                tray.Open();
            }

            EventBus.RaiseCraftStarted();
        }

        public void EndCraft()
        {
            CurrentStage = CraftStage.None;

            if (tray != null)
            {
                tray.Close();
            }

            if (pestle != null)
            {
                pestle.SetInteractable(false);
            }

            if (teaware != null)
            {
                teaware.SetInteractable(false);
            }

            if (mixMinigame != null)
            {
                mixMinigame.Cancel();
            }

            if (pourMinigame != null)
            {
                pourMinigame.Cancel();
            }
        }

        public void OnPestleClicked()
        {
            if (CurrentStage != CraftStage.IngredientSelect || bowl == null || bowl.Contents.Count < 1)
            {
                return;
            }

            bowl.Lock();

            if (pestle != null)
            {
                pestle.SetInteractable(false);
            }

            CurrentStage = CraftStage.MixMinigame;

            if (mixMinigame != null)
            {
                mixMinigame.Begin(HandleMixSuccess, HandleMixFail);
                return;
            }

            HandleMixSuccess();
        }

        public void OnTeawareClicked()
        {
            if (CurrentStage != CraftStage.PourPrep)
            {
                return;
            }

            if (teaware != null)
            {
                teaware.SetInteractable(false);
                teaware.PlayPour(HandleTeawarePourDone);
                return;
            }

            HandleTeawarePourDone();
        }

        private void HandleIngredientAdded(IngredientSO ingredient)
        {
            if (CurrentStage != CraftStage.IngredientSelect || pestle == null || bowl == null)
            {
                return;
            }

            pestle.SetInteractable(bowl.Contents.Count > 0);
        }

        private void HandleMixSuccess()
        {
            if (CurrentStage != CraftStage.MixMinigame)
            {
                return;
            }

            CurrentStage = CraftStage.PourPrep;

            if (teaware != null)
            {
                teaware.SetInteractable(true);
            }
        }

        private void HandleMixFail()
        {
            ResolveAndRaise(DrinkResult.Fail);
        }

        private void HandleTeawarePourDone()
        {
            if (CurrentStage != CraftStage.PourPrep)
            {
                return;
            }

            if (dualView != null)
            {
                dualView.SwitchTo(CafeView.Customer);
            }

            CurrentStage = CraftStage.PourMinigame;

            if (pourMinigame != null)
            {
                pourMinigame.Begin(HandlePourSuccess, HandlePourFail);
                return;
            }

            HandlePourSuccess();
        }

        private void HandlePourSuccess()
        {
            if (CurrentStage != CraftStage.PourMinigame)
            {
                return;
            }

            CurrentStage = CraftStage.DrinkComplete;
            _pendingResult = RecipeEvaluator.Evaluate(bowl != null ? bowl.Contents : null, _targetRecipe);

            var drinkName = _targetRecipe != null ? _targetRecipe.drinkName : string.Empty;
            if (drinkPopup != null)
            {
                drinkPopup.Show(null, drinkName, HandleDrinkPopupDone);
                return;
            }

            HandleDrinkPopupDone();
        }

        private void HandleDrinkPopupDone()
        {
            if (serveSequence != null)
            {
                serveSequence.Play(HandleServeDone);
                return;
            }

            HandleServeDone();
        }

        private void HandleServeDone()
        {
            ResolveAndRaise(_pendingResult);
        }

        private void HandlePourFail()
        {
            ResolveAndRaise(DrinkResult.Fail);
        }

        private void ResolveAndRaise(DrinkResult result)
        {
            if (_resultRaised)
            {
                return;
            }

            _resultRaised = true;
            CurrentStage = CraftStage.Done;
            EventBus.RaiseDrinkResult(result);
        }

        private void HandleDrinkCompleted(RecipeSO result)
        {
            if (tray != null)
            {
                tray.Close();
            }
        }
    }
}
