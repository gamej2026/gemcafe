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

            gm.Router.Load(SceneRouter.SceneCafe);
        }
    }
}
