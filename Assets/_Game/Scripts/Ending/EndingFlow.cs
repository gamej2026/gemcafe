using GemCafe.Core;
using UnityEngine;

namespace GemCafe.Ending
{
    /// <summary>
    /// 엔딩 진입 흐름을 한 곳에서 처리한다. 정식 경로(DayManager)와 디버그/테스트
    /// 진입점(에디터 메뉴, F9 오버레이)이 동일한 방식으로 Ending 씬을 로드하도록 한다.
    /// </summary>
    public static class EndingFlow
    {
        /// <summary>
        /// 엔딩 결과를 확정하고 Ending 씬을 로드한다. 상태 전이 규칙을 우회하므로
        /// (테스트용) 어떤 상태에서든 호출할 수 있다.
        /// </summary>
        public static void Trigger(EndingKind kind, int totalCoins, int greatCoins)
        {
            var gm = GameManager.Instance;
            if (gm == null)
            {
                Debug.LogWarning("EndingFlow.Trigger: GameManager 인스턴스가 없습니다. 플레이 모드에서 시도하세요.");
                return;
            }

            gm.SetEndingResult(kind, totalCoins, greatCoins);
            EnterEndingScene();
        }

        /// <summary>
        /// 이미 확정된 엔딩 결과(PendingEnding)로 Ending 상태에 진입하고 씬을 로드한다.
        /// 정식 경로(DayManager.EndDay → EndingCoinSummary 콜백)에서 호출한다.
        /// </summary>
        public static void EnterEndingScene()
        {
            var gm = GameManager.Instance;
            if (gm == null)
            {
                return;
            }

            if (gm.StateMachine.Current != GameState.Ending)
            {
                if (gm.StateMachine.CanTransition(GameState.Ending))
                {
                    gm.StateMachine.TryTransition(GameState.Ending);
                }
                else
                {
                    // 테스트/디버그 진입 등 정식 전이 경로가 아닌 경우 규칙을 우회한다.
                    gm.StateMachine.Restore(GameState.Ending);
                }
            }

            gm.Router?.Load(SceneRouter.SceneEnding);
        }
    }
}
