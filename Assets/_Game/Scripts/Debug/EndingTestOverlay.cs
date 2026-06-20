#if UNITY_EDITOR || DEVELOPMENT_BUILD
using GemCafe.Core;
using GemCafe.Ending;
using UnityEngine;

namespace GemCafe.DebugTools
{
    /// <summary>
    /// 엔딩 연출을 빠르게 확인하기 위한 개발자용 테스트 오버레이.
    /// 에디터/개발 빌드에서만 컴파일되며 별도 씬 세팅 없이 자동 생성된다.
    /// 단축키: F9 토글, 숫자키 1~3(또는 화면 버튼)로 각 엔딩을 즉시 재생한다.
    ///   1: 진엔딩 A (금코인 3개)
    ///   2: 노말 엔딩 B (코인 1 ~ 3개)
    ///   3: 배드 엔딩 C (코인 0개)
    /// </summary>
    public class EndingTestOverlay : MonoBehaviour
    {
        private const KeyCode ToggleKey = KeyCode.F9;

        private static EndingTestOverlay _instance;

        private bool _visible;

        private bool _stylesReady;
        private GUIStyle _panelStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _buttonStyle;
        private Texture2D _panelTex;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (_instance != null)
            {
                return;
            }

            var go = new GameObject("[EndingTestOverlay]");
            _instance = go.AddComponent<EndingTestOverlay>();
            DontDestroyOnLoad(go);
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(ToggleKey))
            {
                _visible = !_visible;
            }

            if (!_visible)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
            {
                Trigger(EndingKind.A, 3, 3);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
            {
                Trigger(EndingKind.B, 2, 0);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
            {
                Trigger(EndingKind.C, 0, 0);
            }
        }

        private static void Trigger(EndingKind kind, int totalCoins, int greatCoins)
        {
            if (_instance != null)
            {
                _instance._visible = false;
            }

            EndingFlow.Trigger(kind, totalCoins, greatCoins);
        }

        private void OnGUI()
        {
            if (!_visible)
            {
                return;
            }

            EnsureStyles();

            const float width = 260f;
            const float x = 10f;
            const float y = 10f;
            var rect = new Rect(x, y, width, 232f);
            GUI.Box(rect, GUIContent.none, _panelStyle);

            GUILayout.BeginArea(new Rect(x + 10f, y + 8f, width - 20f, 232f - 16f));
            GUILayout.Label("엔딩 테스트 (F9 닫기)", _titleStyle);
            GUILayout.Space(4f);

            if (GUILayout.Button("1. 진엔딩 A (금코인 3)", _buttonStyle, GUILayout.Height(34f)))
            {
                Trigger(EndingKind.A, 3, 3);
            }

            if (GUILayout.Button("2. 노말 엔딩 B (코인 1~3)", _buttonStyle, GUILayout.Height(34f)))
            {
                Trigger(EndingKind.B, 2, 0);
            }

            if (GUILayout.Button("3. 배드 엔딩 C (코인 0)", _buttonStyle, GUILayout.Height(34f)))
            {
                Trigger(EndingKind.C, 0, 0);
            }

            GUILayout.EndArea();
        }

        private void EnsureStyles()
        {
            if (_stylesReady)
            {
                return;
            }

            _stylesReady = true;

            _panelTex = new Texture2D(1, 1);
            _panelTex.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.78f));
            _panelTex.Apply();

            _panelStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = _panelTex }
            };

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.85f, 0.4f) }
            };

            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleLeft
            };
        }
    }
}
#endif
