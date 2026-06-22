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

        private void Start()
        {
            AudioManager.Instance?.PlayStage1Bgm();
        }

        private void OnEnable() { if (interactor != null) interactor.OnInteract += HandleInteract; }
        private void OnDisable() { if (interactor != null) interactor.OnInteract -= HandleInteract; }

        private void HandleInteract(Interactable target)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Stage1Director] HandleInteract target={(target != null ? target.DisplayName : "null")}, runner={(dialogueRunner != null ? (dialogueRunner.IsPlaying ? "playing" : "idle") : "null")}");
#endif
            if (target == null) return;
            if (dialogueRunner == null || dialogueRunner.IsPlaying) return;
            bool isExit = target == exitInteractable;
            bool partnerOnRight = IsTargetOnRight(target);
            dialogueRunner.Play(target.Dialogue, () => { if (isExit) TriggerCafeEntry(); }, partnerOnRight);
        }

        // ????솕 ?긽??? NPC媛? ?뵆?젅?씠?뼱(Interactor) 湲곗?? ?삤瑜몄そ?뿉 ?엳?뒗吏? ?뙋?떒?븳?떎.
        private bool IsTargetOnRight(Interactable target)
        {
            if (interactor == null || target == null) return true;
            return target.transform.position.x >= interactor.transform.position.x;
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
            TouchControls.SetMoveButtonsVisible(false);
            gm.Router.Load(SceneRouter.SceneCafeDialog);
        }
    }
}
