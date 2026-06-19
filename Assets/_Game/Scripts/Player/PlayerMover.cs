using GemCafe.Core;
using UnityEngine;

namespace GemCafe.Player
{
    public class PlayerMover : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private float fallbackMoveSpeed = 5f;

        private bool _inputLocked;

        private void OnEnable()
        {
            EventBus.OnDialogueStarted += HandleDialogueStarted;
            EventBus.OnDialogueEnded += HandleDialogueEnded;
        }

        private void OnDisable()
        {
            EventBus.OnDialogueStarted -= HandleDialogueStarted;
            EventBus.OnDialogueEnded -= HandleDialogueEnded;
        }

        private void Update()
        {
            if (_inputLocked)
            {
                return;
            }

            var speed = GameManager.Instance != null ? GameManager.Instance.Config.moveSpeed : fallbackMoveSpeed;
            var x = Input.GetAxisRaw("Horizontal");

            transform.Translate(x * speed * Time.deltaTime, 0f, 0f);

            if (spriteRenderer != null)
            {
                if (x > 0f)
                {
                    spriteRenderer.flipX = false;
                }
                else if (x < 0f)
                {
                    spriteRenderer.flipX = true;
                }
            }
        }

        public void SetInputLocked(bool locked)
        {
            _inputLocked = locked;
        }

        private void HandleDialogueStarted()
        {
            _inputLocked = true;
        }

        private void HandleDialogueEnded()
        {
            _inputLocked = false;
        }
    }
}
