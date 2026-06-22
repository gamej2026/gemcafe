using UnityEngine;

namespace GemCafe.Core
{
    /// <summary>
    /// 에디터에서 TouchControls를 강제로 활성화해 모바일 입력을 테스트하기 위한 옵션.
    /// 씬의 임의 GameObject에 붙여 Inspector에서 설정한다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TouchControlsEditorOptions : MonoBehaviour
    {
#if UNITY_EDITOR
        [Header("Editor Test")]
        [SerializeField] private bool forceEnableInEditor = true;
    [SerializeField] private bool showMoveButtonsOnStart;

        private void OnEnable()
        {
            Apply();
        }

        private void OnValidate()
        {
            Apply();
        }

        private void OnDisable()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            TouchControls.ClearEditorOverrides();
        }

        private void Apply()
        {
            TouchControls.ConfigureEditorOverrides(forceEnableInEditor, showMoveButtonsOnStart);
        }
#endif
    }
}
