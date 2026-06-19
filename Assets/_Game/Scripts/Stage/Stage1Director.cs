using GemCafe.Core;
using GemCafe.Player;
using GemCafe.Dialogue;
using UnityEngine;

namespace GemCafe.Stage
{
    public class Stage1Director : MonoBehaviour
    {
        [SerializeField] private Interactor interactor;
        [SerializeField] private DialogueRunner dialogueRunner;
        [SerializeField] private Interactable exitInteractable;

        private bool _transitioned;

        private void OnEnable() { if (interactor != null) interactor.OnInteract += HandleInteract; }
        private void OnDisable() { if (interactor != null) interactor.OnInteract -= HandleInteract; }

        private void HandleInteract(Interactable target)
        {
            if (target == null) return;
            if (dialogueRunner == null || dialogueRunner.IsPlaying) return;
            bool isExit = target == exitInteractable;
            dialogueRunner.Play(target.Dialogue, () => { if (isExit) TriggerCafeEntry(); });
        }

        private void TriggerCafeEntry()
        {
            if (_transitioned) return;
            var gm = GameManager.Instance;
            if (gm == null) return;
            _transitioned = true;
            if (gm.StateMachine.Current != GameState.IntroStage1)
                gm.StateMachine.Restore(GameState.IntroStage1);
            gm.StateMachine.TryTransition(GameState.CafeIntro);
            gm.Router.Load(SceneRouter.SceneCafe);
        }
    }
}
