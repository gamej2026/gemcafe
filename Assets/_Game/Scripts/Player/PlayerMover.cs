using GemCafe.Core;
using UnityEngine;

namespace GemCafe.Player
{
    public class PlayerMover : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Animator animator;
        [SerializeField] private float fallbackMoveSpeed = 5f;

        [Tooltip("二쇱씤怨듭씠 ?씠?룞?븷 ?닔 ?엳?뒗 X醫뚰몴 ?븳怨?. 짹媛? 踰붿쐞瑜? 踰쀬뼱?굹吏? 紐삵븯寃? 踰쎌쿂?읆 留됰뒗?떎.")]
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
            var x = Mathf.Clamp(Input.GetAxisRaw("Horizontal") + TouchControls.Horizontal, -1f, 1f);

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
