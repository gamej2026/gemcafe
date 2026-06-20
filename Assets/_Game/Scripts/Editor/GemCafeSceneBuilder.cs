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
        private const string MixMinigameConfigPath = "Assets/_Game/Data/MixMinigameConfig.asset";
        private const string PourMinigameConfigPath = "Assets/_Game/Data/PourMinigameConfig.asset";

        private const string ArtMaterialsDir = "Assets/_Game/Art/Materials";
        private const string OutlineMaterialPath = "Assets/_Game/Art/Materials/NPCOutline.mat";
        private const string NormalMaterialPath = "Assets/_Game/Art/Materials/NPCNormal.mat";
        private const string OutlineShaderName = "GemCafe/SpriteOutline";
        private const string CafeScenePath = "Assets/_Game/Scenes/Cafe.unity";
        private const string LobbyScenePath = "Assets/_Game/Scenes/Lobby.unity";
        private const string Stage1ScenePath = "Assets/_Game/Scenes/Stage1_Riverside.unity";

        // Stage1 빌더가 생성/관리하는 루트 오브젝트 이름. 빌드 시 이 이름들만 교체하고
        // 그 외 수동으로 추가한 오브젝트(프리팹 등)는 그대로 보존한다.
        private static readonly string[] Stage1ManagedRootNames =
        {
            "EventSystem",
            "GameManager",
            "AudioManager",
            "Player",
            "Main Camera",
            "NPC_Dolsoe",
            "NPC_Manim",
            "Canvas",
            "Stage1Director"
        };

        private const string ResTrayPath = "Assets/Resource/Tray.png";
        private const string ResRockPath = "Assets/Resource/Ingredient_0.png";     // 돌
        private const string ResGinsengPath = "Assets/Resource/Ingredient_1.png";  // 인삼
        private const string ResPersimmonPath = "Assets/Resource/Ingredient_2.png"; // 곶감

        private const string IllustDolsoePath = "Assets/Images/dolsoe_illust.png"; // 돌쇠 대화 일러스트
        private const string IllustManimPath = "Assets/Images/manim_illust.png";   // 마님 대화 일러스트

        private static readonly string[] ResourceSpritePaths =
        {
            ResTrayPath, ResRockPath, ResGinsengPath, ResPersimmonPath
        };

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

            EnsureSpriteImports();
            var sprWaterIcon = AssetDatabase.LoadAssetAtPath<Sprite>(ResPersimmonPath); // 곶감
            var sprSyrupIcon = AssetDatabase.LoadAssetAtPath<Sprite>(ResRockPath);       // 돌
            var sprToppingIcon = AssetDatabase.LoadAssetAtPath<Sprite>(ResGinsengPath);  // 인삼

            var gameConfig = LoadOrCreateAsset<GameConfig>(GameConfigPath);
            var water = LoadOrCreateAsset<IngredientSO>(WaterPath);
            var syrup = LoadOrCreateAsset<IngredientSO>(SyrupPath);
            var topping = LoadOrCreateAsset<IngredientSO>(ToppingPath);
            var mixConfig = LoadOrCreateAsset<MixMinigameConfig>(MixMinigameConfigPath);
            var pourConfig = LoadOrCreateAsset<PourMinigameConfig>(PourMinigameConfigPath);

            water.id = "ing_water";
            water.displayName = "삼도천 물";
            water.category = IngredientCategory.Base;
            water.icon = sprWaterIcon;
            EditorUtility.SetDirty(water);

            syrup.id = "ing_syrup";
            syrup.displayName = "시럽";
            syrup.category = IngredientCategory.Syrup;
            syrup.icon = sprSyrupIcon;
            EditorUtility.SetDirty(syrup);

            topping.id = "ing_topping";
            topping.displayName = "고명";
            topping.category = IngredientCategory.Topping;
            topping.icon = sprToppingIcon;
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

            EditorUtility.SetDirty(mixConfig);
            EditorUtility.SetDirty(pourConfig);

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
            var mixMinigameConfig = AssetDatabase.LoadAssetAtPath<MixMinigameConfig>(MixMinigameConfigPath);
            var pourMinigameConfig = AssetDatabase.LoadAssetAtPath<PourMinigameConfig>(PourMinigameConfigPath);

            var sprTray = AssetDatabase.LoadAssetAtPath<Sprite>(ResTrayPath);
            var sprPersimmon = AssetDatabase.LoadAssetAtPath<Sprite>(ResPersimmonPath);
            var sprRock = AssetDatabase.LoadAssetAtPath<Sprite>(ResRockPath);
            var sprGinseng = AssetDatabase.LoadAssetAtPath<Sprite>(ResGinsengPath);

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

            var worldViewRoot = CreateUIObject("WorldViewRoot", canvasGo.transform, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            worldViewRoot.transform.SetAsFirstSibling();

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

            var customerImageGo = CreateUIObject("CustomerImage", worldViewRoot.transform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(380f, 40f), new Vector2(640f, 820f), new Vector2(0.5f, 0f));
            var customerImage = customerImageGo.AddComponent<Image>();
            customerImage.color = new Color(0.75f, 0.85f, 0.95f, 1f);
            customerImage.preserveAspect = true;

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
            var dimGo = CreateUIObject("BackgroundDim", speakerViewGo.transform, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            dimGo.transform.SetAsFirstSibling();
            var backgroundDim = dimGo.AddComponent<Image>();
            backgroundDim.color = new Color(0f, 0f, 0f, 0.35f);
            SetObjectRef(speakerView, "backgroundDim", backgroundDim);
            SetString(speakerView, "leftSpeakerId", "주인공");

            var dialogueRunnerGo = new GameObject("DialogueRunner");
            dialogueRunnerGo.transform.SetParent(canvasGo.transform, false);
            var dialogueRunner = dialogueRunnerGo.AddComponent<DialogueRunner>();
            SetObjectRef(dialogueRunner, "view", dialogueView);
            SetObjectRef(dialogueRunner, "speakerView", speakerView);

            var craftingRoot = CreateUIObject("Crafting", worldViewRoot.transform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(960f, 0f), Vector2.zero, new Vector2(0.5f, 0.5f));

            // 우상단 트레이 (테이블 탑뷰) — Tray.png
            var trayPanel = CreateUIObject("Tray", craftingRoot.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30f, -30f), new Vector2(1140f, 560f), new Vector2(1f, 1f));
            var trayImage = trayPanel.AddComponent<Image>();
            trayImage.sprite = sprTray;
            trayImage.color = Color.white;
            trayImage.preserveAspect = false;
            trayImage.raycastTarget = false;

            var trayController = trayPanel.AddComponent<TrayController>();
            SetObjectRef(trayController, "panel", trayPanel.GetComponent<RectTransform>());
            SetVector2(trayController, "openAnchoredPos", new Vector2(-30f, -30f));
            SetVector2(trayController, "closedAnchoredPos", new Vector2(1200f, -30f));

            // 트레이 위 재료 3종: 곶감(좌상) / 돌(좌하) / 인삼(우)
            var ingredientSOs = new[] { ingWater, ingSyrup, ingTopping };
            var ingredientSprites = new[] { sprPersimmon, sprRock, sprGinseng };
            var ingredientPositions = new[]
            {
                new Vector2(-330f, 70f),
                new Vector2(-330f, -150f),
                new Vector2(380f, -10f)
            };
            var ingredientSizes = new[]
            {
                new Vector2(300f, 240f),
                new Vector2(330f, 220f),
                new Vector2(250f, 330f)
            };

            for (int i = 0; i < 3; i++)
            {
                var ingGo = CreateUIObject("Ingredient_" + i, trayPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), ingredientPositions[i], ingredientSizes[i], new Vector2(0.5f, 0.5f));
                var ingImg = ingGo.AddComponent<Image>();
                ingImg.sprite = ingredientSprites[i];
                ingImg.color = Color.white;
                ingImg.preserveAspect = true;
                var ingCg = ingGo.AddComponent<CanvasGroup>();
                ingCg.blocksRaycasts = true;

                var draggable = ingGo.AddComponent<DraggableIngredient>();
                SetObjectRef(draggable, "ingredient", ingredientSOs[i]);
                SetObjectRef(draggable, "canvas", canvas);
                SetObjectRef(draggable, "iconImage", ingImg);
            }

            // 우하단 컵(사발) — 재료 드롭 타깃 "음료 보이는 곳 (컵)"
            var bowlGo = CreateUIObject("Bowl", craftingRoot.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-30f, 30f), new Vector2(1140f, 470f), new Vector2(1f, 0f));
            var bowlImage = bowlGo.AddComponent<Image>();
            bowlImage.color = new Color(0.30f, 0.55f, 0.95f, 1f);
            var bowlReceiver = bowlGo.AddComponent<BowlReceiver>();
            SetObjectRef(bowlReceiver, "bowlRect", bowlGo.GetComponent<RectTransform>());
            SetObjectRef(bowlReceiver, "uiCamera", null);

            var bowlLabelGo = CreateUIObject("CupLabel", bowlGo.transform, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            var bowlLabel = bowlLabelGo.AddComponent<Text>();
            ApplyDefaultText(bowlLabel, "음료 보이는 곳 (컵)", 40, TextAnchor.MiddleCenter, Color.white);
            bowlLabel.raycastTarget = false;

            // 막자 (섞기 도구) — 컵에 드롭하면 제조 완료
            var pestleGo = CreateUIObject("Pestle", craftingRoot.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(360f, 60f), new Vector2(110f, 260f), new Vector2(0.5f, 0f));
            var pestleImage = pestleGo.AddComponent<Image>();
            pestleImage.color = new Color(0.45f, 0.3f, 0.2f, 1f);
            var pestleMixer = pestleGo.AddComponent<PestleMixer>();
            SetObjectRef(pestleMixer, "bowl", bowlReceiver);
            SetObjectRef(pestleMixer, "bowlRect", bowlGo.GetComponent<RectTransform>());
            SetObjectRef(pestleMixer, "uiCamera", null);
            SetObjectRef(pestleMixer, "pestleRect", pestleGo.GetComponent<RectTransform>());

            var mixRootGo = CreateUIObject("Mix_Root", worldViewRoot.transform, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            var mixRootGroup = mixRootGo.AddComponent<CanvasGroup>();
            mixRootGroup.alpha = 0f;
            mixRootGroup.interactable = false;
            mixRootGroup.blocksRaycasts = false;

            var mixHoldAreaGo = CreateUIObject("Mix_HoldArea", mixRootGo.transform, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            var mixHoldAreaImage = mixHoldAreaGo.AddComponent<Image>();
            mixHoldAreaImage.color = new Color(0f, 0f, 0f, 0f);
            mixHoldAreaImage.raycastTarget = true;
            var mixHoldArea = mixHoldAreaGo.AddComponent<HoldInputArea>();

            var mixTrackGo = CreateUIObject("Mix_Track", mixRootGo.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(860f, 0f), new Vector2(160f, 700f), new Vector2(0.5f, 0.5f));
            var mixTrackImage = mixTrackGo.AddComponent<Image>();
            mixTrackImage.color = new Color(0.2f, 0.2f, 0.2f, 0.75f);

            var mixBarGo = CreateUIObject("Mix_Bar", mixTrackGo.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(120f, 220f), new Vector2(0.5f, 0.5f));
            var mixBarImage = mixBarGo.AddComponent<Image>();
            mixBarImage.color = new Color(0.2f, 0.8f, 0.2f, 0.85f);

            var mixLeafGo = CreateUIObject("Mix_Leaf", mixTrackGo.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(80f, 80f), new Vector2(0.5f, 0.5f));
            var mixLeafImage = mixLeafGo.AddComponent<Image>();
            mixLeafImage.color = new Color(0.95f, 0.95f, 0.2f, 1f);

            var mixProgressGo = CreateUIObject("Mix_ProgressFill", mixRootGo.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(700f, 0f), new Vector2(60f, 700f), new Vector2(0.5f, 0.5f));
            var mixProgressFill = mixProgressGo.AddComponent<Image>();
            mixProgressFill.color = new Color(0.3f, 0.8f, 1f, 1f);
            mixProgressFill.type = Image.Type.Filled;
            mixProgressFill.fillMethod = Image.FillMethod.Vertical;
            mixProgressFill.fillOrigin = (int)Image.OriginVertical.Bottom;
            mixProgressFill.fillAmount = 0.4f;

            var mixMinigame = mixRootGo.AddComponent<MixMinigame>();
            SetObjectRef(mixMinigame, "config", mixMinigameConfig);
            SetObjectRef(mixMinigame, "root", mixRootGroup);
            SetObjectRef(mixMinigame, "trackRect", mixTrackGo.GetComponent<RectTransform>());
            SetObjectRef(mixMinigame, "barRect", mixBarGo.GetComponent<RectTransform>());
            SetObjectRef(mixMinigame, "leafRect", mixLeafGo.GetComponent<RectTransform>());
            SetObjectRef(mixMinigame, "progressFill", mixProgressFill);
            SetObjectRef(mixMinigame, "holdArea", mixHoldArea);

            var pourRootGo = CreateUIObject("Pour_Root", worldViewRoot.transform, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            var pourRootGroup = pourRootGo.AddComponent<CanvasGroup>();
            pourRootGroup.alpha = 0f;
            pourRootGroup.interactable = false;
            pourRootGroup.blocksRaycasts = false;

            var pourHoldAreaGo = CreateUIObject("Pour_HoldArea", pourRootGo.transform, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            var pourHoldAreaImage = pourHoldAreaGo.AddComponent<Image>();
            pourHoldAreaImage.color = new Color(0f, 0f, 0f, 0f);
            pourHoldAreaImage.raycastTarget = true;
            var pourHoldArea = pourHoldAreaGo.AddComponent<HoldInputArea>();

            var pourFillGo = CreateUIObject("Pour_Fill", pourRootGo.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(350f, 160f), new Vector2(240f, 420f), new Vector2(0.5f, 0f));
            var pourFillImage = pourFillGo.AddComponent<Image>();
            pourFillImage.color = new Color(0.25f, 0.6f, 0.95f, 1f);
            pourFillImage.type = Image.Type.Filled;
            pourFillImage.fillMethod = Image.FillMethod.Vertical;
            pourFillImage.fillOrigin = (int)Image.OriginVertical.Bottom;
            pourFillImage.fillAmount = 0f;

            var pourTargetBandGo = CreateUIObject("Pour_TargetBand", pourFillGo.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(220f, 100f), new Vector2(0.5f, 0.5f));
            var pourTargetBandImage = pourTargetBandGo.AddComponent<Image>();
            pourTargetBandImage.color = new Color(1f, 0.85f, 0.2f, 0.45f);

            var pourTeapotGo = CreateUIObject("Pour_Teapot", pourRootGo.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(720f, 510f), new Vector2(180f, 140f), new Vector2(0.5f, 0.5f));
            var pourTeapotImage = pourTeapotGo.AddComponent<Image>();
            pourTeapotImage.color = new Color(0.45f, 0.3f, 0.2f, 1f);

            var pourMinigame = pourRootGo.AddComponent<PourMinigame>();
            SetObjectRef(pourMinigame, "config", pourMinigameConfig);
            SetObjectRef(pourMinigame, "root", pourRootGroup);
            SetObjectRef(pourMinigame, "fillImage", pourFillImage);
            SetObjectRef(pourMinigame, "targetBandRect", pourTargetBandGo.GetComponent<RectTransform>());
            SetObjectRef(pourMinigame, "teapotRect", pourTeapotGo.GetComponent<RectTransform>());
            SetObjectRef(pourMinigame, "holdArea", pourHoldArea);

            var teawareGo = CreateUIObject("Teaware", worldViewRoot.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(1270f, 340f), new Vector2(180f, 140f), new Vector2(0.5f, 0.5f));
            var teawareImage = teawareGo.AddComponent<Image>();
            teawareImage.color = new Color(0.55f, 0.4f, 0.25f, 1f);

            var teawareGuideGo = CreateUIObject("Teaware_Guide", teawareGo.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 70f), new Vector2(220f, 56f), new Vector2(0.5f, 0.5f));
            var teawareGuideText = teawareGuideGo.AddComponent<Text>();
            ApplyDefaultText(teawareGuideText, "다기를 누르세요", 24, TextAnchor.MiddleCenter, Color.white);
            teawareGuideGo.SetActive(false);

            var teawarePour = teawareGo.AddComponent<TeawarePour>();

            var craftingControllerGo = new GameObject("CraftingController");
            craftingControllerGo.transform.SetParent(craftingRoot.transform, false);
            var craftingController = craftingControllerGo.AddComponent<CraftingController>();
            SetObjectRef(craftingController, "tray", trayController);
            SetObjectRef(craftingController, "bowl", bowlReceiver);
            SetObjectRef(craftingController, "pestle", pestleMixer);
            SetObjectRef(craftingController, "mixMinigame", mixMinigame);
            SetObjectRef(craftingController, "pourMinigame", pourMinigame);
            SetObjectRef(craftingController, "teaware", teawarePour);

            SetObjectRef(pestleMixer, "controller", craftingController);
            SetObjectRef(teawarePour, "controller", craftingController);
            SetObjectRef(teawarePour, "guideHint", teawareGuideGo);
            SetObjectRef(teawarePour, "pourAnimator", null);
            SetFloat(teawarePour, "pourDuration", 1.2f);

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

            var drinkPopupRootGo = CreateUIObject("DrinkPopup_Root", canvasGo.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 180f), new Vector2(560f, 300f), new Vector2(0.5f, 0.5f));
            var drinkPopupRoot = drinkPopupRootGo.AddComponent<CanvasGroup>();
            drinkPopupRoot.alpha = 0f;
            drinkPopupRoot.interactable = false;
            drinkPopupRoot.blocksRaycasts = false;

            var drinkPopupImageGo = CreateUIObject("DrinkImage", drinkPopupRootGo.transform, new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.55f), Vector2.zero, new Vector2(200f, 200f), new Vector2(0.5f, 0.5f));
            var drinkPopupImage = drinkPopupImageGo.AddComponent<Image>();
            drinkPopupImage.color = new Color(0.85f, 0.95f, 1f, 1f);

            var drinkPopupSparkleGo = CreateUIObject("Sparkle", drinkPopupRootGo.transform, new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.55f), Vector2.zero, new Vector2(260f, 260f), new Vector2(0.5f, 0.5f));
            var drinkPopupSparkleImage = drinkPopupSparkleGo.AddComponent<Image>();
            drinkPopupSparkleImage.color = new Color(1f, 1f, 0.5f, 0.25f);

            var drinkPopupNameGo = CreateUIObject("NameLabel", drinkPopupRootGo.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(520f, 60f), new Vector2(0.5f, 0.5f));
            var drinkPopupNameLabel = drinkPopupNameGo.AddComponent<Text>();
            ApplyDefaultText(drinkPopupNameLabel, string.Empty, 40, TextAnchor.MiddleCenter, Color.white);

            var drinkPopup = drinkPopupRootGo.AddComponent<DrinkPopup>();
            SetObjectRef(drinkPopup, "root", drinkPopupRoot);
            SetObjectRef(drinkPopup, "drinkImage", drinkPopupImage);
            SetObjectRef(drinkPopup, "sparkle", drinkPopupSparkleGo);
            SetObjectRef(drinkPopup, "nameLabel", drinkPopupNameLabel);

            var serveSequenceGo = new GameObject("ServeSequence", typeof(RectTransform), typeof(Animator), typeof(ServeSequence));
            serveSequenceGo.transform.SetParent(canvasGo.transform, false);
            var serveSequenceAnimator = serveSequenceGo.GetComponent<Animator>();
            var serveSequence = serveSequenceGo.GetComponent<ServeSequence>();
            SetObjectRef(serveSequence, "serveAnimator", serveSequenceAnimator);
            SetString(serveSequence, "offerTrigger", "Offer");
            SetString(serveSequence, "drinkTrigger", "Drink");
            SetFloat(serveSequence, "stepDuration", 1f);

            var coinGainRootGo = CreateUIObject("CoinGain_Root", canvasGo.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 140f), new Vector2(700f, 360f), new Vector2(0.5f, 0.5f));
            var coinGainRoot = coinGainRootGo.AddComponent<CanvasGroup>();
            coinGainRoot.alpha = 0f;
            coinGainRoot.interactable = false;
            coinGainRoot.blocksRaycasts = false;
            var coinGainBg = coinGainRootGo.AddComponent<Image>();
            coinGainBg.color = new Color(0f, 0f, 0f, 0.65f);

            var coinGainImageGo = CreateUIObject("CoinImage", coinGainRootGo.transform, new Vector2(0.5f, 0.7f), new Vector2(0.5f, 0.7f), Vector2.zero, new Vector2(140f, 140f), new Vector2(0.5f, 0.5f));
            var coinGainImage = coinGainImageGo.AddComponent<Image>();
            coinGainImage.color = new Color(1f, 0.9f, 0.25f, 1f);

            var coinGainMessageGo = CreateUIObject("MessageText", coinGainRootGo.transform, new Vector2(0.5f, 0.4f), new Vector2(0.5f, 0.4f), Vector2.zero, new Vector2(620f, 80f), new Vector2(0.5f, 0.5f));
            var coinGainMessageText = coinGainMessageGo.AddComponent<Text>();
            ApplyDefaultText(coinGainMessageText, string.Empty, 34, TextAnchor.MiddleCenter, Color.white);

            var coinGainNextGo = CreateUIObject("NextButton", coinGainRootGo.transform, new Vector2(0.5f, 0.12f), new Vector2(0.5f, 0.12f), Vector2.zero, new Vector2(180f, 64f), new Vector2(0.5f, 0.5f));
            var coinGainNextImage = coinGainNextGo.AddComponent<Image>();
            coinGainNextImage.color = new Color(0.25f, 0.45f, 0.8f, 1f);
            var coinGainNextButton = coinGainNextGo.AddComponent<Button>();
            var coinGainNextTextGo = CreateUIObject("Text", coinGainNextGo.transform, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            var coinGainNextText = coinGainNextTextGo.AddComponent<Text>();
            ApplyDefaultText(coinGainNextText, "다음", 24, TextAnchor.MiddleCenter, Color.white);

            var coinGainScreen = coinGainRootGo.AddComponent<CoinGainScreen>();
            SetObjectRef(coinGainScreen, "root", coinGainRoot);
            SetObjectRef(coinGainScreen, "coinImage", coinGainImage);
            SetObjectRef(coinGainScreen, "messageText", coinGainMessageText);
            SetObjectRef(coinGainScreen, "nextButton", coinGainNextButton);

            var endingCoinRootGo = CreateUIObject("EndingCoin_Root", canvasGo.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 100f), new Vector2(820f, 420f), new Vector2(0.5f, 0.5f));
            var endingCoinRoot = endingCoinRootGo.AddComponent<CanvasGroup>();
            endingCoinRoot.alpha = 0f;
            endingCoinRoot.interactable = false;
            endingCoinRoot.blocksRaycasts = false;
            var endingCoinBg = endingCoinRootGo.AddComponent<Image>();
            endingCoinBg.color = new Color(0f, 0f, 0f, 0.72f);

            var endingCoinSlots = new Image[3];
            var endingGreatBadges = new GameObject[3];
            for (int i = 0; i < 3; i++)
            {
                var slotGo = CreateUIObject("CoinSlot_" + (i + 1), endingCoinRootGo.transform, new Vector2(0.5f, 0.68f), new Vector2(0.5f, 0.68f), new Vector2(-180f + (i * 180f), 0f), new Vector2(120f, 120f), new Vector2(0.5f, 0.5f));
                var slotImage = slotGo.AddComponent<Image>();
                slotImage.color = new Color(1f, 0.9f, 0.25f, 1f);
                endingCoinSlots[i] = slotImage;

                var badgeGo = CreateUIObject("GreatBadge", slotGo.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 30f), new Vector2(100f, 40f), new Vector2(0.5f, 0.5f));
                var badgeImage = badgeGo.AddComponent<Image>();
                badgeImage.color = new Color(1f, 0.25f, 0.25f, 0.9f);
                var badgeText = badgeGo.AddComponent<Text>();
                ApplyDefaultText(badgeText, "GREAT", 20, TextAnchor.MiddleCenter, Color.white);
                badgeGo.SetActive(false);
                endingGreatBadges[i] = badgeGo;
            }

            var endingCoinMessageGo = CreateUIObject("MessageText", endingCoinRootGo.transform, new Vector2(0.5f, 0.36f), new Vector2(0.5f, 0.36f), Vector2.zero, new Vector2(760f, 80f), new Vector2(0.5f, 0.5f));
            var endingCoinMessageText = endingCoinMessageGo.AddComponent<Text>();
            ApplyDefaultText(endingCoinMessageText, string.Empty, 30, TextAnchor.MiddleCenter, Color.white);

            var endingCoinNextGo = CreateUIObject("NextButton", endingCoinRootGo.transform, new Vector2(0.5f, 0.12f), new Vector2(0.5f, 0.12f), Vector2.zero, new Vector2(180f, 64f), new Vector2(0.5f, 0.5f));
            var endingCoinNextImage = endingCoinNextGo.AddComponent<Image>();
            endingCoinNextImage.color = new Color(0.25f, 0.45f, 0.8f, 1f);
            var endingCoinNextButton = endingCoinNextGo.AddComponent<Button>();
            var endingCoinNextTextGo = CreateUIObject("Text", endingCoinNextGo.transform, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            var endingCoinNextText = endingCoinNextTextGo.AddComponent<Text>();
            ApplyDefaultText(endingCoinNextText, "다음", 24, TextAnchor.MiddleCenter, Color.white);

            var endingCoinSummary = endingCoinRootGo.AddComponent<EndingCoinSummary>();
            SetObjectRef(endingCoinSummary, "root", endingCoinRoot);
            SetObjectRefArray(endingCoinSummary, "coinSlots", endingCoinSlots);
            SetObjectRefArray(endingCoinSummary, "greatBadges", new UnityEngine.Object[] { endingGreatBadges[0], endingGreatBadges[1], endingGreatBadges[2] });
            SetObjectRef(endingCoinSummary, "messageText", endingCoinMessageText);
            SetObjectRef(endingCoinSummary, "nextButton", endingCoinNextButton);

            SetObjectRef(craftingController, "drinkPopup", drinkPopup);
            SetObjectRef(craftingController, "serveSequence", serveSequence);

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
            SetObjectRef(screenTransition, "viewRoot", worldViewRoot.GetComponent<RectTransform>());
            SetVector2(screenTransition, "customerViewPos", new Vector2(0f, 0f));
            SetVector2(screenTransition, "craftViewPos", new Vector2(-960f, 0f));
            SetFloat(screenTransition, "viewSwitchDuration", 0.5f);
            transitionRect.anchoredPosition = new Vector2(2200f, 0f);
            worldViewRoot.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 0f);

            SetObjectRef(craftingController, "dualView", screenTransition);

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
            SetObjectRef(dayManager, "coinGainScreen", coinGainScreen);
            SetObjectRef(dayManager, "endingCoinSummary", endingCoinSummary);
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

            // 기존 씬이 있으면 열어서 수동 추가 오브젝트를 보존하고, 없으면 새 씬을 만든다.
            // 빌더가 관리하는 루트만 제거한 뒤 다시 생성하므로 수동 프리팹은 변경되지 않는다.
            Scene scene = OpenOrCreateScene(Stage1ScenePath);
            DestroyManagedRoots(scene, Stage1ManagedRootNames);

            var gameConfig = AssetDatabase.LoadAssetAtPath<GameConfig>(GameConfigPath);

            var sprDolsoeIllust = AssetDatabase.LoadAssetAtPath<Sprite>(IllustDolsoePath);
            var sprManimIllust = AssetDatabase.LoadAssetAtPath<Sprite>(IllustManimPath);

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
            dolsoeCollider.size = Vector2.one;
            var dolsoeInteractable = dolsoeGo.GetComponent<Interactable>();
            SetString(dolsoeInteractable, "displayName", "돌쇠");
            SetDialogueLines(dolsoeInteractable, "dialogue", new[]
            {
                ("돌쇠", "이보게, 삼도천을 건너려는가?"),
                ("주인공", "...네."),
                ("돌쇠", "저 위 다방에 마님을 찾아가 보게.")
            });
            // 돌쇠가 말하는 라인(0, 2)에 돌쇠 일러스트를 표시한다.
            SetDialoguePortrait(dolsoeInteractable, "dialogue", sprDolsoeIllust, 0, 2);
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
            manimCollider.size = Vector2.one;
            var manimInteractable = manimGo.GetComponent<Interactable>();
            SetString(manimInteractable, "displayName", "마님");
            SetDialogueLines(manimInteractable, "dialogue", new[]
            {
                ("마님", "어서 오게. 일손이 필요하던 참이야."),
                ("주인공", "제가 돕겠습니다."),
                ("마님", "좋아, 안으로 들어오게.")
            });
            // 마님이 말하는 라인(0, 2)에 마님 일러스트를 표시한다.
            SetDialoguePortrait(manimInteractable, "dialogue", sprManimIllust, 0, 2);
            var manimHighlightGo = new GameObject("Highlight", typeof(SpriteRenderer));
            manimHighlightGo.transform.SetParent(manimGo.transform, false);
            var manimHighlightSprite = manimHighlightGo.GetComponent<SpriteRenderer>();
            manimHighlightSprite.color = new Color(1f, 1f, 0.35f, 0.65f);
            manimHighlightGo.SetActive(false);
            SetObjectRef(manimInteractable, "highlightVisual", manimHighlightGo);

            // 근접 시 NPC 테두리 발광(아웃라인 셰이더) 연결.
            var outlineMaterial = EnsureOutlineMaterial();
            var normalSpriteMaterial = EnsureNormalSpriteMaterial();
            SetObjectRef(dolsoeInteractable, "outlineTarget", dolsoeSprite);
            SetObjectRef(dolsoeInteractable, "outlineMaterial", outlineMaterial);
            SetObjectRef(dolsoeInteractable, "normalMaterial", normalSpriteMaterial);
            SetObjectRef(manimInteractable, "outlineTarget", manimSprite);
            SetObjectRef(manimInteractable, "outlineMaterial", outlineMaterial);
            SetObjectRef(manimInteractable, "normalMaterial", normalSpriteMaterial);

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
            rightPortrait.preserveAspect = true;
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

            // 대화 UI는 시작 시 꺼져 있어야 한다. 대화 재생 시 런타임에서 다시 켜진다.
            dialogueRoot.SetActive(false);
            speakerViewGo.SetActive(false);

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

        // 씬 파일이 있으면 열고(수동 추가 오브젝트 보존), 없으면 빈 씬을 새로 만든다.
        private static Scene OpenOrCreateScene(string scenePath)
        {
            if (File.Exists(Path.GetFullPath(scenePath)))
            {
                return EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }

            return EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        // 빌더가 관리하는 이름의 루트 오브젝트만 제거한다. 그 외 오브젝트는 보존된다.
        private static void DestroyManagedRoots(Scene scene, string[] managedRootNames)
        {
            if (!scene.IsValid())
            {
                return;
            }

            var nameSet = new HashSet<string>(managedRootNames, StringComparer.Ordinal);
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root != null && nameSet.Contains(root.name))
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }
        }

        private static void EnsureSpriteImports()
        {
            foreach (var path in ResourceSpritePaths)
            {
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                {
                    continue;
                }

                bool changed = false;
                if (importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    changed = true;
                }

                if (importer.spriteImportMode != SpriteImportMode.Single)
                {
                    importer.spriteImportMode = SpriteImportMode.Single;
                    changed = true;
                }

                if (!importer.alphaIsTransparency)
                {
                    importer.alphaIsTransparency = true;
                    changed = true;
                }

                if (changed)
                {
                    importer.SaveAndReimport();
                }
            }
        }

        private static Material EnsureOutlineMaterial()
        {
            var shader = Shader.Find(OutlineShaderName);
            var material = AssetDatabase.LoadAssetAtPath<Material>(OutlineMaterialPath);
            if (material == null)
            {
                if (shader == null)
                {
                    Debug.LogWarning("GemCafeSceneBuilder: '" + OutlineShaderName + "' shader not found; outline material skipped.");
                    return null;
                }

                EnsureFolder(ArtMaterialsDir);
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, OutlineMaterialPath);
            }
            else if (shader != null)
            {
                material.shader = shader;
            }

            material.SetColor("_OutlineColor", new Color(1f, 0.92f, 0.3f, 1f));
            material.SetFloat("_OutlineSize", 2.5f);
            material.SetFloat("_OutlineGlow", 1.9f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material EnsureNormalSpriteMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(NormalMaterialPath);
            if (material == null)
            {
                EnsureFolder(ArtMaterialsDir);
                material = new Material(Shader.Find("Sprites/Default"));
                AssetDatabase.CreateAsset(material, NormalMaterialPath);
                EditorUtility.SetDirty(material);
            }

            return material;
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

        private const string KoreanFontPath = "Assets/_Game/Resources/Fonts/NanumGothic.ttf";

        private static void ApplyDefaultText(Text text, string content, int fontSize, TextAnchor alignment, Color color)
        {
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            // WebGL에서는 OS 폰트에 접근할 수 없어 Arial로는 한글이 깨지므로
            // 임베디드 한글 폰트를 직접 마크한다. 폰트가 없으면 기본 폰트로 대체.
            var koreanFont = AssetDatabase.LoadAssetAtPath<Font>(KoreanFontPath);
            text.font = koreanFont != null ? koreanFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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

        private static void SetDialogueLines(UnityEngine.Object target, string propertyName, (string speakerId, string text, Sprite portrait)[] lines)
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
                el.FindPropertyRelative("portrait").objectReferenceValue = lines[i].portrait;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // 지정한 라인 인덱스의 대화 일러스트(portrait)만 설정한다. 화자 측 텍스트는 건드리지 않는다.
        private static void SetDialoguePortrait(UnityEngine.Object target, string propertyName, Sprite portrait, params int[] lineIndices)
        {
            var so = new SerializedObject(target);
            var property = so.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException("Property not found: " + propertyName + " on " + target.GetType().Name);
            }

            foreach (int idx in lineIndices)
            {
                if (idx < 0 || idx >= property.arraySize)
                {
                    continue;
                }

                property.GetArrayElementAtIndex(idx).FindPropertyRelative("portrait").objectReferenceValue = portrait;
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
