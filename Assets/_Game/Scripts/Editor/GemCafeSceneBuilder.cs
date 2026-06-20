using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using GemCafe.Core;
using GemCafe.Crafting;
using GemCafe.Customer;
using GemCafe.Data;
using GemCafe.Dialogue;
using GemCafe.Ending;
using GemCafe.Player;
using GemCafe.Stage;
using GemCafe.Tutorial;
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
        private const string GinsengPath = "Assets/_Game/Data/Ingredients/ing_ginseng.asset";
        private const string PersimmonPath = "Assets/_Game/Data/Ingredients/ing_persimmon.asset";
        private const string JujubePath = "Assets/_Game/Data/Ingredients/ing_jujube.asset";
        private const string ChrysPath = "Assets/_Game/Data/Ingredients/ing_chrys.asset";
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
        private const string CafeDialogScenePath = "Assets/_Game/Scenes/cafe_dialog.unity";
        private const string CafeTutorialScenePath = "Assets/_Game/Scenes/cafe_tutorial.unity";
        private const string EndingScenePath = "Assets/_Game/Scenes/Ending.unity";
        private const string PrefabsDir = "Assets/_Game/Prefabs";
        private const string DialogueSystemPrefabPath = "Assets/_Game/Prefabs/DialogueSystem.prefab";
        private const string ResourcesRoot = "Assets/_Game/Resources";
        private const string EndingResourcesDir = "Assets/_Game/Resources/Endings";
        private const string EndingCsvAssetPath = "Assets/_Game/Resources/Endings/ending_dialogue.csv";

        private const string ResTrayPath = "Assets/Resource/Tray.png";
        private const string Ing0Path = "Assets/Images/Ingredient/Ingredient_0.png";
        private const string Ing1Path = "Assets/Images/Ingredient/Ingredient_1.png";
        private const string Ing2Path = "Assets/Images/Ingredient/Ingredient_2.png";
        private const string Ing3Path = "Assets/Images/Ingredient/Ingredient_3.png";
        private const string Ing4Path = "Assets/Images/Ingredient/Ingredient_4.png";
        private const string Ing5Path = "Assets/Images/Ingredient/Ingredient_5.png";
        private const string Ing6Path = "Assets/Images/Ingredient/Ingredient_6.png";

        // 코인 슬롯(HUD) 이미지.
        private const string CoinNormalPath = "Assets/Images/Coin/Coin_Normal.png";
        private const string CoinGoldPath = "Assets/Images/Coin/Coin_Gold.png";

        // 일자별 손님 이미지 (CustomerImage에 표시). 이 경로에 PNG를 넣으면 자동으로 적용됩니다.
        private const string CustomersImageDir = "Assets/Images/Customers";
        private const string CustomerPortrait1Path = "Assets/Images/Customers/cst_day1.png";
        private const string CustomerPortrait2Path = "Assets/Images/Customers/cst_day2.png";
        private const string CustomerPortrait3Path = "Assets/Images/Customers/cst_day3.png";

        // 손님 데이터 CSV(런타임 Resources 로드용)와 손님 이미지의 Resources 사본 경로.
        private const string ResourcesRootDir = "Assets/_Game/Resources";
        private const string ResourcesInportCsvDir = "Assets/_Game/Resources/InportCsv";
        private const string ResourcesCustomersDir = "Assets/_Game/Resources/Customers";
        private const string CustomerCsvAssetPath = "Assets/_Game/Resources/InportCsv/CustumersData.csv";
        private const string CustomerCsvResourcePath = "InportCsv/CustumersData";
        private const string MainDialogCsvResourcePath = "Cafe/Main/cafe_MainDialog_Source";
        private const string ResourcesCustomerPortrait1Path = "Assets/_Game/Resources/Customers/cst_day1.png";
        private const string ResourcesCustomerPortrait2Path = "Assets/_Game/Resources/Customers/cst_day2.png";
        private const string ResourcesCustomerPortrait3Path = "Assets/_Game/Resources/Customers/cst_day3.png";

        private static readonly string[] ResourceSpritePaths =
        {
            ResTrayPath, Ing0Path, Ing1Path, Ing2Path, Ing3Path, Ing4Path, Ing5Path, Ing6Path,
            CoinNormalPath, CoinGoldPath,
            CustomerPortrait1Path, CustomerPortrait2Path, CustomerPortrait3Path
        };

        [MenuItem("GemCafe/Build/0. Build All")]
        public static void BuildAll()
        {
            CreateSampleData();
            BuildCafeScene();
            BuildLobbyScene();
            BuildStage1Scene();
            BuildEndingScene();
            BuildDialogueSystemPrefab();
            BuildCafeDialogScene();
            BuildCafeTutorialScene();
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
            EnsureFolder(CustomersImageDir);

            EnsureSpriteImports();
            var sprIng0 = AssetDatabase.LoadAssetAtPath<Sprite>(Ing0Path);
            var sprIng1 = AssetDatabase.LoadAssetAtPath<Sprite>(Ing1Path);
            var sprIng2 = AssetDatabase.LoadAssetAtPath<Sprite>(Ing2Path);
            var sprIng3 = AssetDatabase.LoadAssetAtPath<Sprite>(Ing3Path);
            var sprIng4 = AssetDatabase.LoadAssetAtPath<Sprite>(Ing4Path);
            var sprIng5 = AssetDatabase.LoadAssetAtPath<Sprite>(Ing5Path);
            var sprIng6 = AssetDatabase.LoadAssetAtPath<Sprite>(Ing6Path);
            var sprCstDay1 = AssetDatabase.LoadAssetAtPath<Sprite>(CustomerPortrait1Path);
            var sprCstDay2 = AssetDatabase.LoadAssetAtPath<Sprite>(CustomerPortrait2Path);
            var sprCstDay3 = AssetDatabase.LoadAssetAtPath<Sprite>(CustomerPortrait3Path);

            var gameConfig = LoadOrCreateAsset<GameConfig>(GameConfigPath);
            var water = LoadOrCreateAsset<IngredientSO>(WaterPath);
            var syrup = LoadOrCreateAsset<IngredientSO>(SyrupPath);
            var topping = LoadOrCreateAsset<IngredientSO>(ToppingPath);
            var ginseng = LoadOrCreateAsset<IngredientSO>(GinsengPath);
            var persimmon = LoadOrCreateAsset<IngredientSO>(PersimmonPath);
            var jujube = LoadOrCreateAsset<IngredientSO>(JujubePath);
            var chrys = LoadOrCreateAsset<IngredientSO>(ChrysPath);
            var mixConfig = LoadOrCreateAsset<MixMinigameConfig>(MixMinigameConfigPath);
            var pourConfig = LoadOrCreateAsset<PourMinigameConfig>(PourMinigameConfigPath);

            water.id = "ing_water";
            water.displayName = "곳감";
            water.category = IngredientCategory.Base;
            water.taste = Taste.Umami;
            water.icon = sprIng0;
            EditorUtility.SetDirty(water);

            syrup.id = "ing_syrup";
            syrup.displayName = "도라지";
            syrup.category = IngredientCategory.Syrup;
            syrup.taste = Taste.Sweet;
            syrup.icon = sprIng1;
            EditorUtility.SetDirty(syrup);

            topping.id = "ing_topping";
            topping.displayName = "삼도천물";
            topping.category = IngredientCategory.Topping;
            topping.taste = Taste.Spicy;
            topping.icon = sprIng2;
            EditorUtility.SetDirty(topping);

            ginseng.id = "ing_ginseng";
            ginseng.displayName = "염라수염";
            ginseng.category = IngredientCategory.Topping;
            ginseng.icon = sprIng3;
            EditorUtility.SetDirty(ginseng);

            persimmon.id = "ing_persimmon";
            persimmon.displayName = "처녀귀신";
            persimmon.category = IngredientCategory.Topping;
            persimmon.icon = sprIng4;
            EditorUtility.SetDirty(persimmon);

            jujube.id = "ing_jujube";
            jujube.displayName = "토끼간";
            jujube.category = IngredientCategory.Topping;
            jujube.icon = sprIng5;
            EditorUtility.SetDirty(jujube);

            chrys.id = "ing_chrys";
            chrys.displayName = "담배";
            chrys.category = IngredientCategory.Topping;
            chrys.icon = sprIng6;
            EditorUtility.SetDirty(chrys);

            var rcpDay1 = LoadOrCreateAsset<RecipeSO>(RecipeDay1Path);
            rcpDay1.id = "rcp_day1";
            rcpDay1.drinkName = "1일차 음료";
            rcpDay1.ingredients = new[] { water, syrup, topping };
            rcpDay1.coreTaste = Taste.Sweet;
            rcpDay1.subTastes = new[] { Taste.Umami, Taste.Sweet };
            EditorUtility.SetDirty(rcpDay1);

            var rcpDay2 = LoadOrCreateAsset<RecipeSO>(RecipeDay2Path);
            rcpDay2.id = "rcp_day2";
            rcpDay2.drinkName = "2일차 음료";
            rcpDay2.ingredients = new[] { syrup, ginseng, persimmon };
            rcpDay2.coreTaste = Taste.Sweet;
            rcpDay2.subTastes = new[] { Taste.Sweet, Taste.Spicy };
            EditorUtility.SetDirty(rcpDay2);

            var rcpDay3 = LoadOrCreateAsset<RecipeSO>(RecipeDay3Path);
            rcpDay3.id = "rcp_day3";
            rcpDay3.drinkName = "3일차 음료";
            rcpDay3.ingredients = new[] { topping, jujube, chrys };
            rcpDay3.coreTaste = Taste.Spicy;
            rcpDay3.subTastes = new[] { Taste.Umami, Taste.Spicy };
            EditorUtility.SetDirty(rcpDay3);

            var cstDay1 = LoadOrCreateAsset<CustomerSO>(CustomerDay1Path);
            cstDay1.id = "cst_day1";
            cstDay1.day = 1;
            cstDay1.targetRecipe = rcpDay1;
            cstDay1.portrait = sprCstDay1;
            cstDay1.orderDialogue = new[]
            {
                new DialogueLine
                {
                    speakerId = "손님",
                    text = "곳감에 도라지와 삼도천물을 올려주게.",
                    portrait = null
                }
            };
            EditorUtility.SetDirty(cstDay1);

            var cstDay2 = LoadOrCreateAsset<CustomerSO>(CustomerDay2Path);
            cstDay2.id = "cst_day2";
            cstDay2.day = 2;
            cstDay2.targetRecipe = rcpDay2;
            cstDay2.portrait = sprCstDay2;
            cstDay2.orderDialogue = new[]
            {
                new DialogueLine
                {
                    speakerId = "손님",
                    text = "도라지에 염라수염과 처녀귀신을 넣어 다오.",
                    portrait = null
                }
            };
            EditorUtility.SetDirty(cstDay2);

            var cstDay3 = LoadOrCreateAsset<CustomerSO>(CustomerDay3Path);
            cstDay3.id = "cst_day3";
            cstDay3.day = 3;
            cstDay3.targetRecipe = rcpDay3;
            cstDay3.portrait = sprCstDay3;
            cstDay3.orderDialogue = new[]
            {
                new DialogueLine
                {
                    speakerId = "손님",
                    text = "삼도천물에 토끼간과 담배를 곁들여주게.",
                    portrait = null
                }
            };
            EditorUtility.SetDirty(cstDay3);

            EditorUtility.SetDirty(mixConfig);
            EditorUtility.SetDirty(pourConfig);

            EditorUtility.SetDirty(gameConfig);
            AssetDatabase.SaveAssets();

            EnsureCustomerCsv();
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
            var ingGinseng = AssetDatabase.LoadAssetAtPath<IngredientSO>(GinsengPath);
            var ingPersimmon = AssetDatabase.LoadAssetAtPath<IngredientSO>(PersimmonPath);
            var ingJujube = AssetDatabase.LoadAssetAtPath<IngredientSO>(JujubePath);
            var ingChrys = AssetDatabase.LoadAssetAtPath<IngredientSO>(ChrysPath);
            var cstDay1 = AssetDatabase.LoadAssetAtPath<CustomerSO>(CustomerDay1Path);
            var cstDay2 = AssetDatabase.LoadAssetAtPath<CustomerSO>(CustomerDay2Path);
            var cstDay3 = AssetDatabase.LoadAssetAtPath<CustomerSO>(CustomerDay3Path);
            var mixMinigameConfig = AssetDatabase.LoadAssetAtPath<MixMinigameConfig>(MixMinigameConfigPath);
            var pourMinigameConfig = AssetDatabase.LoadAssetAtPath<PourMinigameConfig>(PourMinigameConfigPath);

            var sprTray = AssetDatabase.LoadAssetAtPath<Sprite>(ResTrayPath);
            var sprIng0 = AssetDatabase.LoadAssetAtPath<Sprite>(Ing0Path);
            var sprIng1 = AssetDatabase.LoadAssetAtPath<Sprite>(Ing1Path);
            var sprIng2 = AssetDatabase.LoadAssetAtPath<Sprite>(Ing2Path);
            var sprIng3 = AssetDatabase.LoadAssetAtPath<Sprite>(Ing3Path);
            var sprIng4 = AssetDatabase.LoadAssetAtPath<Sprite>(Ing4Path);
            var sprIng5 = AssetDatabase.LoadAssetAtPath<Sprite>(Ing5Path);
            var sprIng6 = AssetDatabase.LoadAssetAtPath<Sprite>(Ing6Path);

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
            // ScreenSpace-Camera 모드: ParticleSystem 이펙트(Pour_Effect 등)가 UI 위에 렌더링되도록 한다.
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = mainCam;
            canvas.planeDistance = 100f;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var worldViewRoot = CreateUIObject("WorldViewRoot", canvasGo.transform, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            worldViewRoot.transform.SetAsFirstSibling();

            var hudRoot = CreateUIObject("HUD", canvasGo.transform, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, Vector2.zero);
            var hud = hudRoot.AddComponent<HUD>();

            var coinNormalSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CoinNormalPath);
            var coinGoldSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CoinGoldPath);
            var coinBaseSprite = coinNormalSprite != null
                ? coinNormalSprite
                : AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            var coinSlots = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                var slotGo = CreateUIObject("CoinSlot_" + (i + 1), hudRoot.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(36f + (i * 72f), -24f), new Vector2(60f, 60f), new Vector2(0f, 1f));
                var slotImage = slotGo.AddComponent<Image>();
                slotImage.sprite = coinBaseSprite;
                slotImage.preserveAspect = true;
                slotImage.color = new Color(0.2f, 0.2f, 0.22f, 0.5f);
                coinSlots[i] = slotImage;
            }
            SetObjectRefArray(hud, "coinSlots", coinSlots);
            SetObjectRef(hud, "normalCoinSprite", coinNormalSprite);
            SetObjectRef(hud, "goldCoinSprite", coinGoldSprite);
            SetColor(hud, "normalCoinColor", Color.white);
            SetColor(hud, "goldCoinColor", Color.white);

            var customerImageGo = CreateUIObject("CustomerImage", worldViewRoot.transform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(380f, 40f), new Vector2(640f, 820f), new Vector2(0.5f, 0f));
            var customerImage = customerImageGo.AddComponent<Image>();
            customerImage.color = Color.white;
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

            // 트레이 위 재료 7종 — 손님은 이 중 서로 다른 3종을 요구한다
            var ingredientSOs = new[] { ingWater, ingSyrup, ingTopping, ingGinseng, ingPersimmon, ingJujube, ingChrys };
            var ingredientSprites = new[] { sprIng0, sprIng1, sprIng2, sprIng3, sprIng4, sprIng5, sprIng6 };
            var ingredientPositions = new[]
            {
                new Vector2(-405f, 120f),
                new Vector2(-135f, 120f),
                new Vector2(135f, 120f),
                new Vector2(405f, 120f),
                new Vector2(-270f, -130f),
                new Vector2(0f, -130f),
                new Vector2(270f, -130f)
            };
            var ingredientSize = new Vector2(200f, 200f);

            for (int i = 0; i < ingredientSOs.Length; i++)
            {
                var ingGo = CreateUIObject("Ingredient_" + i, trayPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), ingredientPositions[i], ingredientSize, new Vector2(0.5f, 0.5f));
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

                // 재료 네임태그 범위(영역) + 하단 라벨
                var nameTagAreaGo = CreateUIObject("NameTagArea", ingGo.transform, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
                var nameTagAreaImg = nameTagAreaGo.AddComponent<Image>();
                nameTagAreaImg.color = new Color(0f, 0f, 0f, 0f);
                nameTagAreaImg.raycastTarget = true;
                var nameTag = nameTagAreaGo.AddComponent<IngredientNameTag>();

                var nameTagLabelGo = CreateUIObject("NameTagLabel", ingGo.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, -30f), new Vector2(260f, 56f), new Vector2(0.5f, 1f));
                var nameTagLabel = nameTagLabelGo.AddComponent<Text>();
                ApplyDefaultText(nameTagLabel, string.Empty, 26, TextAnchor.UpperCenter, Color.white);
                nameTagLabel.raycastTarget = false;
                nameTagLabel.enabled = false;

                SetObjectRef(nameTag, "ingredient", draggable);
                SetObjectRef(nameTag, "label", nameTagLabel);
            }

            // 우하단 컵(사발) — 재료 드롭 타깃 "음료 보이는 곳 (컵)"
            var bowlGo = CreateUIObject("Bowl", craftingRoot.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-30f, 30f), new Vector2(1140f, 470f), new Vector2(1f, 0f));
            var bowlImage = bowlGo.AddComponent<Image>();
            bowlImage.color = new Color(0.30f, 0.55f, 0.95f, 1f);
            var bowlReceiver = bowlGo.AddComponent<BowlReceiver>();
            SetObjectRef(bowlReceiver, "bowlRect", bowlGo.GetComponent<RectTransform>());
            SetObjectRef(bowlReceiver, "uiCamera", null);
            var bowlCg = bowlGo.AddComponent<CanvasGroup>();

            // 재료가 순서대로(1,2,3) 놓이는 고정 슬롯 3개
            var bowlSlots = new UnityEngine.Object[3];
            float[] slotX = { -340f, 0f, 340f };
            for (int s = 0; s < 3; s++)
            {
                var slotGo = CreateUIObject(
                    "Slot_" + (s + 1),
                    bowlGo.transform,
                    new Vector2(0.5f, 0.5f),
                    new Vector2(0.5f, 0.5f),
                    new Vector2(slotX[s], 0f),
                    new Vector2(200f, 200f),
                    new Vector2(0.5f, 0.5f));
                bowlSlots[s] = slotGo.GetComponent<RectTransform>();
            }
            SetObjectRefArray(bowlReceiver, "slots", bowlSlots);

            var bowlLabelGo = CreateUIObject("CupLabel", bowlGo.transform, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            var bowlLabel = bowlLabelGo.AddComponent<Text>();
            ApplyDefaultText(bowlLabel, "음료 보이는 곳 (컵)", 40, TextAnchor.MiddleCenter, Color.white);
            bowlLabel.raycastTarget = false;

            // 막자 (섞기 도구) — 컵에 드롭하면 제조 완료
            var pestleGo = CreateUIObject("Pestle", craftingRoot.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(360f, 60f), new Vector2(110f, 260f), new Vector2(0.5f, 0f));
            var pestleImage = pestleGo.AddComponent<Image>();
            pestleImage.color = new Color(0.45f, 0.3f, 0.2f, 1f);
            var pestleCg = pestleGo.AddComponent<CanvasGroup>();
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

            var mixTrackGo = CreateUIObject("Mix_Track", mixRootGo.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(772f, -80f), new Vector2(700f, 160f), new Vector2(0.5f, 0.5f));
            var mixTrackImage = mixTrackGo.AddComponent<Image>();
            mixTrackImage.color = new Color(0.2f, 0.2f, 0.2f, 0.75f);

            var mixBarGo = CreateUIObject("Mix_Bar", mixTrackGo.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(220f, 120f), new Vector2(0.5f, 0.5f));
            var mixBarImage = mixBarGo.AddComponent<Image>();
            mixBarImage.color = new Color(0.2f, 0.8f, 0.2f, 0.85f);

            var mixLeafGo = CreateUIObject("Mix_Leaf", mixTrackGo.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(120f, 0f), new Vector2(80f, 80f), new Vector2(0.5f, 0.5f));
            var mixLeafImage = mixLeafGo.AddComponent<Image>();
            mixLeafImage.color = new Color(0.95f, 0.95f, 0.2f, 1f);

            var filledSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

            var mixProgressGo = CreateUIObject("Mix_ProgressFill", mixRootGo.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(772f, -190f), new Vector2(700f, 60f), new Vector2(0.5f, 0.5f));
            var mixProgressFill = mixProgressGo.AddComponent<Image>();
            mixProgressFill.color = new Color(0.3f, 0.8f, 1f, 1f);
            mixProgressFill.sprite = filledSprite;
            mixProgressFill.type = Image.Type.Filled;
            mixProgressFill.fillMethod = Image.FillMethod.Horizontal;
            mixProgressFill.fillOrigin = (int)Image.OriginHorizontal.Left;
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
            pourFillImage.color = Color.white;
            pourFillImage.raycastTarget = false;
            pourFillImage.type = Image.Type.Simple;
            pourFillImage.preserveAspect = true;

            var pourTargetBandGo = CreateUIObject("Pour_TargetBand", pourFillGo.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(220f, 100f), new Vector2(0.5f, 0.5f));
            var pourTargetBandImage = pourTargetBandGo.AddComponent<Image>();
            pourTargetBandImage.color = new Color(1f, 0.85f, 0.2f, 0.45f);
            pourTargetBandImage.raycastTarget = false;

            // 따르기 완료 이펙트(파티클). 기본은 빈 시스템 → 재생해도 아무것도 안 나온다.
            var pourEffectGo = new GameObject("Pour_Effect");
            pourEffectGo.transform.SetParent(pourFillGo.transform, false);
            var pourEffectPs = pourEffectGo.AddComponent<ParticleSystem>();
            var pourEffectMain = pourEffectPs.main;
            pourEffectMain.playOnAwake = false;
            var pourEffectEmission = pourEffectPs.emission;
            pourEffectEmission.enabled = false;
            // Pour_Effect 오브젝트에 직렬화로 ParticleSystem을 받아 재생하는 컴포넌트.
            var pourEffect = pourEffectGo.AddComponent<PourEffect>();
            SetObjectRef(pourEffect, "effect", pourEffectPs);
            SetObjectRef(pourEffect, "renderCamera", mainCam);

            var pourTeapotGo = CreateUIObject("Pour_Teapot", pourRootGo.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(720f, 510f), new Vector2(180f, 140f), new Vector2(0.5f, 0.5f));
            var pourTeapotImage = pourTeapotGo.AddComponent<Image>();
            pourTeapotImage.color = new Color(0.45f, 0.3f, 0.2f, 1f);
            pourTeapotImage.raycastTarget = true;

            // 따르기 클릭 대상: 주전자(Pour_Teapot). 클릭하면 따르기 연출이 시작된다.
            var teawarePour = pourTeapotGo.AddComponent<TeawarePour>();
            var teawareGuideGo = CreateUIObject("Teaware_Guide", pourTeapotGo.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 40f), new Vector2(300f, 56f), new Vector2(0.5f, 0.5f));
            var teawareGuideText = teawareGuideGo.AddComponent<Text>();
            ApplyDefaultText(teawareGuideText, "주전자를 누르세요", 24, TextAnchor.MiddleCenter, Color.white);
            teawareGuideText.raycastTarget = false;
            teawareGuideGo.SetActive(false);


            var pourMinigame = pourRootGo.AddComponent<PourMinigame>();
            SetObjectRef(pourMinigame, "config", pourMinigameConfig);
            SetObjectRef(pourMinigame, "root", pourRootGroup);
            SetObjectRef(pourMinigame, "fillImage", pourFillImage);
            SetObjectRef(pourMinigame, "targetBandRect", pourTargetBandGo.GetComponent<RectTransform>());
            SetObjectRef(pourMinigame, "teapotRect", pourTeapotGo.GetComponent<RectTransform>());
            SetObjectRef(pourMinigame, "holdArea", pourHoldArea);

            // 스프라이트 교체 방식: Images/Cup/1..10.png 순서대로 할당
            var cupSprites = new UnityEngine.Object[10];
            for (int i = 0; i < 10; i++)
            {
                cupSprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Images/Cup/" + (i + 1) + ".png");
            }
            SetObjectRefArray(pourMinigame, "cupSprites", cupSprites);

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

            // 섞기 미니게임 포커스 연출 (Bowl 확대 + 디밍 + 시작 버튼)
            var mixDimGo = CreateUIObject("MixDimOverlay", canvasGo.transform, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            var mixDimImage = mixDimGo.AddComponent<Image>();
            mixDimImage.color = new Color(0f, 0f, 0f, 1f);
            mixDimImage.raycastTarget = true;
            var mixDimCanvas = mixDimGo.AddComponent<Canvas>();
            mixDimCanvas.overrideSorting = true;
            mixDimCanvas.sortingOrder = 50;
            mixDimGo.AddComponent<GraphicRaycaster>();
            var mixDimCg = mixDimGo.AddComponent<CanvasGroup>();
            mixDimCg.alpha = 0f;
            mixDimCg.interactable = false;
            mixDimCg.blocksRaycasts = false;
            mixDimGo.SetActive(false);

            var mixStartBtnGo = CreateUIObject("Btn_MixStart", bowlGo.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -300f), new Vector2(240f, 84f), new Vector2(0.5f, 0.5f));
            var mixStartBtnImage = mixStartBtnGo.AddComponent<Image>();
            mixStartBtnImage.color = new Color(0.85f, 0.55f, 0.2f, 1f);
            mixStartBtnImage.raycastTarget = true;
            var mixStartButton = mixStartBtnGo.AddComponent<Button>();
            mixStartButton.targetGraphic = mixStartBtnImage;
            var mixStartLabelGo = CreateUIObject("Label", mixStartBtnGo.transform, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            var mixStartLabel = mixStartLabelGo.AddComponent<Text>();
            ApplyDefaultText(mixStartLabel, "시작", 40, TextAnchor.MiddleCenter, Color.white);
            mixStartLabel.raycastTarget = false;
            mixStartBtnGo.SetActive(false);

            var mixFocus = craftingControllerGo.AddComponent<MixFocusController>();
            SetObjectRef(mixFocus, "zoomRoot", worldViewRoot.GetComponent<RectTransform>());
            SetObjectRef(mixFocus, "focusTarget", bowlGo.GetComponent<RectTransform>());
            SetObjectRef(mixFocus, "mixUiGroup", mixRootGroup);
            SetObjectRef(mixFocus, "dimOverlay", mixDimCg);
            SetObjectRef(mixFocus, "startButtonRoot", mixStartBtnGo);
            SetObjectRef(mixFocus, "startButton", mixStartButton);
            SetObjectRef(craftingController, "mixFocus", mixFocus);

            // 따르기 미니게임 포커스 연출 (Pour_Fill 확대 + 디밍 + 시작 버튼 + 완료 이펙트)
            var pourDimGo = CreateUIObject("PourDimOverlay", canvasGo.transform, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            var pourDimImage = pourDimGo.AddComponent<Image>();
            pourDimImage.color = new Color(0f, 0f, 0f, 1f);
            pourDimImage.raycastTarget = true;
            var pourDimCanvas = pourDimGo.AddComponent<Canvas>();
            pourDimCanvas.overrideSorting = true;
            pourDimCanvas.sortingOrder = 50;
            pourDimGo.AddComponent<GraphicRaycaster>();
            var pourDimCg = pourDimGo.AddComponent<CanvasGroup>();
            pourDimCg.alpha = 0f;
            pourDimCg.interactable = false;
            pourDimCg.blocksRaycasts = false;
            pourDimGo.SetActive(false);

            var pourStartBtnGo = CreateUIObject("Btn_PourStart", pourFillGo.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, -30f), new Vector2(240f, 84f), new Vector2(0.5f, 1f));
            var pourStartBtnImage = pourStartBtnGo.AddComponent<Image>();
            pourStartBtnImage.color = new Color(0.85f, 0.55f, 0.2f, 1f);
            pourStartBtnImage.raycastTarget = true;
            var pourStartButton = pourStartBtnGo.AddComponent<Button>();
            pourStartButton.targetGraphic = pourStartBtnImage;
            var pourStartLabelGo = CreateUIObject("Label", pourStartBtnGo.transform, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            var pourStartLabel = pourStartLabelGo.AddComponent<Text>();
            ApplyDefaultText(pourStartLabel, "시작", 40, TextAnchor.MiddleCenter, Color.white);
            pourStartLabel.raycastTarget = false;
            pourStartBtnGo.SetActive(false);

            var pourFocus = craftingControllerGo.AddComponent<PourFocusController>();
            SetObjectRef(pourFocus, "zoomRoot", worldViewRoot.GetComponent<RectTransform>());
            SetObjectRef(pourFocus, "focusTarget", pourFillGo.GetComponent<RectTransform>());
            SetObjectRef(pourFocus, "pourUiGroup", pourRootGroup);
            SetObjectRef(pourFocus, "dimOverlay", pourDimCg);
            SetObjectRef(pourFocus, "startButtonRoot", pourStartBtnGo);
            SetObjectRef(pourFocus, "startButton", pourStartButton);
            SetObjectRef(pourFocus, "pourEffect", pourEffect);
            SetObjectRef(craftingController, "pourFocus", pourFocus);

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

            // 손님 주문 대사 다시보기 — 토글 팝업
            var orderRecallGo = CreateUIObject("Popup_OrderRecall", canvasGo.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(800f, 520f), new Vector2(0.5f, 0.5f));
            var orderRecallCg = orderRecallGo.AddComponent<CanvasGroup>();
            var orderRecallPopup = orderRecallGo.AddComponent<OrderRecallPopup>();

            var orderRecallDimGo = CreateUIObject("Dim", orderRecallGo.transform, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            orderRecallDimGo.transform.SetAsFirstSibling();
            var orderRecallDimImage = orderRecallDimGo.AddComponent<Image>();
            orderRecallDimImage.color = new Color(0f, 0f, 0f, 0.55f);
            orderRecallDimImage.raycastTarget = false;

            var orderRecallPanelGo = CreateUIObject("Panel", orderRecallGo.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(680f, 440f), new Vector2(0.5f, 0.5f));
            var orderRecallPanelImage = orderRecallPanelGo.AddComponent<Image>();
            orderRecallPanelImage.color = new Color(0.18f, 0.16f, 0.22f, 1f);

            var orderRecallTitleGo = CreateUIObject("Title", orderRecallPanelGo.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -20f), new Vector2(-40f, 60f), new Vector2(0.5f, 1f));
            var orderRecallTitle = orderRecallTitleGo.AddComponent<Text>();
            ApplyDefaultText(orderRecallTitle, "대화 다시보기", 32, TextAnchor.MiddleCenter, Color.white);

            // 대화 로그 스크롤뷰 (말풍선이 쌓이는 영역)
            var orderRecallScrollGo = CreateUIObject("ScrollView", orderRecallPanelGo.transform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, -10f), new Vector2(-60f, -200f), new Vector2(0.5f, 0.5f));
            var orderRecallScrollBg = orderRecallScrollGo.AddComponent<Image>();
            orderRecallScrollBg.color = new Color(0.12f, 0.11f, 0.15f, 1f);
            var orderRecallScroll = orderRecallScrollGo.AddComponent<ScrollRect>();
            orderRecallScroll.horizontal = false;
            orderRecallScroll.vertical = true;
            orderRecallScroll.movementType = ScrollRect.MovementType.Clamped;
            orderRecallScroll.scrollSensitivity = 24f;

            var orderRecallViewportGo = CreateUIObject("Viewport", orderRecallScrollGo.transform, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            var orderRecallViewportImage = orderRecallViewportGo.AddComponent<Image>();
            orderRecallViewportImage.color = new Color(1f, 1f, 1f, 0.004f);
            orderRecallViewportGo.AddComponent<RectMask2D>();
            var orderRecallViewportRt = orderRecallViewportGo.GetComponent<RectTransform>();

            var orderRecallContentGo = CreateUIObject("Content", orderRecallViewportGo.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, new Vector2(0f, 0f), new Vector2(0.5f, 1f));
            var orderRecallMessageRoot = orderRecallContentGo.GetComponent<RectTransform>();
            var orderRecallContentLayout = orderRecallContentGo.AddComponent<VerticalLayoutGroup>();
            orderRecallContentLayout.padding = new RectOffset(16, 16, 16, 16);
            orderRecallContentLayout.spacing = 16f;
            orderRecallContentLayout.childAlignment = TextAnchor.UpperLeft;
            orderRecallContentLayout.childControlWidth = true;
            orderRecallContentLayout.childControlHeight = true;
            orderRecallContentLayout.childForceExpandWidth = true;
            orderRecallContentLayout.childForceExpandHeight = false;
            var orderRecallContentFitter = orderRecallContentGo.AddComponent<ContentSizeFitter>();
            orderRecallContentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            orderRecallScroll.viewport = orderRecallViewportRt;
            orderRecallScroll.content = orderRecallMessageRoot;

            var orderRecallCloseGo = CreateUIObject("CloseButton", orderRecallPanelGo.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(180f, 56f), new Vector2(0.5f, 0f));
            var orderRecallCloseImage = orderRecallCloseGo.AddComponent<Image>();
            orderRecallCloseImage.color = new Color(0.8f, 0.2f, 0.2f, 1f);
            var orderRecallCloseButton = orderRecallCloseGo.AddComponent<Button>();
            var orderRecallCloseTextGo = CreateUIObject("Text", orderRecallCloseGo.transform, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            var orderRecallCloseText = orderRecallCloseTextGo.AddComponent<Text>();
            ApplyDefaultText(orderRecallCloseText, "닫기", 24, TextAnchor.MiddleCenter, Color.white);

            // Crafting(제작 공간) 상단 토글 버튼 "UI 대화 다시보기"
            var orderRecallToggleGo = CreateUIObject("Btn_OrderRecall", craftingRoot.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(360f, 64f), new Vector2(0.5f, 1f));
            var orderRecallToggleImage = orderRecallToggleGo.AddComponent<Image>();
            orderRecallToggleImage.color = new Color(0.25f, 0.45f, 0.8f, 1f);
            var orderRecallToggleButton = orderRecallToggleGo.AddComponent<Button>();
            var orderRecallToggleCg = orderRecallToggleGo.AddComponent<CanvasGroup>();
            var orderRecallToggleTextGo = CreateUIObject("Text", orderRecallToggleGo.transform, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            var orderRecallToggleText = orderRecallToggleTextGo.AddComponent<Text>();
            ApplyDefaultText(orderRecallToggleText, "UI 대화 다시보기", 26, TextAnchor.MiddleCenter, Color.white);

            // Crafting(제작 공간) 상단 토글 버튼 "레시피 보기"
            var recipeToggleGo = CreateUIObject("Btn_Recipe", craftingRoot.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -104f), new Vector2(360f, 64f), new Vector2(0.5f, 1f));
            var recipeToggleImage = recipeToggleGo.AddComponent<Image>();
            recipeToggleImage.color = new Color(0.25f, 0.45f, 0.8f, 1f);
            var recipeToggleButton = recipeToggleGo.AddComponent<Button>();
            var recipeToggleCg = recipeToggleGo.AddComponent<CanvasGroup>();
            var recipeToggleTextGo = CreateUIObject("Text", recipeToggleGo.transform, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            var recipeToggleText = recipeToggleTextGo.AddComponent<Text>();
            ApplyDefaultText(recipeToggleText, "레시피 보기", 26, TextAnchor.MiddleCenter, Color.white);

            // Tray가 완전히 도착한 뒤 페이드인되는 대상들 (Tray/Controller 제외)
            SetObjectRefArray(trayController, "revealTargets", new UnityEngine.Object[] { bowlCg, pestleCg, orderRecallToggleCg, recipeToggleCg });

            SetObjectRef(orderRecallPopup, "root", orderRecallCg);
            SetObjectRef(orderRecallPopup, "toggleButton", orderRecallToggleButton);
            SetObjectRef(orderRecallPopup, "closeButton", orderRecallCloseButton);
            SetObjectRef(orderRecallPopup, "dim", orderRecallDimImage);
            SetObjectRef(orderRecallPopup, "messageRoot", orderRecallMessageRoot);
            SetObjectRef(orderRecallPopup, "scrollRect", orderRecallScroll);
            SetObjectRef(orderRecallPopup, "bubbleSprite", AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"));
            orderRecallPopup.Close();

            // 레시피 보기 — 토글 팝업
            var recipeViewGo = CreateUIObject("Popup_RecipeView", canvasGo.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(800f, 520f), new Vector2(0.5f, 0.5f));
            var recipeViewCg = recipeViewGo.AddComponent<CanvasGroup>();
            var recipePopup = recipeViewGo.AddComponent<RecipePopup>();

            var recipeDimGo = CreateUIObject("Dim", recipeViewGo.transform, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            recipeDimGo.transform.SetAsFirstSibling();
            var recipeDimImage = recipeDimGo.AddComponent<Image>();
            recipeDimImage.color = new Color(0f, 0f, 0f, 0.55f);
            recipeDimImage.raycastTarget = false;

            var recipePanelGo = CreateUIObject("Panel", recipeViewGo.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(680f, 440f), new Vector2(0.5f, 0.5f));
            var recipePanelImage = recipePanelGo.AddComponent<Image>();
            recipePanelImage.color = new Color(0.18f, 0.16f, 0.22f, 1f);

            var recipeTitleGo = CreateUIObject("Title", recipePanelGo.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -20f), new Vector2(-40f, 60f), new Vector2(0.5f, 1f));
            var recipeTitle = recipeTitleGo.AddComponent<Text>();
            ApplyDefaultText(recipeTitle, "레시피 보기", 32, TextAnchor.MiddleCenter, Color.white);

            var recipeContentGo = CreateUIObject("Content", recipePanelGo.transform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(-60f, -160f), new Vector2(0.5f, 0.5f));
            var recipeContent = recipeContentGo.AddComponent<Text>();
            ApplyDefaultText(recipeContent, "", 26, TextAnchor.UpperLeft, Color.white);

            var recipeCloseGo = CreateUIObject("CloseButton", recipePanelGo.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(180f, 56f), new Vector2(0.5f, 0f));
            var recipeCloseImage = recipeCloseGo.AddComponent<Image>();
            recipeCloseImage.color = new Color(0.8f, 0.2f, 0.2f, 1f);
            var recipeCloseButton = recipeCloseGo.AddComponent<Button>();
            var recipeCloseTextGo = CreateUIObject("Text", recipeCloseGo.transform, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            var recipeCloseText = recipeCloseTextGo.AddComponent<Text>();
            ApplyDefaultText(recipeCloseText, "닫기", 24, TextAnchor.MiddleCenter, Color.white);

            SetObjectRef(recipePopup, "root", recipeViewCg);
            SetObjectRef(recipePopup, "toggleButton", recipeToggleButton);
            SetObjectRef(recipePopup, "closeButton", recipeCloseButton);
            SetObjectRef(recipePopup, "dim", recipeDimImage);
            SetObjectRef(recipePopup, "contentText", recipeContent);
            recipePopup.Close();

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
                var badgeTextGo = CreateUIObject("Text", badgeGo.transform, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
                var badgeText = badgeTextGo.AddComponent<Text>();
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

            var dayIntroRootGo = CreateUIObject("DayIntro_Root", canvasGo.transform, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            var dayIntroRoot = dayIntroRootGo.AddComponent<CanvasGroup>();
            dayIntroRoot.alpha = 0f;
            dayIntroRoot.interactable = false;
            dayIntroRoot.blocksRaycasts = false;
            var dayIntroTextGo = CreateUIObject("DayText", dayIntroRootGo.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(900f, 240f), new Vector2(0.5f, 0.5f));
            var dayIntroText = dayIntroTextGo.AddComponent<Text>();
            ApplyDefaultText(dayIntroText, "1\uC77C\uCC28", 120, TextAnchor.MiddleCenter, new Color(0.85f, 0.12f, 0.12f, 1f));
            var dayIntro = dayIntroRootGo.AddComponent<DayIntro>();
            SetObjectRef(dayIntro, "root", dayIntroRoot);
            SetObjectRef(dayIntro, "dayText", dayIntroText);
            dayIntroRootGo.transform.SetAsLastSibling();

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

            var dayManagerGo = new GameObject("DayManager", typeof(DayManager));
            var dayManager = dayManagerGo.GetComponent<DayManager>();
            var customerTable = dayManagerGo.AddComponent<CustomerCsvTable>();
            SetString(customerTable, "resourcePath", CustomerCsvResourcePath);
            SetObjectRefArray(customerTable, "ingredientPool", new UnityEngine.Object[]
            {
                ingWater, ingSyrup, ingTopping, ingGinseng, ingPersimmon, ingJujube, ingChrys
            });
            var mainDialogTable = dayManagerGo.AddComponent<CafeMainDialogTable>();
            SetString(mainDialogTable, "resourcePath", MainDialogCsvResourcePath);
            SetObjectRef(dayManager, "spawner", spawner);
            SetObjectRef(dayManager, "dialogue", dialogueRunner);
            SetObjectRef(dayManager, "crafting", craftingController);
            SetObjectRef(dayManager, "resultToast", resultToast);
            SetObjectRef(dayManager, "craftTransition", screenTransition);
            SetObjectRef(dayManager, "coinGainScreen", coinGainScreen);
            SetObjectRef(dayManager, "endingCoinSummary", endingCoinSummary);
            SetObjectRef(dayManager, "dayIntro", dayIntro);
            SetObjectRef(dayManager, "customerTable", customerTable);
            SetObjectRef(dayManager, "mainDialogTable", mainDialogTable);
            SetObjectRef(orderRecallPopup, "dialogTable", mainDialogTable);
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
            dolsoeCollider.size = Vector2.one;
            var dolsoeInteractable = dolsoeGo.GetComponent<Interactable>();
            SetString(dolsoeInteractable, "displayName", "돌쇠");
            SetString(dolsoeInteractable, "dialogueCsvKey", "돌쇠");
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
            SetString(manimInteractable, "dialogueCsvKey", "마님");
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
            var leftPortraitGo = CreateUIObject("LeftPortrait", speakerViewGo.transform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(180f, 260f), new Vector2(220f, 300f), new Vector2(0.5f, 0f));
            var leftPortrait = leftPortraitGo.AddComponent<Image>();
            leftPortrait.color = new Color(0.3f, 0.65f, 1f, 1f);
            var rightPortraitGo = CreateUIObject("RightPortrait", speakerViewGo.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-180f, 260f), new Vector2(220f, 300f), new Vector2(0.5f, 0f));
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

        [MenuItem("GemCafe/Build/6. Build Ending Scene")]
        public static void BuildEndingScene()
        {
            CreateSampleData();
            ImportEndingCsv();
            AssetDatabase.SaveAssets();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var gameConfig = AssetDatabase.LoadAssetAtPath<GameConfig>(GameConfigPath);

            var eventSystemGo = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            var cameraGo = new GameObject("Main Camera", typeof(Camera));
            cameraGo.tag = "MainCamera";
            var mainCam = cameraGo.GetComponent<Camera>();
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = new Color(0.05f, 0.05f, 0.06f, 1f);
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

            // 배경 (전체 화면, 최하단)
            var backgroundGo = CreateUIObject("Background", canvasGo.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            var backgroundImage = backgroundGo.AddComponent<Image>();
            backgroundImage.color = new Color(0.12f, 0.12f, 0.14f, 1f);
            backgroundGo.transform.SetAsFirstSibling();

            // CG (전체 화면, 기본 비활성)
            var cgGo = CreateUIObject("CG", canvasGo.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            var cgImage = cgGo.AddComponent<Image>();
            cgImage.color = new Color(0.18f, 0.16f, 0.2f, 1f);
            cgGo.SetActive(false);

            // 스탠딩 일러스트(SpeakerView)
            var speakerViewGo = new GameObject("SpeakerView", typeof(RectTransform));
            speakerViewGo.transform.SetParent(canvasGo.transform, false);
            var speakerView = speakerViewGo.AddComponent<SpeakerView>();
            var leftPortraitGo = CreateUIObject("LeftPortrait", speakerViewGo.transform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(180f, 260f), new Vector2(220f, 300f), new Vector2(0.5f, 0f));
            var leftPortrait = leftPortraitGo.AddComponent<Image>();
            leftPortrait.color = new Color(0.3f, 0.65f, 1f, 1f);
            leftPortrait.gameObject.SetActive(false);
            var rightPortraitGo = CreateUIObject("RightPortrait", speakerViewGo.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-180f, 260f), new Vector2(220f, 300f), new Vector2(0.5f, 0f));
            var rightPortrait = rightPortraitGo.AddComponent<Image>();
            rightPortrait.color = new Color(1f, 0.65f, 0.2f, 1f);
            rightPortrait.gameObject.SetActive(false);
            var speakerDimGo = CreateUIObject("BackgroundDim", speakerViewGo.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            speakerDimGo.transform.SetAsFirstSibling();
            var speakerDim = speakerDimGo.AddComponent<Image>();
            speakerDim.color = new Color(0f, 0f, 0f, 0f);
            speakerDim.gameObject.SetActive(false);
            SetObjectRef(speakerView, "leftPortrait", leftPortrait);
            SetObjectRef(speakerView, "rightPortrait", rightPortrait);
            SetObjectRef(speakerView, "backgroundDim", speakerDim);
            SetString(speakerView, "leftSpeakerId", "주인공");

            // 대화창(DialogueView)
            var dialogueRoot = CreateUIObject("Dialogue", canvasGo.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(1500f, 250f), new Vector2(0.5f, 0f));
            var dialoguePanelImage = dialogueRoot.AddComponent<Image>();
            dialoguePanelImage.color = new Color(0f, 0f, 0f, 0.7f);
            var dialogueCanvasGroup = dialogueRoot.AddComponent<CanvasGroup>();

            var speakerNameGo = CreateUIObject("SpeakerName", dialogueRoot.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -15f), new Vector2(360f, 40f), new Vector2(0f, 1f));
            var speakerNameText = speakerNameGo.AddComponent<Text>();
            ApplyDefaultText(speakerNameText, "마님", 28, TextAnchor.UpperLeft, Color.white);

            var bodyTextGo = CreateUIObject("Body", dialogueRoot.transform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(20f, 20f), new Vector2(-180f, -80f), new Vector2(0f, 0f));
            var bodyText = bodyTextGo.AddComponent<Text>();
            ApplyDefaultText(bodyText, string.Empty, 30, TextAnchor.UpperLeft, Color.white);

            var nextButtonGo = CreateUIObject("NextButton", dialogueRoot.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-20f, 20f), new Vector2(140f, 54f), new Vector2(1f, 0f));
            var nextButtonImage = nextButtonGo.AddComponent<Image>();
            nextButtonImage.color = new Color(0.25f, 0.45f, 0.8f, 1f);
            var nextButton = nextButtonGo.AddComponent<Button>();
            var nextTextGo = CreateUIObject("Text", nextButtonGo.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            var nextText = nextTextGo.AddComponent<Text>();
            ApplyDefaultText(nextText, "다음", 24, TextAnchor.MiddleCenter, Color.white);

            var dialogueView = dialogueRoot.AddComponent<DialogueView>();
            SetObjectRef(dialogueView, "root", dialogueCanvasGroup);
            SetObjectRef(dialogueView, "speakerNameText", speakerNameText);
            SetObjectRef(dialogueView, "bodyText", bodyText);
            SetObjectRef(dialogueView, "nextButton", nextButton);

            // 화면 효과 오버레이 (전체 화면, 최상단)
            var effectGo = CreateUIObject("EffectOverlay", canvasGo.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            var effectOverlay = effectGo.AddComponent<Image>();
            effectOverlay.color = new Color(0f, 0f, 0f, 0f);
            effectOverlay.raycastTarget = false;
            effectGo.transform.SetAsLastSibling();

            // 엔딩 디렉터
            var directorGo = new GameObject("EndingDirector", typeof(EndingDirector));
            var director = directorGo.GetComponent<EndingDirector>();
            SetObjectRef(director, "backgroundImage", backgroundImage);
            SetObjectRef(director, "cgImage", cgImage);
            SetObjectRef(director, "effectOverlay", effectOverlay);
            SetObjectRef(director, "dialogueView", dialogueView);
            SetObjectRef(director, "speakerView", speakerView);

            EnsureFolder(ScenesDir);
            EditorSceneManager.SaveScene(scene, EndingScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("GemCafeSceneBuilder: Ending scene build complete.");
        }

        [MenuItem("GemCafe/Build/7. Import Ending CSV")]
        public static void ImportEndingCsv()
        {
            var projectRoot = Path.GetDirectoryName(Application.dataPath);
            var src = Path.Combine(projectRoot, "Doc", "엔딩 대사.csv");
            if (!File.Exists(src))
            {
                Debug.LogWarning("GemCafeSceneBuilder.ImportEndingCsv: 원본 CSV를 찾을 수 없습니다: " + src);
                return;
            }

            EnsureFolder(ResourcesRoot);
            EnsureFolder(EndingResourcesDir);

            var destFull = Path.GetFullPath(EndingCsvAssetPath);
            File.Copy(src, destFull, true);
            AssetDatabase.ImportAsset(EndingCsvAssetPath);
            AssetDatabase.Refresh();

            Debug.Log("GemCafeSceneBuilder: Ending CSV import complete -> " + EndingCsvAssetPath);
        }

        [MenuItem("GemCafe/Build/8. Build Dialogue System Prefab")]
        public static GameObject BuildDialogueSystemPrefab()
        {
            EnsureFolder(PrefabsDir);

            // 공용 대화 프리팹: 자체 Canvas + 스탠딩 일러스트(SpeakerView) + 대화창(DialogueView) + DialogueRunner.
            var root = new GameObject("DialogueSystem", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            // 스탠딩 일러스트(SpeakerView) - 대화창 뒤쪽에 표시.
            var speakerViewGo = new GameObject("SpeakerView", typeof(RectTransform));
            speakerViewGo.transform.SetParent(root.transform, false);
            var speakerView = speakerViewGo.AddComponent<SpeakerView>();
            var dimGo = CreateUIObject("BackgroundDim", speakerViewGo.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            var backgroundDim = dimGo.AddComponent<Image>();
            backgroundDim.color = new Color(0f, 0f, 0f, 0.35f);
            var leftPortraitGo = CreateUIObject("LeftPortrait", speakerViewGo.transform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(180f, 260f), new Vector2(220f, 300f), new Vector2(0.5f, 0f));
            var leftPortrait = leftPortraitGo.AddComponent<Image>();
            leftPortrait.color = new Color(0.3f, 0.65f, 1f, 1f);
            leftPortrait.preserveAspect = true;
            var rightPortraitGo = CreateUIObject("RightPortrait", speakerViewGo.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-180f, 260f), new Vector2(220f, 300f), new Vector2(0.5f, 0f));
            var rightPortrait = rightPortraitGo.AddComponent<Image>();
            rightPortrait.color = new Color(1f, 0.65f, 0.2f, 1f);
            rightPortrait.preserveAspect = true;
            SetObjectRef(speakerView, "leftPortrait", leftPortrait);
            SetObjectRef(speakerView, "rightPortrait", rightPortrait);
            SetObjectRef(speakerView, "backgroundDim", backgroundDim);
            SetString(speakerView, "leftSpeakerId", "주인공");

            // 대화창(DialogueView) - 화면 앞쪽에 표시.
            var dialogueRoot = CreateUIObject("Dialogue", root.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(1500f, 250f), new Vector2(0.5f, 0f));
            var dialoguePanelImage = dialogueRoot.AddComponent<Image>();
            dialoguePanelImage.color = new Color(0f, 0f, 0f, 0.7f);
            var dialogueCanvasGroup = dialogueRoot.AddComponent<CanvasGroup>();

            var speakerNameGo = CreateUIObject("SpeakerName", dialogueRoot.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -15f), new Vector2(360f, 40f), new Vector2(0f, 1f));
            var speakerNameText = speakerNameGo.AddComponent<Text>();
            ApplyDefaultText(speakerNameText, string.Empty, 28, TextAnchor.UpperLeft, Color.white);

            var bodyTextGo = CreateUIObject("Body", dialogueRoot.transform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(20f, 20f), new Vector2(-180f, -80f), new Vector2(0f, 0f));
            var bodyText = bodyTextGo.AddComponent<Text>();
            ApplyDefaultText(bodyText, string.Empty, 30, TextAnchor.UpperLeft, Color.white);

            var nextButtonGo = CreateUIObject("NextButton", dialogueRoot.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-20f, 20f), new Vector2(140f, 54f), new Vector2(1f, 0f));
            var nextButtonImage = nextButtonGo.AddComponent<Image>();
            nextButtonImage.color = new Color(0.25f, 0.45f, 0.8f, 1f);
            var nextButton = nextButtonGo.AddComponent<Button>();
            var nextTextGo = CreateUIObject("Text", nextButtonGo.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
            var nextText = nextTextGo.AddComponent<Text>();
            ApplyDefaultText(nextText, "다음", 24, TextAnchor.MiddleCenter, Color.white);

            var dialogueView = dialogueRoot.AddComponent<DialogueView>();
            SetObjectRef(dialogueView, "root", dialogueCanvasGroup);
            SetObjectRef(dialogueView, "speakerNameText", speakerNameText);
            SetObjectRef(dialogueView, "bodyText", bodyText);
            SetObjectRef(dialogueView, "nextButton", nextButton);

            var dialogueRunnerGo = new GameObject("DialogueRunner");
            dialogueRunnerGo.transform.SetParent(root.transform, false);
            var dialogueRunner = dialogueRunnerGo.AddComponent<DialogueRunner>();
            SetObjectRef(dialogueRunner, "view", dialogueView);
            SetObjectRef(dialogueRunner, "speakerView", speakerView);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, DialogueSystemPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("GemCafeSceneBuilder: DialogueSystem prefab build complete -> " + DialogueSystemPrefabPath);
            return prefab;
        }

        [MenuItem("GemCafe/Build/9. Build CafeDialog Scene")]
        public static void BuildCafeDialogScene()
        {
            CreateSampleData();
            AssetDatabase.SaveAssets();

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DialogueSystemPrefabPath);
            if (prefab == null)
            {
                prefab = BuildDialogueSystemPrefab();
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var gameConfig = AssetDatabase.LoadAssetAtPath<GameConfig>(GameConfigPath);

            var eventSystemGo = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            var cameraGo = new GameObject("Main Camera", typeof(Camera));
            cameraGo.tag = "MainCamera";
            var mainCam = cameraGo.GetComponent<Camera>();
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = new Color(0.18f, 0.14f, 0.12f, 1f);
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

            // 공용 대화 프리팹 인스턴스 배치(프리팹 연결 유지).
            var dialogueSystemGo = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            var dialogueRunner = dialogueSystemGo.GetComponentInChildren<DialogueRunner>(true);

            var directorGo = new GameObject("CafeDialogDirector", typeof(CafeDialogDirector));
            var director = directorGo.GetComponent<CafeDialogDirector>();
            SetObjectRef(director, "dialogueRunner", dialogueRunner);
            SetString(director, "dialogueCsvKey", "카페");

            EnsureFolder(ScenesDir);
            EditorSceneManager.SaveScene(scene, CafeDialogScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("GemCafeSceneBuilder: CafeDialog scene build complete.");
        }

        [MenuItem("GemCafe/Build/2b. Build Cafe Tutorial Scene")]
        public static void BuildCafeTutorialScene()
        {
            // 튜토리얼 씬은 의도적으로 거의 비어 둔다: CafeTutorialDirector 하나만 둔다.
            // 이 감독이 실제 Cafe 씬을 Additive 로 띄워 배경/강조 앵커로 사용하고,
            // 카메라/EventSystem/GameManager 는 Cafe 씬이 제공하므로 중복을 피한다.
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var directorGo = new GameObject("CafeTutorialDirector", typeof(CafeTutorialDirector));
            var director = directorGo.GetComponent<CafeTutorialDirector>();
            SetString(director, "cafeSceneName", "Cafe");
            SetString(director, "csvResourcePath", "Cafe/cafe_tutorial");
            SetFloat(director, "dimAlpha", 0.72f);

            EnsureFolder(ScenesDir);
            EditorSceneManager.SaveScene(scene, CafeTutorialScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 새로 생성된 씬의 실제 guid 로 브의 세팅 항목을 정리한다(오래된 guid 제거).
            var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes ?? Array.Empty<EditorBuildSettingsScene>());
            list.RemoveAll(s => string.Equals(s.path, CafeTutorialScenePath, StringComparison.OrdinalIgnoreCase));
            if (File.Exists(Path.GetFullPath(CafeTutorialScenePath)))
            {
                list.Add(new EditorBuildSettingsScene(CafeTutorialScenePath, true));
            }

            EditorBuildSettings.scenes = list.ToArray();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("GemCafeSceneBuilder: CafeTutorial scene build complete.");
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
            AddSceneIfExists(existing, paths, CafeDialogScenePath);
            AddSceneIfExists(existing, paths, CafeTutorialScenePath);
            AddSceneIfExists(existing, paths, CafeScenePath);
            AddSceneIfExists(existing, paths, EndingScenePath);

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

        private static void EnsureCustomerCsv()
        {
            EnsureFolder(ResourcesRootDir);
            EnsureFolder(ResourcesInportCsvDir);
            EnsureFolder(ResourcesCustomersDir);

            // 손님 이미지를 Resources 폴더로 복사(런타임 Resources.Load 가능하도록).
            CopyPortraitToResources(CustomerPortrait1Path, ResourcesCustomerPortrait1Path);
            CopyPortraitToResources(CustomerPortrait2Path, ResourcesCustomerPortrait2Path);
            CopyPortraitToResources(CustomerPortrait3Path, ResourcesCustomerPortrait3Path);

            var sb = new StringBuilder();
            sb.AppendLine("id,day,drinkName,ingredient1,ingredient2,ingredient3,speaker,orderText,imagePath");
            sb.AppendLine("cst_day1,1,1일차 음료,곳감,도라지,,손님,곳감. 도라지.,Customers/cst_day1");
            sb.AppendLine("cst_day2,2,2일차 음료,도라지,염라수염,,손님,도라지에 염라수염을 넣어 다오.,Customers/cst_day2");
            sb.AppendLine("cst_day3,3,3일차 음료,삼도천물,토끼간,,손님,삼도천물에 토끼간을 곁들여주게.,Customers/cst_day3");

            string fullPath = Path.GetFullPath(CustomerCsvAssetPath);
            File.WriteAllText(fullPath, sb.ToString(), new UTF8Encoding(false));
            AssetDatabase.ImportAsset(CustomerCsvAssetPath);
        }

        private static void CopyPortraitToResources(string sourcePath, string destPath)
        {
            if (!File.Exists(Path.GetFullPath(sourcePath)))
            {
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<Sprite>(destPath) != null
                || File.Exists(Path.GetFullPath(destPath)))
            {
                return;
            }

            AssetDatabase.CopyAsset(sourcePath, destPath);
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

        private static void SetColor(UnityEngine.Object target, string propertyName, Color value)
        {
            var so = new SerializedObject(target);
            var property = so.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException("Property not found: " + propertyName + " on " + target.GetType().Name);
            }

            property.colorValue = value;
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
