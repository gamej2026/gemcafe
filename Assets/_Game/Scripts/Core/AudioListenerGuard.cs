using UnityEngine;
using UnityEngine.SceneManagement;

namespace GemCafe.Core
{
    // 여러 씬이 각자 카메라(와 경우에 따라 AudioListener)를 들고 있어
    // 씬 전환/Additive 로드 시 "There are no audio listeners" 혹은
    // "multiple audio listeners" 경고가 번갈아 발생한다.
    // 런타임 내내 영속 AudioListener 하나만 켜두고, 씬에 딸려온 다른 리스너는
    // 모두 꺼서 항상 정확히 하나만 존재하도록 보장한다.
    [DefaultExecutionOrder(-1000)]
    public class AudioListenerGuard : MonoBehaviour
    {
        private static AudioListenerGuard _instance;
        private AudioListener _listener;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _instance = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (_instance != null)
            {
                return;
            }

            var go = new GameObject("[AudioListenerGuard]");
            go.AddComponent<AudioListenerGuard>();
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            _listener = GetComponent<AudioListener>();
            if (_listener == null)
            {
                _listener = gameObject.AddComponent<AudioListener>();
            }
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            EnforceSingleListener();
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnforceSingleListener();
        }

        private void OnSceneUnloaded(Scene scene)
        {
            EnforceSingleListener();
        }

        // 영속 리스너만 켜두고, 씬 카메라 등에 딸려온 다른 AudioListener 는 비활성화한다.
        private void EnforceSingleListener()
        {
            if (_listener == null)
            {
                _listener = GetComponent<AudioListener>();
                if (_listener == null)
                {
                    _listener = gameObject.AddComponent<AudioListener>();
                }
            }

            _listener.enabled = true;

            var listeners = FindObjectsByType<AudioListener>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < listeners.Length; i++)
            {
                var other = listeners[i];
                if (other == null || other == _listener)
                {
                    continue;
                }

                other.enabled = false;
            }
        }
    }
}
