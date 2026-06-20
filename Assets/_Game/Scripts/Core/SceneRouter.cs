using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GemCafe.Core
{
    public class SceneRouter : MonoBehaviour
    {
        public const string SceneLobby = "Lobby";
        public const string SceneStage1 = "Stage1_Riverside";
        public const string SceneCafe = "Cafe";
        public const string SceneEnding = "Ending";

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
            var operation = SceneManager.LoadSceneAsync(sceneName);

            while (!operation.isDone)
            {
                yield return null;
            }

            IsLoading = false;
            onComplete?.Invoke();
        }
    }
}