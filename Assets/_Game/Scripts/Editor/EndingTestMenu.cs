using GemCafe.Core;
using GemCafe.Ending;
using UnityEditor;
using UnityEngine;

namespace GemCafe.EditorTools
{
    /// <summary>
    /// 플레이 모드에서 엔딩 연출을 즉시 확인하기 위한 에디터 메뉴.
    /// GemCafe/Test/Ending 하위 항목으로 각 엔딩을 트리거한다.
    /// 플레이 중이 아니면 안내 후 무시한다.
    /// </summary>
    public static class EndingTestMenu
    {
        private const string Root = "GemCafe/Test/Ending/";

        [MenuItem(Root + "진엔딩 A (금코인 3개)", priority = 0)]
        private static void EndingA()
        {
            Trigger(EndingKind.A, 3, 3);
        }

        [MenuItem(Root + "노말 엔딩 B (코인 3개)", priority = 1)]
        private static void EndingB()
        {
            Trigger(EndingKind.B, 3, 0);
        }

        [MenuItem(Root + "배드 엔딩 C (코인 0개)", priority = 2)]
        private static void EndingC()
        {
            Trigger(EndingKind.C, 0, 0);
        }

        [MenuItem(Root + "배드 (뱃삯 미달 · 코인 2개)", priority = 3)]
        private static void EndingBadFare()
        {
            Trigger(EndingKind.C, 2, 0);
        }

        private static void Trigger(EndingKind kind, int totalCoins, int greatCoins)
        {
            if (!EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog(
                    "엔딩 테스트",
                    "엔딩 연출은 플레이 모드에서만 재생할 수 있습니다.\n먼저 플레이 모드로 진입한 뒤 다시 시도하세요.",
                    "확인");
                return;
            }

            EndingFlow.Trigger(kind, totalCoins, greatCoins);
        }
    }
}
