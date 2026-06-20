#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using System.Text;
using GemCafe.Core;
using GemCafe.Dialogue;
using GemCafe.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GemCafe.DebugTools
{
    /// <summary>
    /// 개발자용 런타임 디버그 오버레이. Stage1_Riverside 씬(횡이동 + 비주얼노벨 인트로)에 맞춰
    /// 게임 상태머신, 플레이어 이동/위치, 상호작용 대상, 대화 진행 상황, 이벤트 로그를 표시한다.
    /// 에디터/개발 빌드에서만 컴파일되며, 별도 씬 세팅 없이 자동으로 생성된다.
    /// 기본적으로 숨겨져 있으며 F1 키로 디버그 패널을, F2 키로 "지금 할 일" 안내를 토글한다.
    /// </summary>
    public class Stage1DebugOverlay : MonoBehaviour
    {
        private const KeyCode ToggleKey = KeyCode.F1;
        private const KeyCode GuideKey = KeyCode.F2;
        private const int MaxLogLines = 40;
        private const float ColumnWidth = 340f;
        private const float ColumnGap = 16f;

        private static Stage1DebugOverlay _instance;

        // 기본적으로 숨김. 씬 시작 후 F1/F2 로 표시.
        private bool _visible;
        private bool _guideVisible;

        // 캐시된 씬 참조 (씬 재로드 시 재획득).
        private PlayerMover _player;
        private Interactor _interactor;
        private DialogueRunner _dialogue;
        private Interactable[] _interactables;

        // 이벤트 누적 통계.
        private bool _dialogueActive;
        private int _dialogueCount;
        private string _lastStateChange = "-";

        private readonly List<string> _log = new List<string>();
        private Vector2 _logScroll;

        // GUI 스타일 (지연 초기화).
        private bool _stylesReady;
        private GUIStyle _panelStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _logStyle;
        private GUIStyle _hintStyle;
        private GUIStyle _guidePanelStyle;
        private GUIStyle _guideTitleStyle;
        private GUIStyle _guideBodyStyle;
        private Texture2D _panelTex;
        private Texture2D _guideTex;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (_instance != null)
            {
                return;
            }

            var go = new GameObject("[Stage1DebugOverlay]");
            _instance = go.AddComponent<Stage1DebugOverlay>();
            DontDestroyOnLoad(go);
        }

        private void OnEnable()
        {
            EventBus.OnStateChanged += HandleStateChanged;
            EventBus.OnDialogueStarted += HandleDialogueStarted;
            EventBus.OnDialogueEnded += HandleDialogueEnded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDisable()
        {
            EventBus.OnStateChanged -= HandleStateChanged;
            EventBus.OnDialogueStarted -= HandleDialogueStarted;
            EventBus.OnDialogueEnded -= HandleDialogueEnded;
            SceneManager.sceneLoaded -= HandleSceneLoaded;
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

            if (Input.GetKeyDown(GuideKey))
            {
                _guideVisible = !_guideVisible;
            }
        }

        // ---- 이벤트 핸들러 ----

        private void HandleStateChanged(GameState from, GameState to)
        {
            _lastStateChange = $"{from} -> {to}";
            AddLog($"State {from} -> {to}");
        }

        private void HandleDialogueStarted()
        {
            _dialogueActive = true;
            _dialogueCount++;
            AddLog("Dialogue started");
        }

        private void HandleDialogueEnded()
        {
            _dialogueActive = false;
            AddLog("Dialogue ended");
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // 새 씬의 참조를 다시 잡도록 캐시 초기화.
            _player = null;
            _interactor = null;
            _dialogue = null;
            _interactables = null;
        }

        private void AddLog(string message)
        {
            _log.Add($"[{Time.timeSinceLevelLoad,6:0.0}] {message}");
            if (_log.Count > MaxLogLines)
            {
                _log.RemoveRange(0, _log.Count - MaxLogLines);
            }
        }

        // ---- GUI ----

        private void OnGUI()
        {
            if (SceneManager.GetActiveScene().name != SceneRouter.SceneStage1)
            {
                return;
            }

            EnsureStyles();
            AcquireReferences();

            // F2 안내 패널은 디버그 패널과 독립적으로 토글된다.
            if (_guideVisible)
            {
                DrawGuidePanel();
            }

            if (!_visible)
            {
                // 기본 숨김 상태: 화면 하단에 키 힌트만 작게 표시.
                GUI.Label(new Rect(10, Screen.height - 24, 560, 20),
                    $"[Debug] {ToggleKey}: 디버그 패널   {GuideKey}: 지금 할 일 안내", _hintStyle);
                return;
            }

            var panelRect = new Rect(8, 8, Screen.width - 16, Screen.height - 16);
            GUILayout.BeginArea(panelRect, _panelStyle);

            GUILayout.Label("STAGE1 (RIVERSIDE) DEBUG OVERLAY", _titleStyle);
            GUILayout.Label($"{ToggleKey}: 디버그 토글   {GuideKey}: 할 일 안내   |  Scene: {SceneManager.GetActiveScene().name}", _labelStyle);
            GUILayout.Space(4);

            GUILayout.BeginHorizontal();

            // 컬럼 1: 상태 + 플레이어 + 대화
            GUILayout.BeginVertical(GUILayout.Width(ColumnWidth));
            DrawStateSection();
            DrawPlayerSection();
            DrawDialogueSection();
            GUILayout.EndVertical();

            GUILayout.Space(ColumnGap);

            // 컬럼 2: 상호작용
            GUILayout.BeginVertical(GUILayout.Width(ColumnWidth));
            DrawInteractionSection();
            GUILayout.EndVertical();

            GUILayout.Space(ColumnGap);

            // 컬럼 3: 이벤트 로그
            GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            DrawLogSection();
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        private void DrawStateSection()
        {
            GUILayout.Space(6);
            GUILayout.Label("■ 상태 머신", _headerStyle);

            var gm = GameManager.Instance;
            if (gm == null)
            {
                GUILayout.Label("GameManager 없음", _labelStyle);
                return;
            }

            GUILayout.Label($"GameState : {gm.StateMachine.Current}", _labelStyle);
            GUILayout.Label($"마지막 전환: {_lastStateChange}", _labelStyle);

            int lives = gm.Lives != null ? gm.Lives.Current : 0;
            int startLives = gm.Config != null ? gm.Config.startingLives : 0;
            GUILayout.Label($"Lives: {lives} / {startLives}", _labelStyle);
        }

        private void DrawPlayerSection()
        {
            GUILayout.Space(6);
            GUILayout.Label("■ 플레이어 / 이동", _headerStyle);

            if (_player == null)
            {
                GUILayout.Label("PlayerMover 없음", _labelStyle);
                return;
            }

            var pos = _player.transform.position;
            float moveSpeed = GameManager.Instance != null && GameManager.Instance.Config != null
                ? GameManager.Instance.Config.moveSpeed
                : 0f;
            float h = Input.GetAxisRaw("Horizontal");
            string dir = h > 0f ? "→ 오른쪽" : (h < 0f ? "← 왼쪽" : "정지");

            GUILayout.Label($"위치: ({pos.x:0.00}, {pos.y:0.00})", _labelStyle);
            GUILayout.Label($"이동 속도: {moveSpeed:0.0}", _labelStyle);
            GUILayout.Label($"입력(Horizontal): {h:0} ({dir})", _labelStyle);
            GUILayout.Label($"이동 잠김: {(_dialogueActive ? "예 (대화 중)" : "아니오")}", _labelStyle);
        }

        private void DrawDialogueSection()
        {
            GUILayout.Space(6);
            GUILayout.Label("■ 대화", _headerStyle);

            bool playing = _dialogueActive || (_dialogue != null && _dialogue.IsPlaying);
            GUILayout.Label($"진행 중: {(playing ? "예" : "아니오")}", _labelStyle);
            GUILayout.Label($"누적 재생 횟수: {_dialogueCount}", _labelStyle);
            GUILayout.Label("진행: 마우스 클릭 또는 Space", _labelStyle);
        }

        private void DrawInteractionSection()
        {
            GUILayout.Space(6);
            GUILayout.Label("■ 상호작용", _headerStyle);

            float radius = GameManager.Instance != null && GameManager.Instance.Config != null
                ? GameManager.Instance.Config.interactRadius
                : 1.5f;
            GUILayout.Label($"상호작용 반경: {radius:0.00}   (F 키로 상호작용)", _labelStyle);

            Vector3? origin = _interactor != null
                ? _interactor.transform.position
                : (_player != null ? _player.transform.position : (Vector3?)null);

            if (origin == null)
            {
                GUILayout.Label("플레이어/Interactor 없음", _labelStyle);
                return;
            }

            if (_interactables == null || _interactables.Length == 0)
            {
                GUILayout.Label("상호작용 대상 없음", _labelStyle);
                return;
            }

            Interactable nearest = null;
            float nearestDist = float.PositiveInfinity;

            for (int i = 0; i < _interactables.Length; i++)
            {
                var it = _interactables[i];
                if (it == null)
                {
                    continue;
                }

                float dist = Vector2.Distance(origin.Value, it.transform.position);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = it;
                }
            }

            if (nearest != null)
            {
                bool inRange = nearestDist <= radius;
                GUILayout.Label($"가장 가까운 대상: {ResolveName(nearest)} ({nearestDist:0.00})", _labelStyle);
                GUILayout.Label($"상호작용 가능: {(inRange ? "[가능] F 키" : "[범위 밖]")}", _labelStyle);
            }

            GUILayout.Space(4);
            GUILayout.Label("— 전체 대상 목록 —", _labelStyle);
            for (int i = 0; i < _interactables.Length; i++)
            {
                var it = _interactables[i];
                if (it == null)
                {
                    continue;
                }

                float dist = Vector2.Distance(origin.Value, it.transform.position);
                bool inRange = dist <= radius;
                int lineCount = it.Dialogue != null ? it.Dialogue.Length : 0;
                GUILayout.Label($"{(inRange ? "●" : "○")} {ResolveName(it)}  d={dist:0.0}  대사={lineCount}", _labelStyle);
            }
        }

        private void DrawLogSection()
        {
            GUILayout.Space(6);
            GUILayout.Label($"■ 이벤트 로그 (최근 {MaxLogLines})", _headerStyle);

            _logScroll = GUILayout.BeginScrollView(_logScroll, GUILayout.ExpandHeight(true));
            for (int i = _log.Count - 1; i >= 0; i--)
            {
                GUILayout.Label(_log[i], _logStyle);
            }

            GUILayout.EndScrollView();
        }

        // ---- F2 안내 패널: 사용자가 지금 무엇을 해야 하는지 표시 ----

        private void DrawGuidePanel()
        {
            BuildGuidance(out var title, out var body);

            const float width = 600f;
            float x = (Screen.width - width) * 0.5f;
            var rect = new Rect(x, 16f, width, 0f);

            float bodyHeight = _guideBodyStyle.CalcHeight(new GUIContent(body), width - 28f);
            rect.height = 52f + bodyHeight;

            GUI.Box(rect, GUIContent.none, _guidePanelStyle);
            GUILayout.BeginArea(new Rect(rect.x + 14f, rect.y + 10f, width - 28f, rect.height - 16f));
            GUILayout.Label(title, _guideTitleStyle);
            GUILayout.Label(body, _guideBodyStyle);
            GUILayout.EndArea();
        }

        private void BuildGuidance(out string title, out string body)
        {
            title = "지금 할 일";

            var gm = GameManager.Instance;
            if (gm == null)
            {
                body = "게임 매니저가 아직 초기화되지 않았습니다.";
                return;
            }

            bool playing = _dialogueActive || (_dialogue != null && _dialogue.IsPlaying);
            if (playing)
            {
                body = "대화가 진행 중입니다. 마우스 클릭 또는 Space 키로 대사를 넘기세요.";
                return;
            }

            float radius = gm.Config != null ? gm.Config.interactRadius : 1.5f;
            Vector3? origin = _interactor != null
                ? _interactor.transform.position
                : (_player != null ? _player.transform.position : (Vector3?)null);

            Interactable nearest = null;
            float nearestDist = float.PositiveInfinity;
            if (origin != null && _interactables != null)
            {
                for (int i = 0; i < _interactables.Length; i++)
                {
                    var it = _interactables[i];
                    if (it == null)
                    {
                        continue;
                    }

                    float dist = Vector2.Distance(origin.Value, it.transform.position);
                    if (dist < nearestDist)
                    {
                        nearestDist = dist;
                        nearest = it;
                    }
                }
            }

            if (nearest == null)
            {
                body = "방향키(←/→)로 강가를 따라 이동하세요. 상호작용할 대상이 보이지 않습니다.";
                return;
            }

            if (nearestDist <= radius)
            {
                body = $"'{ResolveName(nearest)}' 근처입니다. F 키를 눌러 상호작용하세요.\n" +
                       "출구로 향하면 카페 씬으로 이동합니다.";
                return;
            }

            string side = origin.Value.x < nearest.transform.position.x ? "오른쪽(→)" : "왼쪽(←)";
            body = $"방향키로 '{ResolveName(nearest)}' 쪽({side})으로 이동하세요. " +
                   $"현재 거리 {nearestDist:0.0} / 상호작용 반경 {radius:0.0}.";
        }

        // ---- 헬퍼 ----

        private void AcquireReferences()
        {
            if (_player == null)
            {
                _player = FindFirstObjectByType<PlayerMover>();
            }

            if (_interactor == null)
            {
                _interactor = FindFirstObjectByType<Interactor>();
            }

            if (_dialogue == null)
            {
                _dialogue = FindFirstObjectByType<DialogueRunner>();
            }

            if (_interactables == null || _interactables.Length == 0)
            {
                _interactables = FindObjectsByType<Interactable>(FindObjectsSortMode.None);
            }
        }

        private static string ResolveName(Interactable interactable)
        {
            if (interactable == null)
            {
                return "null";
            }

            if (!string.IsNullOrEmpty(interactable.DisplayName))
            {
                return interactable.DisplayName;
            }

            return interactable.name;
        }

        private void EnsureStyles()
        {
            if (_stylesReady)
            {
                return;
            }

            _panelTex = MakeTex(new Color(0f, 0f, 0f, 0.78f));

            _panelStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = _panelTex },
                padding = new RectOffset(10, 10, 10, 10),
                alignment = TextAnchor.UpperLeft
            };

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.6f, 0.85f, 1f) }
            };

            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.6f, 0.9f, 1f) }
            };

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                wordWrap = true,
                normal = { textColor = Color.white }
            };

            _logStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                normal = { textColor = new Color(0.8f, 0.85f, 0.8f) }
            };

            _hintStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 1f, 1f, 0.6f) }
            };

            _guideTex = MakeTex(new Color(0.05f, 0.12f, 0.22f, 0.92f));
            _guidePanelStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = _guideTex },
                border = new RectOffset(2, 2, 2, 2)
            };

            _guideTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 17,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.5f, 0.85f, 1f) }
            };

            _guideBodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                wordWrap = true,
                normal = { textColor = Color.white }
            };

            _stylesReady = true;
        }

        private static Texture2D MakeTex(Color color)
        {
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            tex.hideFlags = HideFlags.HideAndDontSave;
            return tex;
        }
    }
}
#endif
