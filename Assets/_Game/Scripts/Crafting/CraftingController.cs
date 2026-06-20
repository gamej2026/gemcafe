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
        MixPrep,
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
        [SerializeField] private MixFocusController mixFocus;
        [SerializeField] private PourMinigame pourMinigame;
        [SerializeField] private PourFocusController pourFocus;
        [SerializeField] private TeawarePour teaware;
        [SerializeField] private DrinkPopup drinkPopup;
        [SerializeField] private ServeSequence serveSequence;
        [SerializeField] private ScreenTransition dualView;

        private RecipeSO _targetRecipe;
        private DrinkResult _pendingResult;
        private bool _resultRaised;
        private bool _mixSucceeded;
        private bool _pourSucceeded;

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
            _mixSucceeded = false;
            _pourSucceeded = false;
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

            if (mixFocus != null)
            {
                mixFocus.CancelImmediate();
            }

            if (pourMinigame != null)
            {
                pourMinigame.Cancel();
            }

            if (pourFocus != null)
            {
                pourFocus.CancelImmediate();
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

            if (mixFocus != null)
            {
                mixFocus.CancelImmediate();
            }

            if (pourMinigame != null)
            {
                pourMinigame.Cancel();
            }

            if (pourFocus != null)
            {
                pourFocus.CancelImmediate();
            }
        }

        public void OnPestleClicked()
        {
            if (CurrentStage != CraftStage.IngredientSelect || bowl == null || bowl.Contents.Count < 2)
            {
                return;
            }

            bowl.Lock();

            if (pestle != null)
            {
                pestle.SetInteractable(false);
            }

            if (mixMinigame != null)
            {
                mixMinigame.PrepareVisuals();
            }

            CurrentStage = CraftStage.MixPrep;

            if (mixFocus != null)
            {
                mixFocus.BeginFocus(BeginMixMinigame);
                return;
            }

            BeginMixMinigame();
        }

        private void BeginMixMinigame()
        {
            if (CurrentStage != CraftStage.MixPrep)
            {
                return;
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
            }

            if (pourFocus != null)
            {
                pourFocus.BeginFocus(BeginPourMinigame);
                return;
            }

            BeginPourMinigame();
        }

        private void BeginPourMinigame()
        {
            if (CurrentStage != CraftStage.PourPrep)
            {
                return;
            }

            CurrentStage = CraftStage.PourMinigame;

            if (pourMinigame != null)
            {
                pourMinigame.Begin(HandlePourSuccess, HandlePourFail);
                return;
            }

            HandlePourSuccess();
        }

        private void HandleIngredientAdded(IngredientSO ingredient)
        {
            if (CurrentStage != CraftStage.IngredientSelect || pestle == null || bowl == null)
            {
                return;
            }

            pestle.SetInteractable(bowl.Contents.Count >= 2);
        }

        private void HandleMixSuccess()
        {
            if (CurrentStage != CraftStage.MixMinigame)
            {
                return;
            }

            _mixSucceeded = true;
            FinishMixFocus();
        }

        private void HandleMixFail()
        {
            if (CurrentStage != CraftStage.MixMinigame)
            {
                return;
            }

            _mixSucceeded = false;
            FinishMixFocus();
        }

        private void FinishMixFocus()
        {
            if (mixFocus != null)
            {
                mixFocus.EndFocus(EnterPourPrep);
                return;
            }

            EnterPourPrep();
        }

        private void EnterPourPrep()
        {
            CurrentStage = CraftStage.PourPrep;

            if (dualView != null)
            {
                dualView.SwitchTo(CafeView.Customer);
            }

            // 화면이 이동함과 동시에 Tray를 Close 위치로 이동 + 나머지 오브젝트 페이드 아웃
            if (tray != null)
            {
                tray.Close();
            }

            // 컵(Pour_Fill)과 주전자(Pour_Teapot)를 보이게 하고 주전자 클릭을 열어둔다.
            if (pourMinigame != null)
            {
                pourMinigame.PrepareVisuals();
            }

            if (teaware != null)
            {
                teaware.SetInteractable(true);
            }
        }

        private void HandlePourSuccess()
        {
            if (CurrentStage != CraftStage.PourMinigame)
            {
                return;
            }

            _pourSucceeded = true;
            FinishPourFocus();
        }

        private void HandlePourFail()
        {
            if (CurrentStage != CraftStage.PourMinigame)
            {
                return;
            }

            _pourSucceeded = false;
            FinishPourFocus();
        }

        private void FinishPourFocus()
        {
            if (pourFocus != null)
            {
                pourFocus.EndFocus(FinishCraft);
                return;
            }

            FinishCraft();
        }

        private void FinishCraft()
        {
            CurrentStage = CraftStage.DrinkComplete;

            int minigameSuccessCount = (_mixSucceeded ? 1 : 0) + (_pourSucceeded ? 1 : 0);
            _pendingResult = RecipeEvaluator.Evaluate(
                bowl != null ? bowl.Contents : null,
                _targetRecipe,
                minigameSuccessCount);

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
