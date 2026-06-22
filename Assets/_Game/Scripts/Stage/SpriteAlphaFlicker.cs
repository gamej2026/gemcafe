using UnityEngine;

namespace GemCafe.Stage
{
    /// <summary>
    /// <see cref="SpriteRenderer"/> 의 알파(투명도)를 시간에 따라 흔들어 촛불·등불 같은
    /// 깜빡임(flicker) 효과를 만든다.
    /// <para>
    /// - <see cref="randomizeStartAlpha"/> 로 시작 알파를 무작위로 잡아, 같은 프리팹을 여러 개
    ///   배치해도 동시에 같은 위상으로 깜빡이지 않게 한다.<br/>
    /// - <see cref="useRandomDelay"/> 로 깜빡임 사이에 무작위 멈춤(딜레이)을 넣어 불규칙한
    ///   촛불 느낌을 강화한다.<br/>
    /// - <see cref="minAlpha"/> ~ <see cref="maxAlpha"/> 범위 안에서 목표 알파를 잡고,
    ///   <see cref="fadeSpeed"/> 로 부드럽게 보간한다.
    /// </para>
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteAlphaFlicker : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("대상 SpriteRenderer. 비워두면 같은 오브젝트의 것을 사용한다.")]
        [SerializeField] private SpriteRenderer targetRenderer;

        [Header("Alpha Range")]
        [Tooltip("깜빡일 때 도달할 수 있는 최소 알파(0 = 완전 투명).")]
        [Range(0f, 1f)]
        [SerializeField] private float minAlpha = 0.4f;

        [Tooltip("깜빡일 때 도달할 수 있는 최대 알파(1 = 완전 불투명).")]
        [Range(0f, 1f)]
        [SerializeField] private float maxAlpha = 1f;

        [Header("Start")]
        [Tooltip("시작 알파를 minAlpha~maxAlpha 사이에서 무작위로 정한다.")]
        [SerializeField] private bool randomizeStartAlpha = true;

        [Tooltip("randomizeStartAlpha 가 꺼져 있을 때 사용할 시작 알파.")]
        [Range(0f, 1f)]
        [SerializeField] private float startAlpha = 1f;

        [Header("Flicker")]
        [Tooltip("현재 알파에서 목표 알파로 수렴하는 속도(클수록 빠르고 거칠다).")]
        [Min(0f)]
        [SerializeField] private float fadeSpeed = 6f;

        [Tooltip("목표 알파에 도달했다고 판단하는 허용 오차.")]
        [Min(0.0001f)]
        [SerializeField] private float reachThreshold = 0.02f;

        [Header("Random Delay")]
        [Tooltip("새 목표 알파로 넘어가기 전에 무작위로 잠시 멈춘다(불규칙한 촛불 느낌).")]
        [SerializeField] private bool useRandomDelay = true;

        [Tooltip("목표 도달 후 다음 깜빡임까지의 최소 대기 시간(초).")]
        [Min(0f)]
        [SerializeField] private float minDelay = 0.02f;

        [Tooltip("목표 도달 후 다음 깜빡임까지의 최대 대기 시간(초).")]
        [Min(0f)]
        [SerializeField] private float maxDelay = 0.2f;

        private float _currentAlpha;
        private float _targetAlpha;
        private float _delayTimer;

        private void Reset()
        {
            targetRenderer = GetComponent<SpriteRenderer>();
        }

        private void Awake()
        {
            if (targetRenderer == null)
            {
                targetRenderer = GetComponent<SpriteRenderer>();
            }
        }

        private void OnEnable()
        {
            float lo = Mathf.Min(minAlpha, maxAlpha);
            float hi = Mathf.Max(minAlpha, maxAlpha);

            _currentAlpha = randomizeStartAlpha
                ? Random.Range(lo, hi)
                : Mathf.Clamp01(startAlpha);

            ApplyAlpha(_currentAlpha);
            PickNewTarget();
            _delayTimer = 0f;
        }

        private void Update()
        {
            if (targetRenderer == null)
            {
                return;
            }

            // 딜레이 중이면 알파를 유지한 채 대기한다.
            if (_delayTimer > 0f)
            {
                _delayTimer -= Time.deltaTime;
                return;
            }

            _currentAlpha = Mathf.MoveTowards(
                _currentAlpha,
                _targetAlpha,
                fadeSpeed * Time.deltaTime);

            ApplyAlpha(_currentAlpha);

            // 목표에 도달하면 (옵션에 따라) 잠시 멈춘 뒤 새 목표를 고른다.
            if (Mathf.Abs(_currentAlpha - _targetAlpha) <= reachThreshold)
            {
                if (useRandomDelay)
                {
                    float lo = Mathf.Min(minDelay, maxDelay);
                    float hi = Mathf.Max(minDelay, maxDelay);
                    _delayTimer = Random.Range(lo, hi);
                }

                PickNewTarget();
            }
        }

        private void PickNewTarget()
        {
            float lo = Mathf.Min(minAlpha, maxAlpha);
            float hi = Mathf.Max(minAlpha, maxAlpha);
            _targetAlpha = Random.Range(lo, hi);
        }

        private void ApplyAlpha(float alpha)
        {
            var color = targetRenderer.color;
            color.a = Mathf.Clamp01(alpha);
            targetRenderer.color = color;
        }
    }
}
