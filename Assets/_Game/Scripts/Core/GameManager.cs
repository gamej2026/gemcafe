using GemCafe.Customer;
using UnityEngine;

namespace GemCafe.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private GameConfig config;
        [SerializeField] private SceneRouter sceneRouter;

        public GameConfig Config => config;
        public SceneRouter Router => sceneRouter;
        public GameStateMachine StateMachine { get; private set; }
        public LivesSystem Lives { get; private set; }
        public int ContinueStartDay { get; private set; } = 1;
        public int ContinueStartFare { get; private set; }

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            StateMachine = new GameStateMachine();
            var startingLives = config != null ? config.startingLives : 3;
            Lives = new LivesSystem(startingLives);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void StartNewGame()
        {
            SaveSystem.Delete();
            var lives = config != null ? config.startingLives : 3;
            Lives.Reset(lives);
            ContinueStartDay = 1;
            ContinueStartFare = 0;

            StateMachine.TryTransition(GameState.IntroStage1);
            Router.Load(SceneRouter.SceneStage1);
        }

        public void ContinueGame()
        {
            if (!SaveSystem.HasSave())
            {
                return;
            }

            var d = SaveSystem.Load();
            if (d == null)
            {
                return;
            }

            Lives.Reset(d.lives);
            ContinueStartDay = d.day;
            ContinueStartFare = d.fare;
            StateMachine.Restore(GameState.ServiceLoop);
            Router.Load(SceneRouter.SceneCafe);
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void GameOver()
        {
            if (StateMachine.Current == GameState.GameOver)
            {
                return;
            }

            StateMachine.TryTransition(GameState.GameOver);
            Router.Load(SceneRouter.SceneLobby);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
        }
    }
}