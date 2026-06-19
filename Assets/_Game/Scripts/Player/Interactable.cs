using GemCafe.Data;
using UnityEngine;

namespace GemCafe.Player
{
    public class Interactable : MonoBehaviour
    {
        [SerializeField] private string displayName;
        [SerializeField] private DialogueLine[] dialogue;
        [SerializeField] private Renderer outlineTarget;
        [SerializeField] private Material outlineMaterial;
        [SerializeField] private Material normalMaterial;
        [SerializeField] private GameObject highlightVisual;

        public string DisplayName => displayName;
        public DialogueLine[] Dialogue => dialogue;

        public void SetHighlight(bool on)
        {
            if (highlightVisual != null)
            {
                highlightVisual.SetActive(on);
            }

            if (outlineTarget != null && outlineMaterial != null && normalMaterial != null)
            {
                outlineTarget.material = on ? outlineMaterial : normalMaterial;
            }
        }
    }
}
