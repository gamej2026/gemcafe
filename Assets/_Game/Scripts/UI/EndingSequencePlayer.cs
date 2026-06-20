using GemCafe.Core;
using UnityEngine;

namespace GemCafe.UI
{
    public class EndingSequencePlayer : MonoBehaviour
    {
        [SerializeField] private GameObject endingARoot;
        [SerializeField] private GameObject endingBRoot;
        [SerializeField] private GameObject endingCRoot;

        private void OnEnable()
        {
            EventBus.OnStateChanged += HandleState;
        }

        private void OnDisable()
        {
            EventBus.OnStateChanged -= HandleState;
        }

        private void HandleState(GameState from, GameState to)
        {
            if (to != GameState.Ending)
            {
                return;
            }

            var kind = GameManager.Instance != null ? GameManager.Instance.PendingEnding : EndingKind.B;
            Play(kind);
        }

        public void Play(EndingKind kind)
        {
            if (endingARoot != null)
            {
                endingARoot.SetActive(kind == EndingKind.A);
            }

            if (endingBRoot != null)
            {
                endingBRoot.SetActive(kind == EndingKind.B);
            }

            if (endingCRoot != null)
            {
                endingCRoot.SetActive(kind == EndingKind.C);
            }
        }
    }
}
