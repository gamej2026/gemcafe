using GemCafe.Core;
using UnityEngine;

namespace GemCafe.Player
{
    public class PlayerMover : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Animator animator;
        [SerializeField] private float fallbackMoveSpeed = 5f;

        [Tooltip("주인공이 이동할 수 있는 X좌표 한계. ±값 범위를 벗어나지 못하게 벽처럼 막는다.")]
        [SerializeField] private float horizontalLimit = 25f;

        private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");

        private bool _inputLocked;

        private void Awake()
        {
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }
        }

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
                UpdateWalkAnimation(0f);
                return;
            }

            var speed = GameManager.Instance != null ? GameManager.Instance.Config.moveSpeed : fallbackMoveSpeed;
            var x = Input.GetAxisRaw("Horizontal");

            transform.Translate(x * speed * Time.deltaTime, 0f, 0f);

            if (horizontalLimit > 0f)
            {
                var pos = transform.position;
                pos.x = Mathf.Clamp(pos.x, -horizontalLimit, horizontalLimit);
                transform.position = pos;
            }

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

            UpdateWalkAnimation(x);
        }

        private void UpdateWalkAnimation(float horizontal)
        {
            if (animator != null)
            {
                animator.SetBool(IsWalkingHash, Mathf.Abs(horizontal) > 0.01f);
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
