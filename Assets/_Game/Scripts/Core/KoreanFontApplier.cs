using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GemCafe.Core
{
    /// <summary>
    /// WebGL 빌드에는 OS 폰트 폴백이 없어 Unity 내장(Arial/LegacyRuntime) 폰트로는
    /// 한글 글리프가 렌더링되지 않는다. 이 클래스는 프로젝트에 임베드된 한글 폰트
    /// (Resources/Fonts/NanumGothic)를 모든 UI <see cref="Text"/>에 런타임으로 적용해
    /// 씬에 미리 배치된 Text와 코드로 동적 생성되는 Text 모두에서 한글이 보이도록 한다.
    /// </summary>
    public static class KoreanFontApplier
    {
        private const string FontResourcePath = "Fonts/NanumGothic";

        private static Font _font;
        private static bool _loaded;

        /// <summary>임베드된 한글 폰트. 로드에 실패하면 null.</summary>
        public static Font Font
        {
            get
            {
                EnsureLoaded();
                return _font;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            EnsureLoaded();
            if (_font == null)
            {
                return;
            }

            var go = new GameObject("[KoreanFontApplier]");
            go.hideFlags = HideFlags.HideAndDontSave;
            Object.DontDestroyOnLoad(go);
            go.AddComponent<Driver>();
        }

        private static void EnsureLoaded()
        {
            if (_loaded)
            {
                return;
            }

            _loaded = true;
            _font = Resources.Load<Font>(FontResourcePath);
            if (_font == null)
            {
                Debug.LogWarning($"KoreanFontApplier: '{FontResourcePath}' 폰트를 찾을 수 없습니다. 한글이 표시되지 않을 수 있습니다.");
            }
        }

        /// <summary>현재 로드된 모든 UI Text에 한글 폰트를 적용한다.</summary>
        public static void ApplyToAll()
        {
            EnsureLoaded();
            if (_font == null)
            {
                return;
            }

            var texts = Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < texts.Length; i++)
            {
                Apply(texts[i]);
            }
        }

        /// <summary>단일 Text에 한글 폰트를 적용한다.</summary>
        public static void Apply(Text text)
        {
            EnsureLoaded();
            if (text == null || _font == null || text.font == _font)
            {
                return;
            }

            text.font = _font;
        }

        /// <summary>씬 로드마다 Text 폰트를 교체하는 영속 드라이버.</summary>
        private sealed class Driver : MonoBehaviour
        {
            private void OnEnable()
            {
                SceneManager.sceneLoaded += OnSceneLoaded;
            }

            private void OnDisable()
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
            }

            private void Start()
            {
                StartCoroutine(ApplyRoutine());
            }

            private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
            {
                StartCoroutine(ApplyRoutine());
            }

            private IEnumerator ApplyRoutine()
            {
                // 씬에 배치된 Text 즉시 적용.
                ApplyToAll();
                // Start/OnEnable에서 동적 생성되는 Text까지 잡기 위해 다음 프레임에 재적용.
                yield return null;
                ApplyToAll();
                yield return new WaitForEndOfFrame();
                ApplyToAll();
            }
        }
    }
}
