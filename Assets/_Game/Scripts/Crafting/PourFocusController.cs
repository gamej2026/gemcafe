using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace GemCafe.Crafting
{
    /// <summary>
    /// Pour_Teapot 클릭 이후 따르기 미니게임 시작 전 단계의 연출을 담당한다.
    /// Pour_Fill에 포커스를 두고 화면을 약간 확대하고, Pour_Root를 제외한 나머지를
    /// 어둡게 하며, 시작 버튼을 노출한다. 미니게임 종료 시에는 Pour_Fill로 화면을
    /// 조금 더 확대한 뒤 Pour Effect(파티클)를 재생하고, 천천히 원래 화면으로
    /// 되돌리며 미니게임을 페이드아웃하고 디밍을 원상복구한다.
    /// </summary>
    public class PourFocusController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RectTransform zoomRoot;    // WorldViewRoot
        [SerializeField] private RectTransform focusTarget; // Pour_Fill
        [SerializeField] private CanvasGroup pourUiGroup;   // Pour_Root CanvasGroup
        [SerializeField] private CanvasGroup dimOverlay;    // 화면을 어둡게 하는 오버레이
        [SerializeField] private GameObject startButtonRoot; // 시작 버튼 루트
        [SerializeField] private Button startButton;        // 시작 버튼
        [SerializeField] private PourEffect pourEffect;     // 따르기 완료 이펙트(파티클 미할당 시 재생 스킵)

        [Header("Tuning")]
        [SerializeField] private float zoomScale = 1.2f;
        [SerializeField] private float finishZoomScale = 1.45f;
        [SerializeField] private float dimAlpha = 0.55f;
        [SerializeField] private float zoomDuration = 0.45f;
        [SerializeField] private float finishZoomDuration = 0.35f;
        [SerializeField] private float effectHoldDuration = 0.6f;
        [SerializeField] private float restoreDuration = 0.7f;
        [SerializeField] private int focusSortingOrder = 60;
        [SerializeField] private int dimSortingOrder = 50;

        private Action _onStartPressed;
        private bool _started;

        private Vector3 _baseScale = Vector3.one;
        private Vector2 _basePos;
        private bool _hasBase;

        private Vector3 _targetScale = Vector3.one;
        private Vector2 _targetPos;

        private Coroutine _routine;
        private bool _active;

        public void BeginFocus(Action onStartPressed)
        {
            if (zoomRoot == null)
            {
                // 연출 불가 시 즉시 시작 콜백 실행
                onStartPressed?.Invoke();
                return;
            }

            _onStartPressed = onStartPressed;
            _started = false;

            // WorldViewRoot의 평상 스케일은 항상 1이므로 현재 값을 캐처하지 않고 1로 고정.
            _baseScale = Vector3.one;
            _basePos = zoomRoot.anchoredPosition;
            _hasBase = true;
            _active = true;

            _targetScale = _baseScale * zoomScale;
            _targetPos = ComputeZoomPos(zoomScale);

            if (startButton != null)
            {
                startButton.onClick.RemoveListener(HandleStartClicked);
                startButton.onClick.AddListener(HandleStartClicked);
            }

            if (startButtonRoot != null)
            {
                startButtonRoot.SetActive(false);
            }

            if (_routine != null)
            {
                StopCoroutine(_routine);
            }

            _routine = StartCoroutine(FocusRoutine());
        }

        public void EndFocus(Action onComplete)
        {
            if (zoomRoot == null || !_hasBase)
            {
                onComplete?.Invoke();
                return;
            }

            if (_routine != null)
            {
                StopCoroutine(_routine);
            }

            _routine = StartCoroutine(RestoreRoutine(onComplete));
        }

        public void CancelImmediate()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }

            bool wasActive = _active;
            _active = false;
            _started = false;
            _onStartPressed = null;

            if (startButton != null)
            {
                startButton.onClick.RemoveListener(HandleStartClicked);
            }

            if (startButtonRoot != null)
            {
                startButtonRoot.SetActive(false);
            }

            if (pourEffect != null)
            {
                pourEffect.StopAndClear();
            }

            if (zoomRoot != null)
            {
                zoomRoot.localScale = Vector3.one;
                // 위치는 포커스가 진행 중일 때만 복원한다(위치는 ScreenTransition이 관리).
                if (wasActive && _hasBase)
                {
                    zoomRoot.anchoredPosition = _basePos;
                }
            }

            if (pourUiGroup != null)
            {
                pourUiGroup.alpha = 0f;
                pourUiGroup.interactable = false;
                pourUiGroup.blocksRaycasts = false;
            }

            if (dimOverlay != null)
            {
                dimOverlay.alpha = 0f;
                dimOverlay.blocksRaycasts = false;
                dimOverlay.gameObject.SetActive(false);
            }

            SetFocusLayer(pourUiGroup != null ? pourUiGroup.transform as RectTransform : null, false);
        }

        private Vector2 ComputeZoomPos(float scale)
        {
            if (focusTarget == null)
            {
                return _basePos;
            }

            Vector3 worldCenter = focusTarget.TransformPoint(focusTarget.rect.center);
            Vector2 cp = zoomRoot.InverseTransformPoint(worldCenter);
            return _basePos - cp * (scale - 1f);
        }

        private IEnumerator FocusRoutine()
        {
            EnsureDimCanvas();
            SetFocusLayer(pourUiGroup != null ? pourUiGroup.transform as RectTransform : null, true);

            if (dimOverlay != null)
            {
                dimOverlay.gameObject.SetActive(true);
                dimOverlay.blocksRaycasts = true;
            }

            if (pourUiGroup != null)
            {
                pourUiGroup.gameObject.SetActive(true);
                pourUiGroup.interactable = true;
                pourUiGroup.blocksRaycasts = true;
            }

            float startDim = dimOverlay != null ? dimOverlay.alpha : 0f;
            float startUi = pourUiGroup != null ? pourUiGroup.alpha : 0f;
            Vector3 startScale = zoomRoot.localScale;
            Vector2 startPos = zoomRoot.anchoredPosition;

            float dur = Mathf.Max(0.01f, zoomDuration);
            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / dur));
                zoomRoot.localScale = Vector3.Lerp(startScale, _targetScale, k);
                zoomRoot.anchoredPosition = Vector2.Lerp(startPos, _targetPos, k);
                if (dimOverlay != null) dimOverlay.alpha = Mathf.Lerp(startDim, dimAlpha, k);
                if (pourUiGroup != null) pourUiGroup.alpha = Mathf.Lerp(startUi, 1f, k);
                yield return null;
            }

            zoomRoot.localScale = _targetScale;
            zoomRoot.anchoredPosition = _targetPos;
            if (dimOverlay != null) dimOverlay.alpha = dimAlpha;
            if (pourUiGroup != null) pourUiGroup.alpha = 1f;

            if (startButtonRoot != null)
            {
                startButtonRoot.SetActive(true);
            }

            _routine = null;
        }

        private IEnumerator RestoreRoutine(Action onComplete)
        {
            if (startButtonRoot != null)
            {
                startButtonRoot.SetActive(false);
            }

            if (pourUiGroup != null)
            {
                pourUiGroup.interactable = false;
                pourUiGroup.blocksRaycasts = false;
            }

            if (dimOverlay != null)
            {
                dimOverlay.blocksRaycasts = false;
            }

            // 1) Pour_Fill로 화면을 조금 더 확대
            Vector3 finishScale = _baseScale * finishZoomScale;
            Vector2 finishPos = ComputeZoomPos(finishZoomScale);

            Vector3 startScale = zoomRoot.localScale;
            Vector2 startPos = zoomRoot.anchoredPosition;

            float zoomDur = Mathf.Max(0.01f, finishZoomDuration);
            float zt = 0f;
            while (zt < zoomDur)
            {
                zt += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(zt / zoomDur));
                zoomRoot.localScale = Vector3.Lerp(startScale, finishScale, k);
                zoomRoot.anchoredPosition = Vector2.Lerp(startPos, finishPos, k);
                yield return null;
            }
            zoomRoot.localScale = finishScale;
            zoomRoot.anchoredPosition = finishPos;

            // 2) Pour Effect 재생 (파티클 시스템이 비어있으면 재생 스킵)
            if (pourEffect != null)
            {
                pourEffect.Play();
            }

            float hold = Mathf.Max(0f, effectHoldDuration);
            float ht = 0f;
            while (ht < hold)
            {
                ht += Time.deltaTime;
                yield return null;
            }

            // 3) 원래 화면으로 천천히 이동하면서 미니게임 페이드아웃 + 디밍 원상복구
            Vector3 restoreStartScale = zoomRoot.localScale;
            Vector2 restoreStartPos = zoomRoot.anchoredPosition;
            float startDim = dimOverlay != null ? dimOverlay.alpha : 0f;
            float startUi = pourUiGroup != null ? pourUiGroup.alpha : 0f;

            float dur = Mathf.Max(0.01f, restoreDuration);
            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / dur));
                zoomRoot.localScale = Vector3.Lerp(restoreStartScale, _baseScale, k);
                zoomRoot.anchoredPosition = Vector2.Lerp(restoreStartPos, _basePos, k);
                if (dimOverlay != null) dimOverlay.alpha = Mathf.Lerp(startDim, 0f, k);
                if (pourUiGroup != null) pourUiGroup.alpha = Mathf.Lerp(startUi, 0f, k);
                yield return null;
            }

            zoomRoot.localScale = _baseScale;
            zoomRoot.anchoredPosition = _basePos;
            if (dimOverlay != null)
            {
                dimOverlay.alpha = 0f;
                dimOverlay.gameObject.SetActive(false);
            }
            if (pourUiGroup != null)
            {
                pourUiGroup.alpha = 0f;
            }

            SetFocusLayer(pourUiGroup != null ? pourUiGroup.transform as RectTransform : null, false);

            _active = false;
            _routine = null;
            onComplete?.Invoke();
        }

        private void HandleStartClicked()
        {
            if (_started)
            {
                return;
            }

            _started = true;

            if (startButtonRoot != null)
            {
                startButtonRoot.SetActive(false);
            }

            var cb = _onStartPressed;
            _onStartPressed = null;
            cb?.Invoke();
        }

        private void SetFocusLayer(RectTransform rt, bool on)
        {
            if (rt == null)
            {
                return;
            }

            var canvas = rt.GetComponent<Canvas>();
            if (on)
            {
                if (canvas == null)
                {
                    canvas = rt.gameObject.AddComponent<Canvas>();
                }

                canvas.overrideSorting = true;
                canvas.sortingOrder = focusSortingOrder;

                if (rt.GetComponent<GraphicRaycaster>() == null)
                {
                    rt.gameObject.AddComponent<GraphicRaycaster>();
                }
            }
            else if (canvas != null)
            {
                canvas.overrideSorting = false;
            }
        }

        private void EnsureDimCanvas()
        {
            if (dimOverlay == null)
            {
                return;
            }

            var canvas = dimOverlay.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = dimOverlay.gameObject.AddComponent<Canvas>();
            }

            canvas.overrideSorting = true;
            canvas.sortingOrder = dimSortingOrder;

            if (dimOverlay.GetComponent<GraphicRaycaster>() == null)
            {
                dimOverlay.gameObject.AddComponent<GraphicRaycaster>();
            }
        }
    }
}
