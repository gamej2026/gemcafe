using GemCafe.Core;
using GemCafe.Data;
using UnityEngine;

namespace GemCafe.Crafting
{
    public class CraftingController : MonoBehaviour
    {
        [SerializeField] private TrayController tray;
        [SerializeField] private BowlReceiver bowl;
        [SerializeField] private PestleMixer pestle;

        private void OnEnable()
        {
            EventBus.OnDrinkCompleted += HandleDrinkCompleted;
        }

        private void OnDisable()
        {
            EventBus.OnDrinkCompleted -= HandleDrinkCompleted;
        }

        public void BeginCraft(RecipeSO targetRecipe)
        {
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
                pestle.SetTargetRecipe(targetRecipe);
            }

            if (tray != null)
            {
                tray.Open();
            }

            EventBus.RaiseCraftStarted();
        }

        public void EndCraft()
        {
            if (tray != null)
            {
                tray.Close();
            }
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
