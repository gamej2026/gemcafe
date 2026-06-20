using UnityEngine;

namespace GemCafe.Tutorial
{
    /// <summary>
    /// 카페 튜토리얼이 진행 중인지 알려주는 전역 플래그.
    /// 튜토리얼은 실제 Cafe 씬을 Additive 로 띄워 배경으로만 사용하므로,
    /// 이 플래그가 켜져 있는 동안에는 DayManager 가 실제 서비스(손님 응대)를
    /// 자동 시작하거나 진행 상황을 저장하지 못하도록 막는다.
    /// 즉, 튜토리얼 진행 결과가 실제 게임 데이터에 절대 반영되지 않는다.
    /// </summary>
    public static class TutorialContext
    {
        /// <summary>튜토리얼이 진행 중이면 true. 이 동안 저장/실서비스가 차단된다.</summary>
        public static bool IsActive { get; private set; }

        public static void Begin()
        {
            IsActive = true;
        }

        public static void End()
        {
            IsActive = false;
        }

        // 플레이 세션이 새로 시작될 때(에디터에서 Domain Reload 비활성화 포함) 항상 초기화.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            IsActive = false;
        }
    }
}
