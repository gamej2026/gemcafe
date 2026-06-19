using System;
using System.Collections.Generic;
using System.IO;
using GemCafe.Core;
using GemCafe.Crafting;
using GemCafe.Customer;
using GemCafe.Data;
using GemCafe.Dialogue;
using GemCafe.Player;
using GemCafe.Stage;
using GemCafe.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GemCafe.EditorTools
{
    public static class GemCafeSceneBuilder
    {
        private const string DataRoot = "Assets/_Game/Data";
        private const string IngredientsDir = "Assets/_Game/Data/Ingredients";
        private const string RecipesDir = "Assets/_Game/Data/Recipes";
        private const string CustomersDir = "Assets/_Game/Data/Customers";
        private const string ScenesDir = "Assets/_Game/Scenes";

        private const string GameConfigPath = "Assets/_Game/Data/GameConfig.asset";
        private const string WaterPath = "Assets/_Game/Data/Ingredients/ing_water.asset";
        private const string SyrupPath = "Assets/_Game/Data/Ingredients/ing_syrup.asset";
        private const string ToppingPath = "Assets/_Game/Data/Ingredients/ing_topping.asset";
        private const string RecipeDay1Path = "Assets/_Game/Data/Recipes/rcp_day1.asset";
        private const string RecipeDay2Path = "Assets/_Game/Data/Recipes/rcp_day2.asset";
        private const string RecipeDay3Path = "Assets/_Game/Data/Recipes/rcp_day3.asset";
        private const string CustomerDay1Path = "Assets/_Game/Data/Customers/cst_day1.asset";
        private const string CustomerDay2Path = "Assets/_Game/Data/Customers/cst_day2.asset";
        private const string CustomerDay3Path = "Assets/_Game/Data/Customers/cst_day3.asset";
        private const string CafeScenePath = "Assets/_Game/Scenes/Cafe.unity";
        private const string LobbyScenePath = "Assets/_Game/Scenes/Lobby.unity";
        private const string Stage1ScenePath = "Assets/_Game/Scenes/Stage1_Riverside.unity";

        [MenuItem("GemCafe/Build/0. Build All")]
        public static void BuildAll()
        {
            CreateSampleData();
            BuildCafeScene();
            BuildLobbyScene();
            BuildStage1Scene();
            RegisterScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("GemCafe/Build/1. Create Sample Data")]
        public static void CreateSampleData()
        {
            EnsureFolder(DataRoot);
            EnsureFolder(IngredientsDir);
            EnsureFolder(RecipesDir);
            EnsureFolder(CustomersDir);

            var gameConfig = LoadOrCreateAsset<GameConfig>(GameConfigPath);
            var water = LoadOrCreateAsset<IngredientSO>(WaterPath);
            var syrup = LoadOrCreateAsset<IngredientSO>(SyrupPath);
            var topping = LoadOrCreateAsset<IngredientSO>(ToppingPath);

            water.id = "ing_water";
            water.displayName = "삼도천 물";
            water.category = IngredientCategory.Base;
            EditorUtility.SetDirty(water);

            syrup.id = "ing_syrup";
            syrup.displayName = "시럽";
            syrup.category = IngredientCategory.Syrup;
            EditorUtility.SetDirty(syrup);

            topping.id = "ing_topping";
            topping.displayName = "고명";
            topping.category = IngredientCategory.Topping;
            EditorUtility.SetDirty(topping);

            var rcpDay1 = LoadOrCreateAsset<RecipeSO>(RecipeDay1Path);
            rcpDay1.id = "rcp_day1";
            rcpDay1.drinkName = "1일차 음료";
            rcpDay1.ingredients = new[] { water, syrup };
            EditorUtility.SetDirty(rcpDay1);

            var rcpDay2 = LoadOrCreateAsset<RecipeSO>(RecipeDay2Path);
            rcpDay2.id = "rcp_day2";
            rcpDay2.drinkName = "2일차 음료";
            rcpDay2.ingredients = new[] { water, syrup, topping };
            EditorUtility.SetDirty(rcpDay2);

            var rcpDay3 = LoadOrCreateAsset<RecipeSO>(RecipeDay3Path);
            rcpDay3.id = "rcp_day3";
            rcpDay3.drinkName = "3일차 음료";
            rcpDay3.ingredients = new[] { water, topping };
            EditorUtility.SetDirty(rcpDay3);

            var cstDay1 = LoadOrCreateAsset<CustomerSO>(CustomerDay1Path);
            cstDay1.id = "cst_day1";
            cstDay1.day = 1;
            cstDay1.patience = 45f;
            cstDay1.targetRecipe = rcpDay1;
            cstDay1.orderDialogue = new[]
            {
                new DialogueLine
                {
                    speakerId = "손님",
                    text = "삼도천 물에 시럽 넣어주게.",
                    portrait = null
                }
            };
            EditorUtility.SetDirty(cstDay1);

            var cstDay2 = LoadOrCreateAsset<CustomerSO>(CustomerDay2Path);
            cstDay2.id = "cst_day2";
            cstDay2.day = 2;
            cstDay2.patience = 40f;
            cstDay2.targetRecipe = rcpDay2;
            cstDay2.orderDialogue = new[]
            {
                new DialogueLine
                {
                    speakerId = "손님",
                    text = "오늘은 고명까지 올려주게.",
                    portrait = null
                }
            };
            EditorUtility.SetDirty(cstDay2);

            var cstDay3 = LoadOrCreateAsset<CustomerSO>(CustomerDay3Path);
            cstDay3.id = "cst_day3";
            cstDay3.day = 3;
            cstDay3.patience = 35f;
            cstDay3.targetRecipe = rcpDay3;
            cstDay3.orderDialogue = new[]
            {
                new DialogueLine
                {
                    speakerId = "손님",
                    text = "시럽은 빼고 물과 고명으로 부탁하네.",
                    portrait = null
                }
            };
            EditorUtility.SetDirty(cstDay3);

            EditorUtility.SetDirty(gameConfig);
            AssetDatabase.SaveAssets();
        }

        [MenuItem("GemCafe/Build/2. Build Cafe Scene")]
        public static void BuildCafeScene()
        {
            CreateSampleData();
            AssetDatabase.SaveAssets();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Load asset references AFTER creating the new scene. Loading them before
            // NewScene(Single) causes Unity to unload them as "unused" during the scene
            // switch, leaving every asset reference null in the built scene.
            var gameConfig = AssetDatabase.LoadAssetAtPath<GameConfig>(GameConfigPath);
            var ingWater = AssetDatabase.LoadAssetAtPath<IngredientSO>(WaterPath);
            var ingSyrup = AssetDatabase.LoadAssetAtPath<IngredientSO>(SyrupPath);
            var ingTopping = AssetDatabase.LoadAssetAtPath<IngredientSO>(ToppingPath);
            var cstDay1 = AssetDatabase.LoadAssetAtPath<CustomerSO>(CustomerDay1Path);
            var cstDay2 = AssetDatabase.LoadAssetAtPath<CustomerSO>(CustomerDay2Path);
            var cstDay3 = AssetDatabase.LoadAssetAtPath<CustomerSO>(CustomerDay3Path);

            var eventSystemGo = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            var cameraGo = new GameObject("Main Camera", typeof(Camera));
            cameraGo.tag = "MainCamera";
            var mainCam = cameraGo.GetComponent<Camera>();
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = new Color(0.12f, 0.12f, 0.14f, 1f);
            mainCam.orthographic = true;
            mainCam.orthographicSize = 5f;
            mainCam.nearClipPlane = -10f;
            mainCam.farClipPlane = 100f;
            cameraGo.transform.position = new Vector3(0f, 0f, -10f);

            var gameManagerGo = new GameObject("GameManager", typeof(GameManager), typeof(SceneRouter));
            var gameManager = gameManagerGo.GetComponent<GameManager>();
            var sceneRouter = gameManagerGo.GetComponent<SceneRouter>();
            SetObjectRef(gameManager, "config", gameConfig);
            SetObjectRef(gameManager, "sceneRouter", sceneRouter);

            var audioManagerGo = new GameObject("AudioManager", typeof(AudioManager));
            var audioManager = audioManagerGo.GetComponent<AudioManager>();
            var sfxSource = audioManagerGo.AddComponent<AudioSource>();
            var bgmSource = audioManagerGo.AddComponent<AudioSource>();
            SetObjectRef(audioManager, "sfxSource", sfxSource);
            SetObjectRef(audioManager, "bgmSource", bgmSource);

            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var hudRoot = CreateUIObject("HUD", canvasGo.transform, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, Vector2.zero);
            var hud = hudRoot.AddComponent<HUD>();
            var lifeIcons = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                var iconGo = CreateUIObject("LifeIcon_" + (i + 1), hudRoot.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f + i * 70f, -20f), new Vector2(56f, 56f), new Vector2(0f, 1f));
                var iconImage = iconGo.AddComponent<Image>();
                iconImage.color = Color.red;
                lifeIcons[i] = iconImage;
            }

            var patienceGo = CreateUIObject("PatienceFill", hudRoot.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(250f, -20f), new Vector2(360f, 32f), new Vector2(0f, 1f));
            var patienceImage = patienceGo.AddComponent<Image>();
            patienceImage.color = new Color(0.15f, 0.85f, 0.15f, 1f);
            patienceImage.type = Image.Type.Filled;
            patienceImage.fillMethod = Image.FillMethod.Horizontal;
            patienceImage.fillAmount = 1f;
            SetObjectRefArray(hud, "lifeIcons", lifeIcons);
            SetObjectRef(hud, "patienceFill", patienceImage);

            var customerImageGo = CreateUIObject("CustomerImage", canvasGo.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -180f), new Vector2(300f, 300f), new Vector2(0.5f, 1f));
            var customerImage = customerImageGo.AddComponent<Image>();
            customerImage.color = new Color(0.75f, 0.85f, 0.95f, 1f);

            var dialogueRoot = CreateUIObject("Dialogue", canvasGo.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(1500f, 250f), new Vector2(0.5f, 0f));
            var dialoguePanelImage = dialogueRoot.AddComponent<Image>();
            dialoguePanelImage.color = new Color(0f, 0f, 0f, 0.65f);
            var dialogueCanvasGroup = dialogueRoot.AddComponent<CanvasGroup>();

            var speakerNameGo = CreateUIObject("SpeakerName", dialogueRoot.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -15f), new Vector2(220f, 40f), new Vector2(0f, 1f));
            var speakerNameText = speakerNameGo.AddComponent<Text>();
            ApplyDefaultText(speakerNameText, "손님", 28, TextAnchor.UpperLeft, Color.white);

            var bodyTextGo = CreateUIObject("Body", dialogueRoot.transform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(20f, 20f), new Vector2(-180f, -80f), new Vector2(0f, 0f));
            var bodyText = bodyTextGo.AddComponent<Text>();
            ApplyDefaultText(bodyText, string.Empty, 30, TextAnchor.UpperLeft, Color.white);

            var nextButtonGo = CreateUIObject("NextButton", dialogueRoot.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-20f, 20f), new Vector2(140f, 54f), new Vector2(1f, 0f));
            var nextButtonImage = nextButtonGo.AddComponent<Image>();
            nextButtonImage.color = new Color(0.25f, 0.45f, 0.8f, 1f);
            var nextButton = nextButtonGo.AddComponent<Button>();
            var nextTextGo = CreateUIObject("Text", nextButtonGo.transform, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            var nextText = nextTextGo.AddComponent<Text>();
            ApplyDefaultText(nextText, "다음", 24, TextAnchor.MiddleCenter, Color.white);

            var dialogueView = dialogueRoot.AddComponent<DialogueView>();
            SetObjectRef(dialogueView, "root", dialogueCanvasGroup);
            SetObjectRef(dialogueView, "speakerNameText", speakerNameText);
            SetObjectRef(dialogueView, "bodyText", bodyText);
            SetObjectRef(dialogueView, "nextButton", nextButton);

            var speakerViewGo = new GameObject("SpeakerView", typeof(RectTransform));
            speakerViewGo.transform.SetParent(canvasGo.transform, false);
            var speakerView = speakerViewGo.AddComponent<SpeakerView>();
            var leftPortraitGo = CreateUIObject("LeftPortrait", speakerViewGo.transform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(180f, 260f), new Vector2(220f, 300f), new Vector2(0f, 0f));
            var leftPortrait = leftPortraitGo.AddComponent<Image>();
            leftPortrait.color = new Color(0.3f, 0.65f, 1f, 1f);
            var rightPortraitGo = CreateUIObject("RightPortrait", speakerViewGo.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-180f, 260f), new Vector2(220f, 300f), new Vector2(1f, 0f));
            var rightPortrait = rightPortraitGo.AddComponent<Image>();
            rightPortrait.color = new Color(1f, 0.65f, 0.2f, 1f);
            var dimGo = CreateUIObject("BackgroundDim", speakerViewGo.transform, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            dimGo.transform.SetAsFirstSibling();
            var backgroundDim = dimGo.AddComponent<Image>();
            backgroundDim.color = new Color(0f, 0f, 0f, 0.35f);
            SetObjectRef(speakerView, "leftPortrait", leftPortrait);
            SetObjectRef(speakerView, "rightPortrait", rightPortrait);
            SetObjectRef(speakerView, "backgroundDim", backgroundDim);
            SetString(speakerView, "leftSpeakerId", "주인공");

            var dialogueRunnerGo = new GameObject("DialogueRunner");
            dialogueRunnerGo.transform.SetParent(canvasGo.transform, false);
            var dialogueRunner = dialogueRunnerGo.AddComponent<DialogueRunner>();
            SetObjectRef(dialogueRunner, "view", dialogueView);
            SetObjectRef(dialogueRunner, "speakerView", speakerView);

            var craftingRoot = CreateUIObject("Crafting", canvasGo.transform, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            var trayPanel = CreateUIObject("Tray", craftingRoot.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(200f, 0f), new Vector2(360f, 500f), new Vector2(0f, 0.5f));
            var trayImage = trayPanel.AddComponent<Image>();
            trayImage.color = new Color(0.15f, 0.2f, 0.25f, 0.9f);

            var trayController = trayPanel.AddComponent<TrayController>();
            SetObjectRef(trayController, "panel", trayPanel.GetComponent<RectTransform>());
            SetVector2(trayController, "openAnchoredPos", new Vector2(200f, 0f));
            SetVector2(trayController, "closedAnchoredPos", new Vector2(-380f, 0f));

            var ingredientSOs = new[] { ingWater, ingSyrup, ingTopping };
            var ingredientNames = new[] { "물", "시럽", "고명" };
            var ingredientColors = new[]
            {
                new Color(0.3f, 0.7f, 1f, 1f),
                new Color(0.95f, 0.45f, 0.2f, 1f),
                new Color(0.35f, 0.85f, 0.35f, 1f)
            };

            for (int i = 0; i < 3; i++)
            {
                var ingGo = CreateUIObject("Ingredient_" + (i + 1), trayPanel.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -70f - i * 130f), new Vector2(260f, 110f), new Vector2(0.5f, 1f));
                var ingImg = ingGo.AddComponent<Image>();
                ingImg.color = ingredientColors[i];
                var ingCg = ingGo.AddComponent<CanvasGroup>();
                ingCg.blocksRaycasts = true;

                var ingLabelGo = CreateUIObject("Name", ingGo.transform, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
                var ingLabel = ingLabelGo.AddComponent<Text>();
                ApplyDefaultText(ingLabel, ingredientNames[i], 26, TextAnchor.MiddleCenter, Color.black);

                var draggable = ingGo.AddComponent<DraggableIngredient>();
                SetObjectRef(draggable, "ingredient", ingredientSOs[i]);
                SetObjectRef(draggable, "canvas", canvas);
                SetObjectRef(draggable, "iconImage", ingImg);
            }

            var bowlGo = CreateUIObject("Bowl", craftingRoot.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(120f, -100f), new Vector2(260f, 170f), new Vector2(0.5f, 0.5f));
            var bowlImage = bowlGo.AddComponent<Image>();
            bowlImage.color = new Color(0.8f, 0.75f, 0.6f, 1f);
            var bowlReceiver = bowlGo.AddComponent<BowlReceiver>();
            SetObjectRef(bowlReceiver, "bowlRect", bowlGo.GetComponent<RectTransform>());
            SetObjectRef(bowlReceiver, "uiCamera", null);

            var pestleGo = CreateUIObject("Pestle", craftingRoot.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(360f, -50f), new Vector2(90f, 210f), new Vector2(0.5f, 0.5f));
            var pestleImage = pestleGo.AddComponent<Image>();
            pestleImage.color = new Color(0.45f, 0.3f, 0.2f, 1f);
            var pestleMixer = pestleGo.AddComponent<PestleMixer>();
            SetObjectRef(pestleMixer, "bowl", bowlReceiver);
            SetObjectRef(pestleMixer, "bowlRect", bowlGo.GetComponent<RectTransform>());
            SetObjectRef(pestleMixer, "uiCamera", null);
            SetObjectRef(pestleMixer, "pestleRect", pestleGo.GetComponent<RectTransform>());

            var craftingControllerGo = new GameObject("CraftingController");
            craftingControllerGo.transform.SetParent(craftingRoot.transform, false);
            var craftingController = craftingControllerGo.AddComponent<CraftingController>();
            SetObjectRef(craftingController, "tray", trayController);
            SetObjectRef(craftingController, "bowl", bowlReceiver);
            SetObjectRef(craftingController, "pestle", pestleMixer);

            var popupManagerGo = new GameObject("PopupManager");
            popupManagerGo.transform.SetParent(canvasGo.transform, false);
            var popupManager = popupManagerGo.AddComponent<PopupManager>();
            var popupTypes = new[] { PopupType.Settings, PopupType.Recipe, PopupType.DialogueLog };
            var popupNames = new[] { "Popup_Settings", "Popup_Recipe", "Popup_DialogueLog" };
            var popupArray = new Popup[3];

            for (int i = 0; i < 3; i++)
            {
                var popupGo = CreateUIObject(popupNames[i], popupManagerGo.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(700f, 420f), new Vector2(0.5f, 0.5f));
                var popup = popupGo.AddComponent<Popup>();
                var popupCg = popupGo.AddComponent<CanvasGroup>();

                var dimGo2 = CreateUIObject("Dim", popupGo.transform, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
                dimGo2.transform.SetAsFirstSibling();
                var dimImage = dimGo2.AddComponent<Image>();
                dimImage.color = new Color(0f, 0f, 0f, 0.55f);

                var panelGo = CreateUIObject("Panel", popupGo.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560f, 300f), new Vector2(0.5f, 0.5f));
                var panelImage = panelGo.AddComponent<Image>();
                panelImage.color = new Color(0.2f, 0.2f, 0.25f, 1f);

                var closeButtonGo = CreateUIObject("CloseButton", panelGo.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-20f, -20f), new Vector2(120f, 54f), new Vector2(1f, 1f));
                var closeImage = closeButtonGo.AddComponent<Image>();
                closeImage.color = new Color(0.8f, 0.2f, 0.2f, 1f);
                var closeButton = closeButtonGo.AddComponent<Button>();
                var closeTextGo = CreateUIObject("Text", closeButtonGo.transform, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
                var closeText = closeTextGo.AddComponent<Text>();
                ApplyDefaultText(closeText, "닫기", 24, TextAnchor.MiddleCenter, Color.white);

                SetEnum(popup, "type", (int)popupTypes[i]);
                SetObjectRef(popup, "root", popupCg);
                SetObjectRef(popup, "closeButton", closeButton);
                SetObjectRef(popup, "dim", dimImage);

                popup.Close();
                popupArray[i] = popup;
            }

            SetObjectRefArray(popupManager, "popups", popupArray);

            var resultToastGo = CreateUIObject("ResultToast", canvasGo.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 250f), new Vector2(520f, 120f), new Vector2(0.5f, 0.5f));
            var resultToast = resultToastGo.AddComponent<ResultToast>();
            var resultToastRoot = resultToastGo.AddComponent<CanvasGroup>();
            var resultToastBg = resultToastGo.AddComponent<Image>();
            resultToastBg.color = new Color(0f, 0f, 0f, 0.65f);
            var resultTextGo = CreateUIObject("MessageText", resultToastGo.transform, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            var resultText = resultTextGo.AddComponent<Text>();
            ApplyDefaultText(resultText, "", 34, TextAnchor.MiddleCenter, Color.white);
            SetObjectRef(resultToast, "root", resultToastRoot);
            SetObjectRef(resultToast, "messageText", resultText);
            resultToastRoot.alpha = 0f;
            resultToastRoot.interactable = false;
            resultToastRoot.blocksRaycasts = false;

            var transitionGo = new GameObject("ScreenTransition", typeof(RectTransform));
            transitionGo.transform.SetParent(canvasGo.transform, false);
            var screenTransition = transitionGo.AddComponent<ScreenTransition>();
            var transitionPanelGo = CreateUIObject("TransitionPanel", transitionGo.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(1920f, 1080f), new Vector2(0.5f, 0.5f));
            var transitionImage = transitionPanelGo.AddComponent<Image>();
            transitionImage.color = new Color(0f, 0f, 0f, 0.35f);
            var transitionRect = transitionPanelGo.GetComponent<RectTransform>();
            SetObjectRef(screenTransition, "panel", transitionRect);
            SetVector2(screenTransition, "offscreenRight", new Vector2(2200f, 0f));
            SetVector2(screenTransition, "onscreen", new Vector2(0f, 0f));
            transitionRect.anchoredPosition = new Vector2(2200f, 0f);

            var spawnerGo = new GameObject("CustomerSpawner", typeof(CustomerSpawner));
            var spawner = spawnerGo.GetComponent<CustomerSpawner>();
            SetObjectRef(spawner, "customerImage", customerImage);
            SetFloat(spawner, "fadeDuration", 0.5f);

            var dayManagerGo = new GameObject("DayManager", typeof(DayManager), typeof(PatienceTimer));
            var dayManager = dayManagerGo.GetComponent<DayManager>();
            var patienceTimer = dayManagerGo.GetComponent<PatienceTimer>();
            SetObjectRef(dayManager, "spawner", spawner);
            SetObjectRef(dayManager, "dialogue", dialogueRunner);
            SetObjectRef(dayManager, "crafting", craftingController);
            SetObjectRef(dayManager, "patience", patienceTimer);
            SetObjectRef(dayManager, "resultToast", resultToast);
            SetObjectRef(dayManager, "craftTransition", screenTransition);
            SetObjectRefList(dayManager, "allCustomers", new[] { cstDay1, cstDay2, cstDay3 });
            SetBool(dayManager, "forceServiceStateOnStart", true);

            EnsureFolder(ScenesDir);
            EditorSceneManager.SaveScene(scene, CafeScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("GemCafeSceneBuilder: Cafe scene build complete.");
        }

        [MenuItem("GemCafe/Build/4. Build Lobby Scene")]
        public static void BuildLobbyScene()
        {
            CreateSampleData();
            AssetDatabase.SaveAssets();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var gameConfig = AssetDatabase.LoadAssetAtPath<GameConfig>(GameConfigPath);

            var eventSystemGo = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            var cameraGo = new GameObject("Main Camera", typeof(Camera));
            cameraGo.tag = "MainCamera";
            var mainCam = cameraGo.GetComponent<Camera>();
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = new Color(0.12f, 0.12f, 0.14f, 1f);
            mainCam.orthographic = true;
            mainCam.orthographicSize = 5f;
            mainCam.nearClipPlane = -10f;
            mainCam.farClipPlane = 100f;
            cameraGo.transform.position = new Vector3(0f, 0f, -10f);

            var gameManagerGo = new GameObject("GameManager", typeof(GameManager), typeof(SceneRouter));
            var gameManager = gameManagerGo.GetComponent<GameManager>();
            var sceneRouter = gameManagerGo.GetComponent<SceneRouter>();
            SetObjectRef(gameManager, "config", gameConfig);
            SetObjectRef(gameManager, "sceneRouter", sceneRouter);

            var audioManagerGo = new GameObject("AudioManager", typeof(AudioManager));
            var audioManager = audioManagerGo.GetComponent<AudioManager>();
            var sfxSource = audioManagerGo.AddComponent<AudioSource>();
            var bgmSource = audioManagerGo.AddComponent<AudioSource>();
            SetObjectRef(audioManager, "sfxSource", sfxSource);
            SetObjectRef(audioManager, "bgmSource", bgmSource);

            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var titleGo = CreateUIObject("Title", canvasGo.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -160f), new Vector2(900f, 120f), new Vector2(0.5f, 1f));
            var titleText = titleGo.AddComponent<Text>();
            ApplyDefaultText(titleText, "삼도천 다방", 64, TextAnchor.MiddleCenter, Color.white);

            var newGameButtonGo = CreateUIObject("NewGameButton", canvasGo.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 110f), new Vector2(360f, 72f), new Vector2(0.5f, 0.5f));
            var newGameButtonImage = newGameButtonGo.AddComponent<Image>();
            newGameButtonImage.color = new Color(0.25f, 0.45f, 0.8f, 1f);
            var newGameButton = newGameButtonGo.AddComponent<Button>();
            var newGameTextGo = CreateUIObject("Text", newGameButtonGo.transform, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            var newGameText = newGameTextGo.AddComponent<Text>();
            ApplyDefaultText(newGameText, "새 게임", 24, TextAnchor.MiddleCenter, Color.white);

            var continueButtonGo = CreateUIObject("ContinueButton", canvasGo.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(360f, 72f), new Vector2(0.5f, 0.5f));
            var continueButtonImage = continueButtonGo.AddComponent<Image>();
            continueButtonImage.color = new Color(0.25f, 0.45f, 0.8f, 1f);
            var continueButton = continueButtonGo.AddComponent<Button>();
            var continueTextGo = CreateUIObject("Text", continueButtonGo.transform, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            var continueText = continueTextGo.AddComponent<Text>();
            ApplyDefaultText(continueText, "이어하기", 24, TextAnchor.MiddleCenter, Color.white);

            var settingsButtonGo = CreateUIObject("SettingsButton", canvasGo.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -70f), new Vector2(360f, 72f), new Vector2(0.5f, 0.5f));
            var settingsButtonImage = settingsButtonGo.AddComponent<Image>();
            settingsButtonImage.color = new Color(0.25f, 0.45f, 0.8f, 1f);
            var settingsButton = settingsButtonGo.AddComponent<Button>();
            var settingsTextGo = CreateUIObject("Text", settingsButtonGo.transform, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            var settingsText = settingsTextGo.AddComponent<Text>();
            ApplyDefaultText(settingsText, "설정", 24, TextAnchor.MiddleCenter, Color.white);

            var quitButtonGo = CreateUIObject("QuitButton", canvasGo.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -160f), new Vector2(360f, 72f), new Vector2(0.5f, 0.5f));
            var quitButtonImage = quitButtonGo.AddComponent<Image>();
            quitButtonImage.color = new Color(0.25f, 0.45f, 0.8f, 1f);
            var quitButton = quitButtonGo.AddComponent<Button>();
            var quitTextGo = CreateUIObject("Text", quitButtonGo.transform, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            var quitText = quitTextGo.AddComponent<Text>();
            ApplyDefaultText(quitText, "게임 종료", 24, TextAnchor.MiddleCenter, Color.white);

            var popupManagerGo = new GameObject("PopupManager");
            popupManagerGo.transform.SetParent(canvasGo.transform, false);
            var popupManager = popupManagerGo.AddComponent<PopupManager>();
            var popupTypes = new[] { PopupType.Settings, PopupType.Recipe, PopupType.DialogueLog };
            var popupNames = new[] { "Popup_Settings", "Popup_Recipe", "Popup_DialogueLog" };
            var popupArray = new Popup[3];

            for (int i = 0; i < 3; i++)
            {
                var popupGo = CreateUIObject(popupNames[i], popupManagerGo.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(700f, 420f), new Vector2(0.5f, 0.5f));
                var popup = popupGo.AddComponent<Popup>();
                var popupCg = popupGo.AddComponent<CanvasGroup>();

                var dimGo = CreateUIObject("Dim", popupGo.transform, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
                dimGo.transform.SetAsFirstSibling();
                var dimImage = dimGo.AddComponent<Image>();
                dimImage.color = new Color(0f, 0f, 0f, 0.55f);

                var panelGo = CreateUIObject("Panel", popupGo.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560f, 300f), new Vector2(0.5f, 0.5f));
                var panelImage = panelGo.AddComponent<Image>();
                panelImage.color = new Color(0.2f, 0.2f, 0.25f, 1f);

                var closeButtonGo = CreateUIObject("CloseButton", panelGo.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-20f, -20f), new Vector2(120f, 54f), new Vector2(1f, 1f));
                var closeImage = closeButtonGo.AddComponent<Image>();
                closeImage.color = new Color(0.8f, 0.2f, 0.2f, 1f);
                var closeButton = closeButtonGo.AddComponent<Button>();
                var closeTextGo = CreateUIObject("Text", closeButtonGo.transform, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
                var closeText = closeTextGo.AddComponent<Text>();
                ApplyDefaultText(closeText, "닫기", 24, TextAnchor.MiddleCenter, Color.white);

                SetEnum(popup, "type", (int)popupTypes[i]);
                SetObjectRef(popup, "root", popupCg);
                SetObjectRef(popup, "closeButton", closeButton);
                SetObjectRef(popup, "dim", dimImage);

                popup.Close();
                popupArray[i] = popup;
            }

            SetObjectRefArray(popupManager, "popups", popupArray);

            var lobbyMenuGo = new GameObject("LobbyMenu");
            lobbyMenuGo.transform.SetParent(canvasGo.transform, false);
            var lobbyMenu = lobbyMenuGo.AddComponent<LobbyMenu>();
            SetObjectRef(lobbyMenu, "newGameButton", newGameButton);
            SetObjectRef(lobbyMenu, "continueButton", continueButton);
            SetObjectRef(lobbyMenu, "settingsButton", settingsButton);
            SetObjectRef(lobbyMenu, "quitButton", quitButton);
            SetObjectRef(lobbyMenu, "popupManager", popupManager);

            EnsureFolder(ScenesDir);
            EditorSceneManager.SaveScene(scene, LobbyScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("GemCafeSceneBuilder: Lobby scene build complete.");
        }

        [MenuItem("GemCafe/Build/5. Build Stage1 Scene")]
        public static void BuildStage1Scene()
        {
            CreateSampleData();
            AssetDatabase.SaveAssets();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var gameConfig = AssetDatabase.LoadAssetAtPath<GameConfig>(GameConfigPath);

            var eventSystemGo = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            var gameManagerGo = new GameObject("GameManager", typeof(GameManager), typeof(SceneRouter));
            var gameManager = gameManagerGo.GetComponent<GameManager>();
            var sceneRouter = gameManagerGo.GetComponent<SceneRouter>();
            SetObjectRef(gameManager, "config", gameConfig);
            SetObjectRef(gameManager, "sceneRouter", sceneRouter);

            var audioManagerGo = new GameObject("AudioManager", typeof(AudioManager));
            var audioManager = audioManagerGo.GetComponent<AudioManager>();
            var sfxSource = audioManagerGo.AddComponent<AudioSource>();
            var bgmSource = audioManagerGo.AddComponent<AudioSource>();
            SetObjectRef(audioManager, "sfxSource", sfxSource);
            SetObjectRef(audioManager, "bgmSource", bgmSource);

            var playerGo = new GameObject("Player", typeof(SpriteRenderer), typeof(PlayerMover), typeof(Interactor));
            playerGo.transform.position = new Vector3(0f, 0f, 0f);
            var playerSpriteRenderer = playerGo.GetComponent<SpriteRenderer>();
            var playerMover = playerGo.GetComponent<PlayerMover>();
            var interactor = playerGo.GetComponent<Interactor>();
            SetObjectRef(playerMover, "spriteRenderer", playerSpriteRenderer);
            SetFloat(playerMover, "fallbackMoveSpeed", 5f);
            SetFloat(interactor, "fallbackRadius", 1.5f);

            var cameraGo = new GameObject("Main Camera", typeof(Camera), typeof(CameraFollow));
            cameraGo.tag = "MainCamera";
            var mainCam = cameraGo.GetComponent<Camera>();
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = new Color(0.12f, 0.12f, 0.14f, 1f);
            mainCam.orthographic = true;
            mainCam.orthographicSize = 5f;
            mainCam.nearClipPlane = -10f;
            mainCam.farClipPlane = 100f;
            cameraGo.transform.position = new Vector3(0f, 0f, -10f);
            var cameraFollow = cameraGo.GetComponent<CameraFollow>();
            SetObjectRef(cameraFollow, "target", playerGo.transform);
            SetFloat(cameraFollow, "fallbackLerp", 2f);

            var dolsoeGo = new GameObject("NPC_Dolsoe", typeof(SpriteRenderer), typeof(BoxCollider2D), typeof(Interactable));
            dolsoeGo.transform.position = new Vector3(-4f, 0f, 0f);
            var dolsoeSprite = dolsoeGo.GetComponent<SpriteRenderer>();
            dolsoeSprite.color = new Color(0.4f, 0.7f, 1f, 1f);
            var dolsoeCollider = dolsoeGo.GetComponent<BoxCollider2D>();
            dolsoeCollider.isTrigger = true;
            var dolsoeInteractable = dolsoeGo.GetComponent<Interactable>();
            SetString(dolsoeInteractable, "displayName", "돌쇠");
            SetDialogueLines(dolsoeInteractable, "dialogue", new[]
            {
                ("돌쇠", "이보게, 삼도천을 건너려는가?"),
                ("주인공", "...네."),
                ("돌쇠", "저 위 다방에 마님을 찾아가 보게.")
            });
            var dolsoeHighlightGo = new GameObject("Highlight", typeof(SpriteRenderer));
            dolsoeHighlightGo.transform.SetParent(dolsoeGo.transform, false);
            var dolsoeHighlightSprite = dolsoeHighlightGo.GetComponent<SpriteRenderer>();
            dolsoeHighlightSprite.color = new Color(1f, 1f, 0.35f, 0.65f);
            dolsoeHighlightGo.SetActive(false);
            SetObjectRef(dolsoeInteractable, "highlightVisual", dolsoeHighlightGo);

            var manimGo = new GameObject("NPC_Manim", typeof(SpriteRenderer), typeof(BoxCollider2D), typeof(Interactable));
            manimGo.transform.position = new Vector3(4f, 0f, 0f);
            var manimSprite = manimGo.GetComponent<SpriteRenderer>();
            manimSprite.color = new Color(1f, 0.65f, 0.2f, 1f);
            var manimCollider = manimGo.GetComponent<BoxCollider2D>();
            manimCollider.isTrigger = true;
            var manimInteractable = manimGo.GetComponent<Interactable>();
            SetString(manimInteractable, "displayName", "마님");
            SetDialogueLines(manimInteractable, "dialogue", new[]
            {
                ("마님", "어서 오게. 일손이 필요하던 참이야."),
                ("주인공", "제가 돕겠습니다."),
                ("마님", "좋아, 안으로 들어오게.")
            });
            var manimHighlightGo = new GameObject("Highlight", typeof(SpriteRenderer));
            manimHighlightGo.transform.SetParent(manimGo.transform, false);
            var manimHighlightSprite = manimHighlightGo.GetComponent<SpriteRenderer>();
            manimHighlightSprite.color = new Color(1f, 1f, 0.35f, 0.65f);
            manimHighlightGo.SetActive(false);
            SetObjectRef(manimInteractable, "highlightVisual", manimHighlightGo);

            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var keyPromptGo = CreateUIObject("KeyPrompt", canvasGo.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -80f), new Vector2(120f, 120f), new Vector2(0.5f, 1f));
            var keyPromptImage = keyPromptGo.AddComponent<Image>();
            keyPromptImage.color = new Color(0f, 0f, 0f, 0.65f);
            var keyPromptTextGo = CreateUIObject("Text", keyPromptGo.transform, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            var keyPromptText = keyPromptTextGo.AddComponent<Text>();
            ApplyDefaultText(keyPromptText, "F", 42, TextAnchor.MiddleCenter, Color.white);
            keyPromptGo.SetActive(false);
            SetObjectRef(interactor, "keyPromptUI", keyPromptGo);

            var dialogueRoot = CreateUIObject("Dialogue", canvasGo.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(1500f, 250f), new Vector2(0.5f, 0f));
            var dialoguePanelImage = dialogueRoot.AddComponent<Image>();
            dialoguePanelImage.color = new Color(0f, 0f, 0f, 0.65f);
            var dialogueCanvasGroup = dialogueRoot.AddComponent<CanvasGroup>();

            var speakerNameGo = CreateUIObject("SpeakerName", dialogueRoot.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -15f), new Vector2(220f, 40f), new Vector2(0f, 1f));
            var speakerNameText = speakerNameGo.AddComponent<Text>();
            ApplyDefaultText(speakerNameText, "손님", 28, TextAnchor.UpperLeft, Color.white);

            var bodyTextGo = CreateUIObject("Body", dialogueRoot.transform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(20f, 20f), new Vector2(-180f, -80f), new Vector2(0f, 0f));
            var bodyText = bodyTextGo.AddComponent<Text>();
            ApplyDefaultText(bodyText, string.Empty, 30, TextAnchor.UpperLeft, Color.white);

            var nextButtonGo = CreateUIObject("NextButton", dialogueRoot.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-20f, 20f), new Vector2(140f, 54f), new Vector2(1f, 0f));
            var nextButtonImage = nextButtonGo.AddComponent<Image>();
            nextButtonImage.color = new Color(0.25f, 0.45f, 0.8f, 1f);
            var nextButton = nextButtonGo.AddComponent<Button>();
            var nextTextGo = CreateUIObject("Text", nextButtonGo.transform, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            var nextText = nextTextGo.AddComponent<Text>();
            ApplyDefaultText(nextText, "다음", 24, TextAnchor.MiddleCenter, Color.white);

            var dialogueView = dialogueRoot.AddComponent<DialogueView>();
            SetObjectRef(dialogueView, "root", dialogueCanvasGroup);
            SetObjectRef(dialogueView, "speakerNameText", speakerNameText);
            SetObjectRef(dialogueView, "bodyText", bodyText);
            SetObjectRef(dialogueView, "nextButton", nextButton);

            var speakerViewGo = new GameObject("SpeakerView", typeof(RectTransform));
            speakerViewGo.transform.SetParent(canvasGo.transform, false);
            var speakerView = speakerViewGo.AddComponent<SpeakerView>();
            var leftPortraitGo = CreateUIObject("LeftPortrait", speakerViewGo.transform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(180f, 260f), new Vector2(220f, 300f), new Vector2(0f, 0f));
            var leftPortrait = leftPortraitGo.AddComponent<Image>();
            leftPortrait.color = new Color(0.3f, 0.65f, 1f, 1f);
            var rightPortraitGo = CreateUIObject("RightPortrait", speakerViewGo.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-180f, 260f), new Vector2(220f, 300f), new Vector2(1f, 0f));
            var rightPortrait = rightPortraitGo.AddComponent<Image>();
            rightPortrait.color = new Color(1f, 0.65f, 0.2f, 1f);
            var dimGo = CreateUIObject("BackgroundDim", speakerViewGo.transform, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            dimGo.transform.SetAsFirstSibling();
            var backgroundDim = dimGo.AddComponent<Image>();
            backgroundDim.color = new Color(0f, 0f, 0f, 0.35f);
            SetObjectRef(speakerView, "leftPortrait", leftPortrait);
            SetObjectRef(speakerView, "rightPortrait", rightPortrait);
            SetObjectRef(speakerView, "backgroundDim", backgroundDim);
            SetString(speakerView, "leftSpeakerId", "주인공");

            var dialogueRunnerGo = new GameObject("DialogueRunner");
            dialogueRunnerGo.transform.SetParent(canvasGo.transform, false);
            var dialogueRunner = dialogueRunnerGo.AddComponent<DialogueRunner>();
            SetObjectRef(dialogueRunner, "view", dialogueView);
            SetObjectRef(dialogueRunner, "speakerView", speakerView);

            var directorGo = new GameObject("Stage1Director", typeof(Stage1Director));
            var director = directorGo.GetComponent<Stage1Director>();
            SetObjectRef(director, "interactor", interactor);
            SetObjectRef(director, "dialogueRunner", dialogueRunner);
            SetObjectRef(director, "exitInteractable", manimInteractable);

            EnsureFolder(ScenesDir);
            EditorSceneManager.SaveScene(scene, Stage1ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("GemCafeSceneBuilder: Stage1 scene build complete.");
        }

        [MenuItem("GemCafe/Build/3. Register Scenes In Build")]
        public static void RegisterScenes()
        {
            var existing = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes ?? Array.Empty<EditorBuildSettingsScene>());
            var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < existing.Count; i++)
            {
                if (!string.IsNullOrEmpty(existing[i].path))
                {
                    paths.Add(existing[i].path);
                }
            }

            if (!paths.Contains(LobbyScenePath) && File.Exists(Path.GetFullPath(LobbyScenePath)))
            {
                existing.Insert(0, new EditorBuildSettingsScene(LobbyScenePath, true));
                paths.Add(LobbyScenePath);
            }

            AddSceneIfExists(existing, paths, Stage1ScenePath);
            AddSceneIfExists(existing, paths, CafeScenePath);

            EditorBuildSettings.scenes = existing.ToArray();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void AddSceneIfExists(List<EditorBuildSettingsScene> list, HashSet<string> pathSet, string scenePath)
        {
            if (!File.Exists(Path.GetFullPath(scenePath)))
            {
                return;
            }

            if (pathSet.Contains(scenePath))
            {
                return;
            }

            list.Add(new EditorBuildSettingsScene(scenePath, true));
            pathSet.Add(scenePath);
        }

        private static T LoadOrCreateAsset<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                return asset;
            }

            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
            {
                EnsureFolder(dir.Replace('\\', '/'));
            }

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void EnsureFolder(string assetPath)
        {
            string fullPath = Path.GetFullPath(assetPath);
            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
                AssetDatabase.Refresh();
            }
        }

        private static GameObject CreateUIObject(
            string name,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            Vector2 pivot)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = sizeDelta;
            return go;
        }

        private static void ApplyDefaultText(Text text, string content, int fontSize, TextAnchor alignment, Color color)
        {
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
        }

        private static void SetObjectRef(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
        {
            var so = new SerializedObject(target);
            var property = so.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException("Property not found: " + propertyName + " on " + target.GetType().Name);
            }

            property.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloat(UnityEngine.Object target, string propertyName, float value)
        {
            var so = new SerializedObject(target);
            var property = so.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException("Property not found: " + propertyName + " on " + target.GetType().Name);
            }

            property.floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetBool(UnityEngine.Object target, string propertyName, bool value)
        {
            var so = new SerializedObject(target);
            var property = so.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException("Property not found: " + propertyName + " on " + target.GetType().Name);
            }

            property.boolValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetString(UnityEngine.Object target, string propertyName, string value)
        {
            var so = new SerializedObject(target);
            var property = so.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException("Property not found: " + propertyName + " on " + target.GetType().Name);
            }

            property.stringValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetVector2(UnityEngine.Object target, string propertyName, Vector2 value)
        {
            var so = new SerializedObject(target);
            var property = so.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException("Property not found: " + propertyName + " on " + target.GetType().Name);
            }

            property.vector2Value = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetEnum(UnityEngine.Object target, string propertyName, int enumIndex)
        {
            var so = new SerializedObject(target);
            var property = so.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException("Property not found: " + propertyName + " on " + target.GetType().Name);
            }

            property.enumValueIndex = enumIndex;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetObjectRefArray(UnityEngine.Object target, string propertyName, UnityEngine.Object[] values)
        {
            var so = new SerializedObject(target);
            var property = so.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException("Property not found: " + propertyName + " on " + target.GetType().Name);
            }

            property.arraySize = values != null ? values.Length : 0;
            for (int i = 0; i < property.arraySize; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetDialogueLines(UnityEngine.Object target, string propertyName, (string speakerId, string text)[] lines)
        {
            var so = new SerializedObject(target);
            var property = so.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException("Property not found: " + propertyName + " on " + target.GetType().Name);
            }

            property.arraySize = lines != null ? lines.Length : 0;
            for (int i = 0; i < property.arraySize; i++)
            {
                var el = property.GetArrayElementAtIndex(i);
                el.FindPropertyRelative("speakerId").stringValue = lines[i].speakerId;
                el.FindPropertyRelative("text").stringValue = lines[i].text;
                el.FindPropertyRelative("portrait").objectReferenceValue = null;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetObjectRefList(UnityEngine.Object target, string propertyName, CustomerSO[] values)
        {
            var so = new SerializedObject(target);
            var property = so.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException("Property not found: " + propertyName + " on " + target.GetType().Name);
            }

            property.arraySize = values != null ? values.Length : 0;
            for (int i = 0; i < property.arraySize; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
