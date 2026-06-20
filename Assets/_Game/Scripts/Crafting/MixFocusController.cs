using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace GemCafe.Crafting
{
    /// <summary>
    /// Pestle 클릭 이후 미니게임 시작 전 단계의 연출을 담당한다.
    /// Bowl에 포커스를 두고 화면을 약간 확대하고, 미니게임 UI를 켜고,
    /// Bowl/미니게임을 제외한 나머지를 어둡게 하며, 시작 버튼을 노출한다.
    /// 미니게임 종료 시 천천히 원래 화면으로 되돌리며 모든 연출을 원상복구한다.
    /// </summary>
    public class MixFocusController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RectTransform zoomRoot;   // WorldViewRoot
        [SerializeField] private RectTransform focusTarget; // Bowl
        [SerializeField] private CanvasGroup mixUiGroup;    // Mix_Root CanvasGroup
        [SerializeField] private CanvasGroup dimOverlay;    // 화면을 어둡게 하는 오버레이
        [SerializeField] private GameObject startButtonRoot; // 시작 버튼 루트
        [SerializeField] private Button startButton;        // 시작 버튼

        [Header("Tuning")]
        [SerializeField] private float zoomScale = 1.2f;
        [SerializeField] private float dimAlpha = 0.55f;
        [SerializeField] private float zoomDuration = 0.45f;
        [SerializeField] private float restoreDuration = 0.6f;
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

            // WorldViewRoot의 평상 스케일은 항상 1이므로 현재 값을 캐처하지 않고 1로 고정(줄 누적/고착 방지).
            _baseScale = Vector3.one;
            _basePos = zoomRoot.anchoredPosition;
            _hasBase = true;
            _active = true;

            ComputeZoomTarget();

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

            if (zoomRoot != null)
            {
                // 줄이 남아 고착되는 것을 막기 위해 스케일은 항상 1로 복원.
                zoomRoot.localScale = Vector3.one;
                // 위치는 포커스가 진행 중일 때만 복원한다. 정상 완료(EndFocus) 이후/EndCraft 시점에는
                // 화면 위치를 건드리지 않는다(위치는 ScreenTransition이 관리). 안 그러면 손님 반응
                // 대사 타이밍에 화면이 craft(Bowl) 위치로 튄다.
                if (wasActive && _hasBase)
                {
                    zoomRoot.anchoredPosition = _basePos;
                }
            }

            if (mixUiGroup != null)
            {
                mixUiGroup.alpha = 0f;
                mixUiGroup.interactable = false;
                mixUiGroup.blocksRaycasts = false;
            }

            if (dimOverlay != null)
            {
                dimOverlay.alpha = 0f;
                dimOverlay.blocksRaycasts = false;
                dimOverlay.gameObject.SetActive(false);
            }

            SetFocusLayer(focusTarget, false);
            SetFocusLayer(mixUiGroup != null ? mixUiGroup.transform as RectTransform : null, false);
        }

        private void ComputeZoomTarget()
        {
            _targetScale = _baseScale * zoomScale;

            if (focusTarget == null)
            {
                _targetPos = _basePos;
                return;
            }

            Vector3 worldCenter = focusTarget.TransformPoint(focusTarget.rect.center);
            Vector2 cp = zoomRoot.InverseTransformPoint(worldCenter);
            _targetPos = _basePos - cp * (zoomScale - 1f);
        }

        private IEnumerator FocusRoutine()
        {
            EnsureDimCanvas();
            SetFocusLayer(focusTarget, true);
            SetFocusLayer(mixUiGroup != null ? mixUiGroup.transform as RectTransform : null, true);

            if (dimOverlay != null)
            {
                dimOverlay.gameObject.SetActive(true);
                dimOverlay.blocksRaycasts = true;
            }

            if (mixUiGroup != null)
            {
                mixUiGroup.gameObject.SetActive(true);
                mixUiGroup.interactable = true;
                mixUiGroup.blocksRaycasts = true;
            }

            float startDim = dimOverlay != null ? dimOverlay.alpha : 0f;
            float startMix = mixUiGroup != null ? mixUiGroup.alpha : 0f;
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
                if (mixUiGroup != null) mixUiGroup.alpha = Mathf.Lerp(startMix, 1f, k);
                yield return null;
            }

            zoomRoot.localScale = _targetScale;
            zoomRoot.anchoredPosition = _targetPos;
            if (dimOverlay != null) dimOverlay.alpha = dimAlpha;
            if (mixUiGroup != null) mixUiGroup.alpha = 1f;

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

            if (mixUiGroup != null)
            {
                mixUiGroup.interactable = false;
                mixUiGroup.blocksRaycasts = false;
            }

            if (dimOverlay != null)
            {
                dimOverlay.blocksRaycasts = false;
            }

            Vector3 startScale = zoomRoot.localScale;
            Vector2 startPos = zoomRoot.anchoredPosition;
            float startDim = dimOverlay != null ? dimOverlay.alpha : 0f;
            float startMix = mixUiGroup != null ? mixUiGroup.alpha : 0f;

            float dur = Mathf.Max(0.01f, restoreDuration);
            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / dur));
                zoomRoot.localScale = Vector3.Lerp(startScale, _baseScale, k);
                zoomRoot.anchoredPosition = Vector2.Lerp(startPos, _basePos, k);
                if (dimOverlay != null) dimOverlay.alpha = Mathf.Lerp(startDim, 0f, k);
                if (mixUiGroup != null) mixUiGroup.alpha = Mathf.Lerp(startMix, 0f, k);
                yield return null;
            }

            zoomRoot.localScale = _baseScale;
            zoomRoot.anchoredPosition = _basePos;
            if (dimOverlay != null)
            {
                dimOverlay.alpha = 0f;
                dimOverlay.gameObject.SetActive(false);
            }
            if (mixUiGroup != null)
            {
                mixUiGroup.alpha = 0f;
            }

            SetFocusLayer(focusTarget, false);
            SetFocusLayer(mixUiGroup != null ? mixUiGroup.transform as RectTransform : null, false);

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
