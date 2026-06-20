using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GemCafe.Core
{
    public class SceneRouter : MonoBehaviour
    {
        public const string SceneLobby = "Lobby";
        public const string SceneStage1 = "Stage1_Riverside";
        public const string SceneCafeDialog = "cafe_dialog";
        public const string SceneCafe = "Cafe";
        public const string SceneEnding = "Ending";

        [Tooltip("씬 전환 시 화면이 어두워졌다 밝아지는 페이드 한 방향(아웃/인) 지속 시간(초). 0이면 즉시 전환.")]
        [SerializeField] private float fadeDuration = 0.35f;
        [Tooltip("씬 전환 페이드에 사용할 화면 덮개 색상.")]
        [SerializeField] private Color fadeColor = Color.black;

        private CanvasGroup _fadeGroup;

        public bool IsLoading { get; private set; }

        public void Load(string sceneName, Action onComplete = null)
        {
            if (IsLoading)
            {
                return;
            }

            StartCoroutine(LoadRoutine(sceneName, onComplete));
        }

        private IEnumerator LoadRoutine(string sceneName, Action onComplete)
        {
            IsLoading = true;

            EnsureFader();

            // 현재 씬을 검은 화면으로 덮은 뒤 로드하여 즉시 전환의 어색함을 없앤다.
            yield return Fade(0f, 1f);

            var operation = SceneManager.LoadSceneAsync(sceneName);
            while (!operation.isDone)
            {
                yield return null;
            }

            // 새 씬을 다시 서서히 드러낸다.
            yield return Fade(1f, 0f);

            IsLoading = false;
            onComplete?.Invoke();
        }

        // GameManager(DontDestroyOnLoad)에 부착되는 영속 페이드 오버레이를 1회 생성한다.
        private void EnsureFader()
        {
            if (_fadeGroup != null)
            {
                return;
            }

            var canvasGo = new GameObject("SceneFadeOverlay", typeof(Canvas), typeof(CanvasGroup));
            canvasGo.transform.SetParent(transform, false);

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;

            _fadeGroup = canvasGo.GetComponent<CanvasGroup>();
            _fadeGroup.alpha = 0f;
            _fadeGroup.interactable = false;
            _fadeGroup.blocksRaycasts = false;

            var imageGo = new GameObject("FadeImage", typeof(RectTransform), typeof(Image));
            imageGo.transform.SetParent(canvasGo.transform, false);

            var rect = imageGo.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = imageGo.GetComponent<Image>();
            image.color = fadeColor;
            image.raycastTarget = true;
        }

        private IEnumerator Fade(float from, float to)
        {
            if (_fadeGroup == null)
            {
                yield break;
            }

            // 전환 중에는 입력을 막아 중복 상호작용을 방지한다.
            _fadeGroup.blocksRaycasts = true;
            _fadeGroup.alpha = from;

            if (fadeDuration > 0f)
            {
                var elapsed = 0f;
                while (elapsed < fadeDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    _fadeGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / fadeDuration));
                    yield return null;
                }
            }

            _fadeGroup.alpha = to;
            _fadeGroup.blocksRaycasts = to > 0f;
        }
    }
}