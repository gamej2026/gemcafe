using GemCafe.Core;
using GemCafe.Data;
using GemCafe.Dialogue;
using UnityEngine;

namespace GemCafe.Stage
{
    /// <summary>
    /// 스테이지에서 카페로 진입하기 전 재생되는 중간 씬(cafe_dialog) 연출을 담당한다.
    /// 마님과의 취업 대화를 재생하고, 대화가 끝나면 카페 씬으로 전환한다.
    /// 대화 데이터는 Resources/Stage/interactable_dialogue.csv 의 dialogueCsvKey 그룹을 사용한다.
    /// </summary>
    public class CafeDialogDirector : MonoBehaviour
    {
        [SerializeField] private DialogueRunner dialogueRunner;
        [SerializeField] private string dialogueCsvKey = "카페";
        // 대화 상대(마님)가 화면 오른쪽, 주인공은 왼쪽에 표시된다.
        [SerializeField] private bool partnerOnRight = true;
        // cafe_dialog 가 끝난 뒤 카페 튜토리얼로 진입할지 여부. 기본은 진입(=skip 아님).
        [SerializeField] private bool skipTutorial = false;

        private bool _transitioned;

        private void Start()
        {
            DialogueLine[] lines = InteractableDialogueCsvLoader.LoadById(dialogueCsvKey);

            if (dialogueRunner == null || lines == null || lines.Length == 0)
            {
                EnterCafe();
                return;
            }

            dialogueRunner.Play(lines, EnterCafe, partnerOnRight);
        }

        private void EnterCafe()
        {
            if (_transitioned)
            {
                return;
            }

            _transitioned = true;

            var gm = GameManager.Instance;
            if (gm == null)
            {
                return;
            }

            // 기본적으로 cafe_dialog 가 끝나면 카페 튜토리얼 씬으로 진입한다.
            // 튜토리얼 씬이 실제 Cafe 씬을 Additive 로 띄워 배경으로 쓰고,
            // 끝나면 스스로 깨끗한 Cafe 로 전환한다.
            gm.Router.Load(skipTutorial ? SceneRouter.SceneCafe : SceneRouter.SceneCafeTutorial);
        }
    }
}
