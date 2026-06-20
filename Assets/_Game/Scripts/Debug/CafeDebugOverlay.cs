#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using System.Text;
using GemCafe.Core;
using GemCafe.Crafting;
using GemCafe.Customer;
using GemCafe.Data;
using GemCafe.Dialogue;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GemCafe.DebugTools
{
    /// <summary>
    /// 개발자용 런타임 디버그 오버레이. Cafe 씬에서 게임 진행 상황(상태머신, 일자/요금/생명,
    /// 현재 손님, 목표 레시피, 인내심 타이머, 보울 내용물, 이벤트 로그)을 자세히 표시한다.
    /// 에디터/개발 빌드에서만 컴파일되며, 별도 씬 세팅 없이 자동으로 생성된다.
    /// 기본적으로 숨겨져 있으며 F1 키로 디버그 패널을, F2 키로 "지금 할 일" 안내를 토글한다.
    /// </summary>
    public class CafeDebugOverlay : MonoBehaviour
    {
        private const KeyCode ToggleKey = KeyCode.F1;
        private const KeyCode GuideKey = KeyCode.F2;
        private const int MaxLogLines = 40;
        private const float ColumnWidth = 320f;
        private const float ColumnGap = 16f;

        private static CafeDebugOverlay _instance;

        // 기본적으로 숨김. 씬 시작 후 F1/F2 로 표시.
        private bool _visible;
        private bool _guideVisible;

        // 캐시된 씬 참조 (씬 재로드 시 재획득).
        private DayManager _dayManager;
        private BowlReceiver _bowl;
        private CustomerSpawner _spawner;
        private DialogueRunner _dialogue;

        // 이벤트 누적 통계.
        private int _servedCount;
        private int _successCount;
        private int _failCount;
        private bool _dialogueActive;
        private string _lastDrinkResult = "-";
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
        private Texture2D _barBgTex;
        private Texture2D _barFillTex;
        private Texture2D _guideTex;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (_instance != null)
            {
                return;
            }

            var go = new GameObject("[CafeDebugOverlay]");
            _instance = go.AddComponent<CafeDebugOverlay>();
            DontDestroyOnLoad(go);
        }

        private void OnEnable()
        {
            EventBus.OnStateChanged += HandleStateChanged;
            EventBus.OnCustomerArrived += HandleCustomerArrived;
            EventBus.OnCraftStarted += HandleCraftStarted;
            EventBus.OnIngredientAdded += HandleIngredientAdded;
            EventBus.OnDrinkCompleted += HandleDrinkCompleted;
            EventBus.OnLivesChanged += HandleLivesChanged;
            EventBus.OnDayCompleted += HandleDayCompleted;
            EventBus.OnDialogueStarted += HandleDialogueStarted;
            EventBus.OnDialogueEnded += HandleDialogueEnded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDisable()
        {
            EventBus.OnStateChanged -= HandleStateChanged;
            EventBus.OnCustomerArrived -= HandleCustomerArrived;
            EventBus.OnCraftStarted -= HandleCraftStarted;
            EventBus.OnIngredientAdded -= HandleIngredientAdded;
            EventBus.OnDrinkCompleted -= HandleDrinkCompleted;
            EventBus.OnLivesChanged -= HandleLivesChanged;
            EventBus.OnDayCompleted -= HandleDayCompleted;
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

        // ---- 이벤트 핸들러 (로그 + 통계 수집) ----

        private void HandleStateChanged(GameState from, GameState to)
        {
            _lastStateChange = $"{from} -> {to}";
            AddLog($"State {from} -> {to}");
        }

        private void HandleCustomerArrived(CustomerSO customer)
        {
            _servedCount++;
            var name = customer != null ? customer.id : "null";
            AddLog($"Customer arrived: {name}");
        }

        private void HandleCraftStarted()
        {
            AddLog("Craft started");
        }

        private void HandleIngredientAdded(IngredientSO ingredient)
        {
            var name = ingredient != null ? ResolveIngredientName(ingredient) : "null";
            AddLog($"Ingredient added: {name}");
        }

        private void HandleDrinkCompleted(RecipeSO recipe)
        {
            if (recipe != null)
            {
                _successCount++;
                _lastDrinkResult = $"SUCCESS ({ResolveRecipeName(recipe)})";
                AddLog($"Drink completed: {ResolveRecipeName(recipe)}");
            }
            else
            {
                _failCount++;
                _lastDrinkResult = "FAIL (wrong recipe)";
                AddLog("Drink failed: wrong recipe");
            }
        }

        private void HandleLivesChanged(int lives)
        {
            AddLog($"Lives changed: {lives}");
        }

        private void HandleDayCompleted(int day)
        {
            AddLog($"Day completed: {day}");
        }

        private void HandleDialogueStarted()
        {
            _dialogueActive = true;
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
            _dayManager = null;
            _bowl = null;
            _spawner = null;
            _dialogue = null;
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
            if (SceneManager.GetActiveScene().name != SceneRouter.SceneCafe)
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

            // 전체 화면을 패널로 사용하고, 내부를 다단 컬럼으로 나눠 세로 스크롤을 최소화한다.
            var panelRect = new Rect(8, 8, Screen.width - 16, Screen.height - 16);
            GUILayout.BeginArea(panelRect, _panelStyle);

            GUILayout.Label("CAFE DEBUG OVERLAY", _titleStyle);
            GUILayout.Label($"{ToggleKey}: 디버그 토글   {GuideKey}: 할 일 안내   |  Scene: {SceneManager.GetActiveScene().name}", _labelStyle);
            GUILayout.Space(4);

            GUILayout.BeginHorizontal();

            // 컬럼 1: 상태 + 진행
            GUILayout.BeginVertical(GUILayout.Width(ColumnWidth));
            DrawStateSection();
            DrawProgressSection();
            GUILayout.EndVertical();

            GUILayout.Space(ColumnGap);

            // 컬럼 2: 손님 + 보울 + 통계
            GUILayout.BeginVertical(GUILayout.Width(ColumnWidth));
            DrawCustomerSection();
            DrawBowlSection();
            DrawStatsSection();
            GUILayout.EndVertical();

            GUILayout.Space(ColumnGap);

            // 컬럼 3: 이벤트 로그 (남은 가로 폭 전체 사용)
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
            GUILayout.Label($"ServiceSub: {gm.StateMachine.ServiceSub}", _labelStyle);
            GUILayout.Label($"마지막 전환: {_lastStateChange}", _labelStyle);
            GUILayout.Label($"대화 진행중: {(_dialogueActive || (_dialogue != null && _dialogue.IsPlaying) ? "예" : "아니오")}", _labelStyle);
        }

        private void DrawProgressSection()
        {
            GUILayout.Space(6);
            GUILayout.Label("■ 진행 상황", _headerStyle);

            var gm = GameManager.Instance;
            int totalDays = gm != null && gm.Config != null ? gm.Config.totalDays : 0;
            int lives = gm != null && gm.Lives != null ? gm.Lives.Current : 0;
            int startLives = gm != null && gm.Config != null ? gm.Config.startingLives : 0;

            if (_dayManager != null)
            {
                GUILayout.Label($"Day  : {_dayManager.CurrentDay} / {totalDays}", _labelStyle);
                GUILayout.Label($"Fare : {_dayManager.Fare}  (보상 +{_dayManager.FareReward})", _labelStyle);
                GUILayout.Label($"남은 손님(큐): {_dayManager.RemainingInQueue}", _labelStyle);
                GUILayout.Label($"현재 손님 해결됨: {(_dayManager.IsResolved ? "예" : "아니오")}", _labelStyle);
            }
            else
            {
                GUILayout.Label("DayManager 없음", _labelStyle);
            }

            GUILayout.Label($"Lives: {lives} / {startLives}", _labelStyle);
        }

        private void DrawCustomerSection()
        {
            GUILayout.Space(6);
            GUILayout.Label("■ 현재 손님 / 목표 레시피", _headerStyle);

            var customer = _spawner != null ? _spawner.Current : (_dayManager != null ? _dayManager.CurrentCustomer : null);
            if (customer == null)
            {
                GUILayout.Label("손님 없음", _labelStyle);
                return;
            }

            GUILayout.Label($"ID: {customer.id}   Day: {customer.day}", _labelStyle);

            var recipe = customer.targetRecipe;
            if (recipe == null)
            {
                GUILayout.Label("목표 레시피: 없음", _labelStyle);
                return;
            }

            GUILayout.Label($"목표 음료: {ResolveRecipeName(recipe)}", _labelStyle);
            GUILayout.Label($"필요 재료: {DescribeIngredients(recipe.ingredients)}", _labelStyle);
        }

        private void DrawBowlSection()
        {
            GUILayout.Space(6);
            GUILayout.Label("■ 보울 (조합 중)", _headerStyle);

            if (_bowl == null)
            {
                GUILayout.Label("BowlReceiver 없음", _labelStyle);
                return;
            }

            var contents = _bowl.Contents;
            GUILayout.Label($"잠김: {(_bowl.IsLocked ? "예" : "아니오")}   재료 수: {(contents != null ? contents.Count : 0)}", _labelStyle);
            GUILayout.Label($"담긴 재료: {DescribeIngredients(contents)}", _labelStyle);

            // 현재 보울이 목표 레시피와 일치하는지 실시간 판정.
            var customer = _spawner != null ? _spawner.Current : (_dayManager != null ? _dayManager.CurrentCustomer : null);
            var target = customer != null ? customer.targetRecipe : null;
            if (target != null && contents != null)
            {
                bool wouldMatch = RecipeEvaluator.Matches(contents, target);
                GUILayout.Label($"현재 일치 여부: {(wouldMatch ? "[일치]" : "[불일치]")}", _labelStyle);
            }
        }

        private void DrawStatsSection()
        {
            GUILayout.Space(6);
            GUILayout.Label("■ 누적 통계", _headerStyle);
            GUILayout.Label($"응대 손님: {_servedCount}   성공: {_successCount}   실패: {_failCount}", _labelStyle);
            GUILayout.Label($"마지막 결과: {_lastDrinkResult}", _labelStyle);
        }

        private void DrawLogSection()
        {
            GUILayout.Space(6);
            GUILayout.Label($"■ 이벤트 로그 (최근 {MaxLogLines})", _headerStyle);

            // 남은 세로 공간을 모두 사용해 스크롤바가 거의 생기지 않도록 한다.
            _logScroll = GUILayout.BeginScrollView(_logScroll, GUILayout.ExpandHeight(true));
            for (int i = _log.Count - 1; i >= 0; i--)
            {
                GUILayout.Label(_log[i], _logStyle);
            }

            GUILayout.EndScrollView();
        }

        private void DrawBar(float ratio)
        {
            ratio = Mathf.Clamp01(ratio);
            var rect = GUILayoutUtility.GetRect(340, 14, GUILayout.ExpandWidth(false));
            GUI.DrawTexture(rect, _barBgTex);
            var fill = new Rect(rect.x, rect.y, rect.width * ratio, rect.height);
            GUI.DrawTexture(fill, _barFillTex);
        }

        // ---- F2 안내 패널: 사용자가 지금 무엇을 해야 하는지 표시 ----

        private void DrawGuidePanel()
        {
            BuildGuidance(out var title, out var body);

            const float width = 600f;
            float x = (Screen.width - width) * 0.5f;
            var rect = new Rect(x, 16f, width, 0f);

            // 높이를 내용에 맞춰 계산.
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

            var state = gm.StateMachine.Current;
            if (state != GameState.ServiceLoop)
            {
                body = GuidanceForState(state);
                return;
            }

            switch (gm.StateMachine.ServiceSub)
            {
                case ServiceSubState.CustomerEnter:
                    body = "손님이 입장하고 있습니다. 잠시 기다리세요.";
                    return;
                case ServiceSubState.OrderDialogue:
                    body = "손님의 주문 대화를 듣고 클릭(또는 진행 입력)으로 대사를 넘기세요.";
                    return;
                case ServiceSubState.Crafting:
                    body = BuildCraftingGuidance();
                    return;
                case ServiceSubState.FinishAnim:
                    body = "음료 완성 연출이 재생 중입니다. 잠시 기다리세요.";
                    return;
                case ServiceSubState.Result:
                    body = "결과를 확인하세요. 곧 다음 손님으로 넘어갑니다.";
                    return;
                default:
                    body = "손님을 기다리는 중입니다.";
                    return;
            }
        }

        private string BuildCraftingGuidance()
        {
            var customer = _spawner != null
                ? _spawner.Current
                : (_dayManager != null ? _dayManager.CurrentCustomer : null);
            var recipe = customer != null ? customer.targetRecipe : null;

            if (recipe == null || recipe.ingredients == null || recipe.ingredients.Length == 0)
            {
                return "음료를 만드세요: 재료를 보울에 드래그한 뒤, 막자(pestle)를 보울 위로 끌어 섞으세요.";
            }

            var contents = _bowl != null ? _bowl.Contents : null;
            string need = DescribeRemainingNeeds(recipe.ingredients, contents);

            string baseMsg = $"'{ResolveRecipeName(recipe)}'을(를) 만드세요.\n" +
                             $"필요 재료: {DescribeIngredients(recipe.ingredients)}";

            if (string.IsNullOrEmpty(need))
            {
                return baseMsg + "\n모든 재료가 준비됐습니다. 막자(pestle)를 보울 위로 드래그해 섞으세요!";
            }

            return baseMsg + $"\n아직 넣어야 할 재료: {need}\n재료를 보울에 드래그하세요.";
        }

        private static string DescribeRemainingNeeds(IReadOnlyList<IngredientSO> target, IReadOnlyList<IngredientSO> bowl)
        {
            var need = new Dictionary<IngredientSO, int>();
            if (target != null)
            {
                for (int i = 0; i < target.Count; i++)
                {
                    var ing = target[i];
                    if (ing == null)
                    {
                        continue;
                    }

                    need.TryGetValue(ing, out var c);
                    need[ing] = c + 1;
                }
            }

            if (bowl != null)
            {
                for (int i = 0; i < bowl.Count; i++)
                {
                    var ing = bowl[i];
                    if (ing != null && need.TryGetValue(ing, out var c))
                    {
                        if (c <= 1)
                        {
                            need.Remove(ing);
                        }
                        else
                        {
                            need[ing] = c - 1;
                        }
                    }
                }
            }

            if (need.Count == 0)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            foreach (var pair in need)
            {
                if (sb.Length > 0)
                {
                    sb.Append(", ");
                }

                sb.Append(ResolveIngredientName(pair.Key));
                if (pair.Value > 1)
                {
                    sb.Append($" x{pair.Value}");
                }
            }

            return sb.ToString();
        }

        private static string GuidanceForState(GameState state)
        {
            switch (state)
            {
                case GameState.Lobby:
                    return "로비입니다. 게임을 시작하세요.";
                case GameState.IntroStage1:
                    return "인트로를 진행하세요.";
                case GameState.CafeIntro:
                    return "카페 인트로를 감상하세요.";
                case GameState.Tutorial:
                    return "튜토리얼 안내에 따라 진행하세요.";
                case GameState.DayEnd:
                    return "하루가 끝났습니다. 결산을 확인하세요.";
                case GameState.Ending:
                    return "엔딩입니다. 수고하셨습니다!";
                case GameState.GameOver:
                    return "게임 오버. 다시 시도하세요.";
                default:
                    return "대기 중...";
            }
        }

        // ---- 헬퍼 ----

        private void AcquireReferences()
        {
            if (_dayManager == null)
            {
                _dayManager = FindFirstObjectByType<DayManager>();
            }

            if (_bowl == null)
            {
                _bowl = FindFirstObjectByType<BowlReceiver>();
            }

            if (_spawner == null)
            {
                _spawner = FindFirstObjectByType<CustomerSpawner>();
            }

            if (_dialogue == null)
            {
                _dialogue = FindFirstObjectByType<DialogueRunner>();
            }
        }

        private static string ResolveRecipeName(RecipeSO recipe)
        {
            if (recipe == null)
            {
                return "null";
            }

            if (!string.IsNullOrEmpty(recipe.drinkName))
            {
                return recipe.drinkName;
            }

            return !string.IsNullOrEmpty(recipe.id) ? recipe.id : recipe.name;
        }

        private static string ResolveIngredientName(IngredientSO ingredient)
        {
            if (ingredient == null)
            {
                return "null";
            }

            if (!string.IsNullOrEmpty(ingredient.displayName))
            {
                return ingredient.displayName;
            }

            return !string.IsNullOrEmpty(ingredient.id) ? ingredient.id : ingredient.name;
        }

        private static string DescribeIngredients(IReadOnlyList<IngredientSO> ingredients)
        {
            if (ingredients == null || ingredients.Count == 0)
            {
                return "(없음)";
            }

            var sb = new StringBuilder();
            for (int i = 0; i < ingredients.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(", ");
                }

                sb.Append(ResolveIngredientName(ingredients[i]));
            }

            return sb.ToString();
        }

        private void EnsureStyles()
        {
            if (_stylesReady)
            {
                return;
            }

            _panelTex = MakeTex(new Color(0f, 0f, 0f, 0.78f));
            _barBgTex = MakeTex(new Color(0.2f, 0.2f, 0.2f, 0.9f));
            _barFillTex = MakeTex(new Color(0.3f, 0.8f, 0.4f, 0.95f));

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
                normal = { textColor = new Color(1f, 0.85f, 0.4f) }
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

            _guideTex = MakeTex(new Color(0.05f, 0.18f, 0.10f, 0.92f));
            _guidePanelStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = _guideTex },
                border = new RectOffset(2, 2, 2, 2)
            };

            _guideTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 17,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.95f, 0.5f) }
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
