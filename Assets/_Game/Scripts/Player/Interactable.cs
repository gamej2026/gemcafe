using GemCafe.Data;
using UnityEngine;

namespace GemCafe.Player
{
    public class Interactable : MonoBehaviour
    {
        [SerializeField] private string displayName;
        [SerializeField] private string dialogueCsvKey;
        [SerializeField] private DialogueLine[] dialogue;
        [SerializeField] private Renderer outlineTarget;
        [SerializeField] private Material outlineMaterial;
        [SerializeField] private Material normalMaterial;
        [SerializeField] private GameObject highlightVisual;

        private bool _dialogueResolved;
        private DialogueLine[] _resolvedDialogue;

        public string DisplayName => displayName;
        public DialogueLine[] Dialogue
        {
            get
            {
                if (!_dialogueResolved)
                {
                    ResolveDialogue();
                }

                return _resolvedDialogue;
            }
        }

        private void OnValidate()
        {
            _dialogueResolved = false;
        }

        private void ResolveDialogue()
        {
            _dialogueResolved = true;

            string key = !string.IsNullOrWhiteSpace(dialogueCsvKey) ? dialogueCsvKey : displayName;
            var fromCsv = GemCafe.Dialogue.InteractableDialogueCsvLoader.LoadById(key);
            if (fromCsv != null && fromCsv.Length > 0)
            {
                _resolvedDialogue = fromCsv;
                return;
            }

            _resolvedDialogue = dialogue != null ? dialogue : System.Array.Empty<DialogueLine>();
        }

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
