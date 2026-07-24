using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ToyPuzzle.Editor
{
    public static class ToyPuzzlePresentationBuilder
    {
        public const string ScenePath = "Assets/Game/Scenes/Game.unity";
        public const string UiPrefabPath = "Assets/Game/Prefabs/UI/PF_AppUI.prefab";
        public const string EffectPrefabPath = "Assets/Game/Prefabs/Effects/PF_EffectPool.prefab";
        private const string GeneratedRoot = "Assets/Game/Generated";

        private static Sprite _background;
        private static Sprite _panelDark;
        private static Sprite _popupCream;
        private static Sprite _boardFrame;
        private static Sprite _boardCell;
        private static Sprite _referenceCard;
        private static Sprite _buttonBlue;
        private static Sprite _buttonRed;
        private static Sprite _buttonYellow;
        private static Sprite _buttonGreen;
        private static Sprite _buttonDark;
        private static Font _font;

        [MenuItem("Tools/Toy Puzzle/Generate Complete Game", priority = 100)]
        public static void GenerateCompleteGame()
        {
            ValidateProjectConfiguration();
            ToyArtGenerator.GenerateArtOnly();
            ToyAudioGenerator.GenerateAudioOnly();
            InvokeOptionalMenu("Tools/Toy Puzzle/Generate Levels Only");
            InvokeOptionalMenu("Tools/Toy Puzzle/Generate Thumbnails");
            ToySpriteAtlasBuilder.RebuildAtlases();
            RebuildUiAndScene();
            ConfigureMobileBuild();
            InvokeOptionalMenu("Tools/Toy Puzzle/Validate All Levels");
            AssetDatabase.SaveAssets();
            Debug.Log("Toy Puzzle complete generation finished. Startup scene: " + ScenePath);
        }

        [MenuItem("Tools/Toy Puzzle/Rebuild UI and Scene", priority = 106)]
        public static void RebuildUiAndScene()
        {
            EnsureFolders();
            LoadPresentationAssets();
            BuildEffectPrefab();
            BuildUiPrefab();
            BuildGameScene();
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Tools/Toy Puzzle/Generate Levels Only", priority = 103)]
        public static void GenerateLevelsOnly()
        {
            if(!EditorApplication.ExecuteMenuItem("Tools/Toy Puzzle/Levels/Import JSON Levels")) Debug.LogError("Level import tools are not installed.");
        }

        [MenuItem("Tools/Toy Puzzle/Validate All Levels", priority = 104)]
        public static void ValidateAllLevels()
        {
            bool source=EditorApplication.ExecuteMenuItem("Tools/Toy Puzzle/Levels/Validate JSON Sources");
            bool runtime=EditorApplication.ExecuteMenuItem("Tools/Toy Puzzle/Levels/Validate Runtime Assets");
            bool prefabs=EditorApplication.ExecuteMenuItem("Tools/Toy Puzzle/Levels/Validate Level Prefabs");
            if(!source||!runtime||!prefabs)Debug.LogError("One or more level validation tools are not installed.");
        }

        [MenuItem("Tools/Toy Puzzle/Generate Thumbnails", priority = 105)]
        public static void GenerateThumbnails()
        {
            ToyLevelThumbnailGenerator.GenerateThumbnails();
        }

        [MenuItem("Tools/Toy Puzzle/Open Level Editor", priority = 108)]
        public static void OpenLevelEditor()
        {
            if(!EditorApplication.ExecuteMenuItem("Tools/Toy Puzzle/Levels/Open Level Editor"))Debug.LogError("Level authoring tools are not installed.");
        }

        [MenuItem("Tools/Toy Puzzle/Run Complete Test Suite", priority = 109)]
        public static void RunCompleteTestSuite()
        {
            Type apiType=FindType("UnityEditor.TestTools.TestRunner.TestRunnerApi");
            Type filterType=FindType("UnityEditor.TestTools.TestRunner.Filter");
            Type settingsType=FindType("UnityEditor.TestTools.TestRunner.ExecutionSettings");
            Type modeType=FindType("UnityEditor.TestTools.TestRunner.TestMode");
            if(apiType==null||filterType==null||settingsType==null||modeType==null)
            {
                EditorApplication.ExecuteMenuItem("Window/General/Test Runner");
                Debug.LogError("Unity Test Runner API was unavailable; the Test Runner window was opened for diagnosis.");
                return;
            }
            ScriptableObject api=ScriptableObject.CreateInstance(apiType);
            object filter=Activator.CreateInstance(filterType);
            int edit=Convert.ToInt32(Enum.Parse(modeType,"EditMode"));
            int play=Convert.ToInt32(Enum.Parse(modeType,"PlayMode"));
            FieldInfo modeField=filterType.GetField("testMode",BindingFlags.Public|BindingFlags.Instance);
            PropertyInfo modeProperty=filterType.GetProperty("testMode",BindingFlags.Public|BindingFlags.Instance);
            object combined=Enum.ToObject(modeType,edit|play);
            if(modeField!=null)modeField.SetValue(filter,combined); else if(modeProperty!=null)modeProperty.SetValue(filter,combined);
            Array filters=Array.CreateInstance(filterType,1);
            filters.SetValue(filter,0);
            object settings=Activator.CreateInstance(settingsType,new object[]{filters});
            MethodInfo execute=apiType.GetMethod("Execute",new[]{settingsType});
            if(execute==null)throw new MissingMethodException(apiType.FullName,"Execute");
            execute.Invoke(api,new[]{settings});
            Debug.Log("Toy Puzzle complete EditMode and PlayMode test run started.");
        }

        [MenuItem("Tools/Toy Puzzle/Configure Mobile Build", priority = 107)]
        public static void ConfigureMobileBuild()
        {
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.toypuzzle.shapeblocks");
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, "com.toypuzzle.shapeblocks");
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            Debug.Log("Toy Puzzle configured for portrait Android/iOS builds.");
        }

        [MenuItem("Tools/Toy Puzzle/Clear Generated Presentation", priority = 190)]
        public static void ClearGeneratedPresentation()
        {
            string[] generated =
            {
                "Assets/Game/Art/Generated",
                "Assets/Game/Art/Atlases",
                "Assets/Game/Audio/Generated",
                "Assets/Game/Prefabs/UI",
                "Assets/Game/Prefabs/Effects",
                ScenePath,
                GeneratedRoot
            };
            for (int i = 0; i < generated.Length; i++)
            {
                if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(generated[i]) != null || AssetDatabase.IsValidFolder(generated[i]))
                {
                    AssetDatabase.DeleteAsset(generated[i]);
                }
            }
            AssetDatabase.Refresh();
            Debug.Log("Generated presentation content cleared. Source JSON and authored data were preserved.");
        }

        [MenuItem("Tools/Toy Puzzle/Clear Generated Content", priority = 191)]
        public static void ClearGeneratedContent()
        {
            ClearGeneratedPresentation();
            if(AssetDatabase.IsValidFolder("Assets/Game/Data/Levels/Generated"))AssetDatabase.DeleteAsset("Assets/Game/Data/Levels/Generated");
            AssetDatabase.Refresh();
            Debug.Log("All generated content cleared. Authoritative JSON sources were preserved.");
        }

        private static void ValidateProjectConfiguration()
        {
            string version = Application.unityVersion;
            if (!version.StartsWith("6000.3", StringComparison.Ordinal))
            {
                Debug.LogWarning("Toy Puzzle was authored for Unity 6000.3; current editor is " + version + ".");
            }
            if (GraphicsSettingsProxy.HasActiveRenderPipeline())
            {
                Debug.Log("Toy Puzzle detected an active scriptable render pipeline. UI remains unlit Screen Space Overlay.");
            }
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/Game/Scenes");
            EnsureFolder("Assets/Game/Prefabs");
            EnsureFolder("Assets/Game/Prefabs/UI");
            EnsureFolder("Assets/Game/Prefabs/Effects");
            EnsureFolder(GeneratedRoot);
        }

        private static void LoadPresentationAssets()
        {
            _background = LoadSprite("background");
            _panelDark = LoadSprite("panel_dark");
            _popupCream = LoadSprite("popup_cream");
            _boardFrame = LoadSprite("board_frame");
            _boardCell = LoadSprite("board_cell");
            _referenceCard = LoadSprite("reference_card");
            _buttonBlue = LoadSprite("button_blue");
            _buttonRed = LoadSprite("button_red");
            _buttonYellow = LoadSprite("button_yellow");
            _buttonGreen = LoadSprite("button_green");
            _buttonDark = LoadSprite("button_dark");
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        private static void BuildEffectPrefab()
        {
            GameObject root = NewUiObject("PF_EffectPool");
            Stretch(root.GetComponent<RectTransform>());
            ToyEffectPool pool = root.AddComponent<ToyEffectPool>();
            SerializedObject so = new SerializedObject(pool);
            so.FindProperty("effectRoot").objectReferenceValue = root.GetComponent<RectTransform>();
            SerializedProperty sprites = so.FindProperty("sprites");
            sprites.arraySize = 4;
            SetEffectSprite(sprites.GetArrayElementAtIndex(0), ToyEffectKind.Star, LoadEffect("star"));
            SetEffectSprite(sprites.GetArrayElementAtIndex(1), ToyEffectKind.Sparkle, LoadEffect("sparkle"));
            SetEffectSprite(sprites.GetArrayElementAtIndex(2), ToyEffectKind.Confetti, LoadEffect("confetti"));
            SetEffectSprite(sprites.GetArrayElementAtIndex(3), ToyEffectKind.HighlightRing, LoadEffect("highlight_ring"));
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, EffectPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void SetEffectSprite(SerializedProperty property, ToyEffectKind kind, Sprite sprite)
        {
            property.FindPropertyRelative("kind").enumValueIndex = (int)kind;
            property.FindPropertyRelative("sprite").objectReferenceValue = sprite;
        }

        private static void BuildUiPrefab()
        {
            GameObject root = NewUiObject("PF_AppUI");
            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            root.AddComponent<GraphicRaycaster>();
            root.AddComponent<MobilePresentationSettings>();

            GameObject background = Panel("Background", root.transform, _background, Color.white);
            Stretch(background.GetComponent<RectTransform>());
            background.transform.SetAsFirstSibling();

            GameObject safeArea = NewUiObject("SafeArea", root.transform);
            Stretch(safeArea.GetComponent<RectTransform>());
            safeArea.AddComponent<SafeAreaController>();

            ToyUIController ui = safeArea.AddComponent<ToyUIController>();
            ScreenManager manager = safeArea.AddComponent<ScreenManager>();
            ToyTween tween = safeArea.AddComponent<ToyTween>();
            AudioService audio = safeArea.AddComponent<AudioService>();
            HapticService haptics = safeArea.AddComponent<HapticService>();
            SerializedObject audioSo = new SerializedObject(audio);
            audioSo.FindProperty("library").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ToyAudioLibrary>(ToyAudioGenerator.LibraryPath);
            audioSo.ApplyModifiedPropertiesWithoutUndo();

            Dictionary<string, UnityEngine.Object> refs = new Dictionary<string, UnityEngine.Object>();
            List<ScreenBindingData> screens = new List<ScreenBindingData>();
            BuildHome(safeArea.transform, refs, screens);
            BuildGameplay(safeArea.transform, refs, screens, tween, audio, haptics);
            BuildLevelSelect(safeArea.transform, refs, screens);
            BuildSettings(safeArea.transform, refs, screens);
            BuildPause(safeArea.transform, refs, screens);
            BuildCompletion(safeArea.transform, refs, screens);
            BuildConfirmation(safeArea.transform, refs, screens, false);
            BuildConfirmation(safeArea.transform, refs, screens, true);
            BuildTutorialOverlay(safeArea.transform, refs);

            GameObject effectsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EffectPrefabPath);
            if (effectsPrefab != null)
            {
                GameObject effects = (GameObject)PrefabUtility.InstantiatePrefab(effectsPrefab);
                effects.name = "GlobalEffects";
                effects.transform.SetParent(safeArea.transform, false);
                Stretch(effects.GetComponent<RectTransform>());
                refs["effectPool"] = effects.GetComponent<ToyEffectPool>();
            }

            ConfigureScreenManager(manager, screens);
            refs["screenManager"] = manager;
            refs["tween"] = tween;
            refs["uiController"] = ui;
            refs["audioService"] = audio;
            refs["hapticService"] = haptics;
            AssignObjectReferences(ui, refs);
            GameObject levelInstances = new GameObject("LevelInstances");
            levelInstances.transform.SetParent(root.transform, false);
            refs["levelInstanceRoot"] = levelInstances.transform;
            PuzzleAppController appController = root.AddComponent<PuzzleAppController>();
            refs["levelCatalog"] = AssetDatabase.LoadAssetAtPath<LevelPrefabCatalog>("Assets/Game/Data/Levels/Generated/LevelPrefabCatalog.asset");
            AssignObjectReferences(appController, refs);
            AttachIfAvailable(root, "ToyPuzzle.ServiceContainer");
            PrefabUtility.SaveAsPrefabAsset(root, UiPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void BuildGameScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "Game";
            GameObject cameraObject = new GameObject("UICamera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(16, 92, 142, 255);
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem));
            Type inputModule = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputModule != null) eventSystem.AddComponent(inputModule);
            else eventSystem.AddComponent<StandaloneInputModule>();

            GameObject uiPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(UiPrefabPath);
            if (uiPrefab == null) throw new InvalidOperationException("UI prefab was not generated: " + UiPrefabPath);
            GameObject ui = (GameObject)PrefabUtility.InstantiatePrefab(uiPrefab, scene);
            ui.name = "AppUI";

            AttachIfAvailable(ui, "ToyPuzzle.SaveService");
            AttachIfAvailable(ui, "ToyPuzzle.ProgressionManager");
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private static void BuildHome(Transform parent, Dictionary<string, UnityEngine.Object> refs, List<ScreenBindingData> screens)
        {
            GameObject screen = Screen("HomeScreen", parent, GameScreenId.Home, false, screens);
            Text logo = Label("Logo", screen.transform, UIStrings.GameTitle, 82, TextAnchor.MiddleCenter, new Color32(247,242,226,255));
            SetRect(logo.rectTransform, new Vector2(.08f,.70f), new Vector2(.92f,.88f), Vector2.zero, Vector2.zero);
            GameObject progressCard = Panel("ProgressCard", screen.transform, _panelDark, Color.white);
            AddMoldedShadow(progressCard, 14f, 0.48f);
            SetRect(progressCard.GetComponent<RectTransform>(), new Vector2(.17f,.56f), new Vector2(.83f,.66f), Vector2.zero, Vector2.zero);
            Text homeProgress = LabelStretch("CurrentLevel", progressCard.transform, "LEVEL 1  •  0 / 50 COMPLETE", 34, new Color32(244,240,223,255));

            Button play = CreateButton("PlayButton", screen.transform, _buttonGreen, Icon("play"), UIStrings.Play, new Color32(244,240,223,255));
            SetRect((RectTransform)play.transform, new Vector2(.17f,.38f), new Vector2(.83f,.51f), Vector2.zero, Vector2.zero);
            Button levels = CreateButton("LevelsButton", screen.transform, _buttonBlue, Icon("levels"), UIStrings.Levels, new Color32(244,240,223,255));
            SetRect((RectTransform)levels.transform, new Vector2(.17f,.22f), new Vector2(.49f,.34f), Vector2.zero, Vector2.zero);
            Button settings = CreateButton("SettingsButton", screen.transform, _buttonDark, Icon("settings"), UIStrings.Settings, new Color32(244,240,223,255));
            SetRect((RectTransform)settings.transform, new Vector2(.51f,.22f), new Vector2(.83f,.34f), Vector2.zero, Vector2.zero);
            refs["playButton"] = play; refs["levelsButton"] = levels; refs["settingsButton"] = settings; refs["homeProgressText"] = homeProgress;
        }

        private static void BuildGameplay(Transform parent, Dictionary<string, UnityEngine.Object> refs, List<ScreenBindingData> screens, ToyTween tween, AudioService audio, HapticService haptics)
        {
            GameObject screen = Screen("GameplayScreen", parent, GameScreenId.Gameplay, false, screens);
            ResponsiveGameLayout layout = screen.AddComponent<ResponsiveGameLayout>();

            GameObject top = NewUiObject("TopZone", screen.transform);
            GameObject reference = Panel("ReferenceCard", top.transform, _referenceCard, Color.white);
            AddMoldedShadow(reference, 16f, 0.55f);
            SetRect(reference.GetComponent<RectTransform>(), new Vector2(.22f,.17f), new Vector2(.78f,.96f), Vector2.zero, Vector2.zero);
            GameObject preview = NewUiObject("TargetPreviewRoot", reference.transform);
            Stretch(preview.GetComponent<RectTransform>(), 26f);
            Button home = CreateButton("HomeButton", top.transform, _buttonDark, Icon("home"), string.Empty, Color.white);
            SetRect((RectTransform)home.transform, new Vector2(.03f,.22f), new Vector2(.19f,.78f), Vector2.zero, Vector2.zero);
            Button reset = CreateButton("ResetButton", top.transform, _buttonBlue, Icon("reset"), string.Empty, Color.white);
            SetRect((RectTransform)reset.transform, new Vector2(.81f,.22f), new Vector2(.97f,.78f), Vector2.zero, Vector2.zero);
            Text gameplayStats = Label("GameplayStats", top.transform, "LEVEL 01  |  MOVES 0/6  |  TIME 00:00", 28, TextAnchor.MiddleCenter, new Color32(244,240,223,255));
            SetRect(gameplayStats.rectTransform, new Vector2(.16f,.01f), new Vector2(.84f,.16f), Vector2.zero, Vector2.zero);

            GameObject board = Panel("PuzzleBoard", screen.transform, _boardFrame, Color.white);
            AddMoldedShadow(board, 20f, 0.62f);
            GameObject grid = NewUiObject("GridCellRoot", board.transform);
            Stretch(grid.GetComponent<RectTransform>(), 42f);
            GameObject targetRoot = NewUiObject("TargetHighlightRoot", board.transform); Stretch(targetRoot.GetComponent<RectTransform>(), 42f);
            GameObject pieceRoot = NewUiObject("PieceRoot", board.transform); Stretch(pieceRoot.GetComponent<RectTransform>(), 42f);
            PuzzleBoardView boardView=board.AddComponent<PuzzleBoardView>();
            SerializedObject boardSo=new SerializedObject(boardView);
            boardSo.FindProperty("boardFrame").objectReferenceValue=board.GetComponent<RectTransform>();
            boardSo.FindProperty("cellLayer").objectReferenceValue=grid.GetComponent<RectTransform>();
            boardSo.FindProperty("pieceLayer").objectReferenceValue=pieceRoot.GetComponent<RectTransform>();
            boardSo.FindProperty("referenceLayer").objectReferenceValue=preview.GetComponent<RectTransform>();
            boardSo.FindProperty("palette").objectReferenceValue=AssetDatabase.LoadAssetAtPath<ToyPalette>(ToyArtGenerator.PalettePath);
            boardSo.FindProperty("roundedSprite").objectReferenceValue=AssetDatabase.LoadAssetAtPath<Sprite>(ToyArtGenerator.PieceRoot+"/piece_rounded_neutral.png");
            boardSo.FindProperty("capsuleSprite").objectReferenceValue=AssetDatabase.LoadAssetAtPath<Sprite>(ToyArtGenerator.PieceRoot+"/piece_capsule_neutral.png");
            boardSo.FindProperty("circleSprite").objectReferenceValue=AssetDatabase.LoadAssetAtPath<Sprite>(ToyArtGenerator.PieceRoot+"/piece_circle_neutral.png");
            boardSo.FindProperty("ringSprite").objectReferenceValue=AssetDatabase.LoadAssetAtPath<Sprite>(ToyArtGenerator.PieceRoot+"/piece_ring_neutral.png");
            boardSo.FindProperty("triangleSprite").objectReferenceValue=AssetDatabase.LoadAssetAtPath<Sprite>(ToyArtGenerator.PieceRoot+"/piece_triangle_neutral.png");
            boardSo.FindProperty("trapezoidSprite").objectReferenceValue=AssetDatabase.LoadAssetAtPath<Sprite>(ToyArtGenerator.PieceRoot+"/piece_trapezoid_neutral.png");
            boardSo.FindProperty("wedgeSprite").objectReferenceValue=AssetDatabase.LoadAssetAtPath<Sprite>(ToyArtGenerator.PieceRoot+"/piece_wedge_neutral.png");
            boardSo.FindProperty("semicircleSprite").objectReferenceValue=AssetDatabase.LoadAssetAtPath<Sprite>(ToyArtGenerator.PieceRoot+"/piece_semicircle_neutral.png");
            boardSo.FindProperty("quarterCircleSprite").objectReferenceValue=AssetDatabase.LoadAssetAtPath<Sprite>(ToyArtGenerator.PieceRoot+"/piece_quarter_circle_neutral.png");
            boardSo.FindProperty("studSprite").objectReferenceValue=AssetDatabase.LoadAssetAtPath<Sprite>(ToyArtGenerator.PieceRoot+"/piece_stud_neutral.png");
            boardSo.FindProperty("recessedHoleSprite").objectReferenceValue=AssetDatabase.LoadAssetAtPath<Sprite>(ToyArtGenerator.PieceRoot+"/piece_hole_neutral.png");
            boardSo.FindProperty("insetPanelSprite").objectReferenceValue=AssetDatabase.LoadAssetAtPath<Sprite>(ToyArtGenerator.PieceRoot+"/piece_inset_neutral.png");
            boardSo.ApplyModifiedPropertiesWithoutUndo();
            PuzzleGameController gameController=screen.AddComponent<PuzzleGameController>();
            SerializedObject gameSo=new SerializedObject(gameController);
            gameSo.FindProperty("boardView").objectReferenceValue=boardView;
            gameSo.FindProperty("tween").objectReferenceValue=tween;
            gameSo.FindProperty("audioService").objectReferenceValue=audio;
            gameSo.FindProperty("hapticService").objectReferenceValue=haptics;
            gameSo.ApplyModifiedPropertiesWithoutUndo();
            refs["gameController"]=gameController;

            GameObject controls = NewUiObject("BottomControls", screen.transform);
            HorizontalLayoutGroup row = controls.AddComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset(32,32,18,24); row.spacing = 14f; row.childAlignment = TextAnchor.MiddleCenter;
            row.childControlWidth = false; row.childControlHeight = false; row.childForceExpandWidth = false; row.childForceExpandHeight = false;
            Button undo=CreateButton("UndoButton",controls.transform,_buttonDark,Icon("undo"),string.Empty,Color.white);
            Button hint=CreateButton("HintButton",controls.transform,_buttonBlue,Icon("hint"),"HINT",Color.white);
            Button rotate=CreateButton("RotateButton",controls.transform,_buttonRed,Icon("rotate"),string.Empty,Color.white);
            Button pause=CreateButton("PauseButton",controls.transform,_buttonYellow,Icon("pause"),string.Empty,new Color32(32,36,29,255));
            Button next=CreateButton("NextTestButton",controls.transform,_buttonGreen,null,"NEXT",Color.white);
            SetControlSize(undo, PuzzleLayoutConstants.ReferenceVisibleButtonSize); SetControlSize(hint, PuzzleLayoutConstants.ReferenceVisibleButtonSize); SetControlSize(rotate, PuzzleLayoutConstants.ReferenceVisibleButtonSize); SetControlSize(pause, PuzzleLayoutConstants.ReferenceVisibleButtonSize); SetControlSize(next, PuzzleLayoutConstants.ReferenceVisibleButtonSize);

            SerializedObject layoutSo=new SerializedObject(layout);
            layoutSo.FindProperty("layoutRoot").objectReferenceValue=screen.GetComponent<RectTransform>();
            layoutSo.FindProperty("topZone").objectReferenceValue=top.GetComponent<RectTransform>();
            layoutSo.FindProperty("board").objectReferenceValue=board.GetComponent<RectTransform>();
            layoutSo.FindProperty("bottomControls").objectReferenceValue=controls.GetComponent<RectTransform>();
            layoutSo.ApplyModifiedPropertiesWithoutUndo();
            refs["homeButton"]=home; refs["resetButton"]=reset; refs["undoButton"]=undo; refs["hintButton"]=hint; refs["rotateButton"]=rotate; refs["pauseButton"]=pause; refs["nextTestButton"]=next; refs["gameplayStatsText"]=gameplayStats; refs["hintButtonText"]=hint.transform.Find("Label").GetComponent<Text>();
        }

        private static void BuildTutorialOverlay(Transform parent, Dictionary<string, UnityEngine.Object> refs)
        {
            GameObject overlay = Panel("TutorialOverlay", parent, _panelDark, new Color(1f, 1f, 1f, 0.92f));
            Stretch(overlay.GetComponent<RectTransform>());
            overlay.GetComponent<Image>().raycastTarget = false;
            CanvasGroup group = overlay.AddComponent<CanvasGroup>();

            Text message = Label("TutorialMessage", overlay.transform, UIStrings.DragTutorial, 42, TextAnchor.MiddleCenter, new Color32(244, 240, 223, 255));
            SetRect(message.rectTransform, new Vector2(.12f, .72f), new Vector2(.88f, .84f), Vector2.zero, Vector2.zero);
            GameObject fingerObject = Panel("TutorialFinger", overlay.transform, _buttonYellow, Color.white);
            fingerObject.GetComponent<Image>().raycastTarget = false;
            RectTransform finger = fingerObject.GetComponent<RectTransform>();
            finger.anchorMin = finger.anchorMax = new Vector2(.5f, .5f);
            finger.pivot = new Vector2(.5f, .5f);
            finger.sizeDelta = new Vector2(108f, 108f);
            finger.anchoredPosition = new Vector2(0f, -230f);
            Image fingerIcon = Panel("FingerIcon", fingerObject.transform, Icon("hint"), Color.white).GetComponent<Image>();
            Stretch(fingerIcon.rectTransform, 22f);
            fingerIcon.raycastTarget = false;

            Button skip = CreateButton("TutorialSkipButton", overlay.transform, _buttonDark, null, UIStrings.Skip, Color.white);
            SetRect((RectTransform)skip.transform, new Vector2(.36f, .08f), new Vector2(.64f, .15f), Vector2.zero, Vector2.zero);
            TutorialOverlayController controller = overlay.AddComponent<TutorialOverlayController>();
            SerializedObject so = new SerializedObject(controller);
            so.FindProperty("canvasGroup").objectReferenceValue = group;
            so.FindProperty("message").objectReferenceValue = message;
            so.FindProperty("finger").objectReferenceValue = finger;
            so.FindProperty("skipButton").objectReferenceValue = skip;
            so.ApplyModifiedPropertiesWithoutUndo();
            refs["tutorialOverlay"] = controller;
            overlay.SetActive(false);
        }

        private static void BuildLevelSelect(Transform parent, Dictionary<string, UnityEngine.Object> refs, List<ScreenBindingData> screens)
        {
            GameObject screen=Screen("LevelSelectScreen",parent,GameScreenId.LevelSelect,false,screens);
            Text title=Label("Title",screen.transform,UIStrings.LevelSelect,58,TextAnchor.MiddleCenter,new Color32(244,240,223,255));
            SetRect(title.rectTransform,new Vector2(.18f,.89f),new Vector2(.82f,.98f),Vector2.zero,Vector2.zero);
            Button back=CreateButton("LevelSelectBackButton",screen.transform,_buttonDark,Icon("home"),string.Empty,Color.white);
            SetRect((RectTransform)back.transform,new Vector2(.03f,.89f),new Vector2(.18f,.98f),Vector2.zero,Vector2.zero);
            refs["levelSelectBackButton"]=back;

            GameObject scroll=NewUiObject("LevelScroll",screen.transform); SetRect(scroll.GetComponent<RectTransform>(),new Vector2(.04f,.03f),new Vector2(.96f,.88f),Vector2.zero,Vector2.zero);
            Image scrollMask=scroll.AddComponent<Image>(); scrollMask.color=new Color(0f,0f,0f,.06f); scrollMask.sprite=_panelDark; scrollMask.type=Image.Type.Sliced;
            scroll.AddComponent<Mask>().showMaskGraphic=true;
            ScrollRect scrollRect=scroll.AddComponent<ScrollRect>(); scrollRect.horizontal=false; scrollRect.movementType=ScrollRect.MovementType.Elastic; scrollRect.scrollSensitivity=48f;
            GameObject viewport=NewUiObject("Viewport",scroll.transform); Stretch(viewport.GetComponent<RectTransform>(),16f);
            GameObject content=NewUiObject("Content",viewport.transform);
            RectTransform contentRect=content.GetComponent<RectTransform>(); contentRect.anchorMin=new Vector2(0f,1f); contentRect.anchorMax=Vector2.one; contentRect.pivot=new Vector2(.5f,1f); contentRect.offsetMin=Vector2.zero; contentRect.offsetMax=Vector2.zero;
            GridLayoutGroup grid=content.AddComponent<GridLayoutGroup>(); grid.constraint=GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount=2; grid.cellSize=new Vector2(420f,280f); grid.spacing=new Vector2(32f,34f); grid.padding=new RectOffset(24,24,24,24); grid.childAlignment=TextAnchor.UpperCenter;
            ContentSizeFitter fitter=content.AddComponent<ContentSizeFitter>(); fitter.verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            scrollRect.viewport=viewport.GetComponent<RectTransform>(); scrollRect.content=contentRect;
            LevelSelectView levelSelectView=screen.AddComponent<LevelSelectView>();
            SerializedObject selectSo=new SerializedObject(levelSelectView);
            selectSo.FindProperty("scrollRect").objectReferenceValue=scrollRect;
            selectSo.FindProperty("content").objectReferenceValue=contentRect;
            selectSo.FindProperty("palette").objectReferenceValue=AssetDatabase.LoadAssetAtPath<ToyPalette>(ToyArtGenerator.PalettePath);
            selectSo.FindProperty("buttonSprite").objectReferenceValue=_buttonBlue;
            selectSo.FindProperty("columns").intValue=2;
            SerializedProperty thumbnails=selectSo.FindProperty("thumbnails");
            thumbnails.arraySize=PuzzleLayoutConstants.TotalPlayableLevels;
            for(int i=0;i<PuzzleLayoutConstants.TotalPlayableLevels;i++)thumbnails.GetArrayElementAtIndex(i).objectReferenceValue=AssetDatabase.LoadAssetAtPath<Sprite>(ToyLevelThumbnailGenerator.ThumbnailRoot+"/level_"+(i+1).ToString("000")+".png");
            selectSo.ApplyModifiedPropertiesWithoutUndo();
            refs["levelSelectView"]=levelSelectView;
        }

        private static void CreateLevelEntry(Transform parent,int index)
        {
            GameObject root=Panel("Level_"+(index+1).ToString("000"),parent,index==0?_buttonGreen:_buttonDark,Color.white);
            Button button=root.AddComponent<Button>(); button.targetGraphic=root.GetComponent<Image>();
            root.AddComponent<ToyButtonFeedback>();
            Text number=Label("Number",root.transform,(index+1).ToString(),54,TextAnchor.UpperLeft,new Color32(244,240,223,255)); SetRect(number.rectTransform,new Vector2(.08f,.68f),new Vector2(.38f,.94f),Vector2.zero,Vector2.zero);
            Image thumbnail=Panel("Thumbnail",root.transform,_referenceCard,Color.white).GetComponent<Image>(); SetRect(thumbnail.rectTransform,new Vector2(.18f,.23f),new Vector2(.82f,.72f),Vector2.zero,Vector2.zero);
            Text best=Label("BestMoves",root.transform,index==0?"BEST —":"LOCKED",24,TextAnchor.MiddleCenter,new Color32(244,240,223,255)); SetRect(best.rectTransform,new Vector2(.05f,.04f),new Vector2(.95f,.20f),Vector2.zero,Vector2.zero);
            Image lockIcon=Panel("Lock",root.transform,Icon("lock"),Color.white).GetComponent<Image>(); SetRect(lockIcon.rectTransform,new Vector2(.68f,.68f),new Vector2(.92f,.93f),Vector2.zero,Vector2.zero); lockIcon.gameObject.SetActive(index>0);
            Image check=Panel("Complete",root.transform,Icon("check"),new Color32(89,198,44,255)).GetComponent<Image>(); SetRect(check.rectTransform,new Vector2(.68f,.68f),new Vector2(.92f,.93f),Vector2.zero,Vector2.zero); check.gameObject.SetActive(false);
            LevelEntryView view=root.AddComponent<LevelEntryView>();
            SerializedObject so=new SerializedObject(view);
            so.FindProperty("button").objectReferenceValue=button; so.FindProperty("levelNumber").objectReferenceValue=number; so.FindProperty("bestMoves").objectReferenceValue=best; so.FindProperty("thumbnail").objectReferenceValue=thumbnail; so.FindProperty("lockIcon").objectReferenceValue=lockIcon; so.FindProperty("completedMark").objectReferenceValue=check;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildSettings(Transform parent,Dictionary<string,UnityEngine.Object> refs,List<ScreenBindingData> screens)
        {
            GameObject overlay=Popup("SettingsPopup",parent,GameScreenId.Settings,screens);
            GameObject card=Panel("Card",overlay.transform,_popupCream,Color.white); SetRect(card.GetComponent<RectTransform>(),new Vector2(.12f,.18f),new Vector2(.88f,.82f),Vector2.zero,Vector2.zero);
            AddMoldedShadow(card, 18f, 0.58f);
            Text title=Label("Title",card.transform,UIStrings.Settings,58,TextAnchor.MiddleCenter,new Color32(32,36,29,255));
            SetRect(title.rectTransform,new Vector2(.08f,.79f),new Vector2(.92f,.96f),Vector2.zero,Vector2.zero);
            Toggle music=CreateToggle("MusicToggle",card.transform,"MUSIC",.68f);
            Toggle sound=CreateToggle("SoundToggle",card.transform,"SOUND",.55f);
            Toggle haptics=CreateToggle("HapticsToggle",card.transform,"HAPTICS",.42f);
            Toggle motion=CreateToggle("ReducedMotionToggle",card.transform,"REDUCED MOTION",.29f);
            Button reset=CreateButton("ResetProgressButton",card.transform,_buttonRed,null,"RESET PROGRESS",Color.white); SetRect((RectTransform)reset.transform,new Vector2(.14f,.08f),new Vector2(.68f,.20f),Vector2.zero,Vector2.zero);
            Button close=CreateButton("SettingsCloseButton",card.transform,_buttonDark,Icon("close"),string.Empty,Color.white); SetRect((RectTransform)close.transform,new Vector2(.72f,.08f),new Vector2(.88f,.20f),Vector2.zero,Vector2.zero);
            refs["musicToggle"]=music; refs["soundToggle"]=sound; refs["hapticsToggle"]=haptics; refs["reducedMotionToggle"]=motion; refs["resetProgressButton"]=reset; refs["settingsCloseButton"]=close;
        }

        private static void BuildPause(Transform parent,Dictionary<string,UnityEngine.Object> refs,List<ScreenBindingData> screens)
        {
            GameObject overlay=Popup("PausePopup",parent,GameScreenId.Pause,screens);
            GameObject card=Panel("Card",overlay.transform,_popupCream,Color.white); SetRect(card.GetComponent<RectTransform>(),new Vector2(.16f,.19f),new Vector2(.84f,.81f),Vector2.zero,Vector2.zero);
            AddMoldedShadow(card, 18f, 0.58f);
            Text title=Label("Title",card.transform,"PAUSED",64,TextAnchor.MiddleCenter,new Color32(32,36,29,255));
            SetRect(title.rectTransform,new Vector2(.08f,.79f),new Vector2(.92f,.95f),Vector2.zero,Vector2.zero);
            Button resume=PopupButton(card.transform,"ResumeButton",UIStrings.Resume,_buttonGreen,.61f);
            Button restart=PopupButton(card.transform,"PauseRestartButton",UIStrings.Restart,_buttonRed,.45f);
            Button settings=PopupButton(card.transform,"PauseSettingsButton",UIStrings.Settings,_buttonBlue,.29f);
            Button exit=PopupButton(card.transform,"PauseExitButton",UIStrings.Levels,_buttonDark,.13f);
            refs["resumeButton"]=resume; refs["pauseRestartButton"]=restart; refs["pauseSettingsButton"]=settings; refs["pauseExitButton"]=exit;
        }

        private static void BuildCompletion(Transform parent,Dictionary<string,UnityEngine.Object> refs,List<ScreenBindingData> screens)
        {
            GameObject overlay=Popup("CompletionPopup",parent,GameScreenId.Completion,screens);
            GameObject card=Panel("Card",overlay.transform,_popupCream,Color.white); SetRect(card.GetComponent<RectTransform>(),new Vector2(.08f,.20f),new Vector2(.92f,.80f),Vector2.zero,Vector2.zero);
            AddMoldedShadow(card, 18f, 0.58f);
            Text title=Label("CompletionTitle",card.transform,UIStrings.LevelComplete,60,TextAnchor.MiddleCenter,new Color32(32,36,29,255)); SetRect(title.rectTransform,new Vector2(.06f,.66f),new Vector2(.94f,.94f),Vector2.zero,Vector2.zero);
            Text stats=Label("CompletionStats",card.transform,"MOVES  0   BEST  0\nTIME  00:00   BEST  00:00",32,TextAnchor.MiddleCenter,new Color32(32,36,29,255)); SetRect(stats.rectTransform,new Vector2(.08f,.40f),new Vector2(.92f,.65f),Vector2.zero,Vector2.zero);
            Button replay=PopupButton(card.transform,"CompletionReplayButton",UIStrings.Replay,_buttonBlue,.25f);
            SetRect((RectTransform)replay.transform,new Vector2(.05f,.10f),new Vector2(.34f,.28f),Vector2.zero,Vector2.zero);
            Button next=PopupButton(card.transform,"CompletionNextButton",UIStrings.Next,_buttonGreen,.25f);
            SetRect((RectTransform)next.transform,new Vector2(.36f,.10f),new Vector2(.65f,.28f),Vector2.zero,Vector2.zero);
            Button levels=PopupButton(card.transform,"CompletionLevelsButton",UIStrings.Levels,_buttonDark,.25f);
            SetRect((RectTransform)levels.transform,new Vector2(.67f,.10f),new Vector2(.96f,.28f),Vector2.zero,Vector2.zero);
            refs["completionReplayButton"]=replay; refs["completionNextButton"]=next; refs["completionLevelsButton"]=levels; refs["completionTitle"]=title; refs["completionStats"]=stats;
        }

        private static void BuildConfirmation(Transform parent,Dictionary<string,UnityEngine.Object> refs,List<ScreenBindingData> screens,bool progress)
        {
            GameScreenId id=progress?GameScreenId.ProgressResetConfirmation:GameScreenId.ResetConfirmation;
            string name=progress?"ProgressResetConfirmationPopup":"ResetConfirmationPopup";
            GameObject overlay=Popup(name,parent,id,screens);
            GameObject card=Panel("Card",overlay.transform,_popupCream,Color.white); SetRect(card.GetComponent<RectTransform>(),new Vector2(.14f,.33f),new Vector2(.86f,.67f),Vector2.zero,Vector2.zero);
            AddMoldedShadow(card, 18f, 0.58f);
            Label("Question",card.transform,progress?UIStrings.ProgressResetQuestion:UIStrings.ResetQuestion,44,TextAnchor.MiddleCenter,new Color32(32,36,29,255));
            Button confirm=CreateButton("ConfirmButton",card.transform,_buttonRed,null,UIStrings.Confirm,Color.white); SetRect((RectTransform)confirm.transform,new Vector2(.08f,.12f),new Vector2(.48f,.34f),Vector2.zero,Vector2.zero);
            Button cancel=CreateButton("CancelButton",card.transform,_buttonDark,null,UIStrings.Cancel,Color.white); SetRect((RectTransform)cancel.transform,new Vector2(.52f,.12f),new Vector2(.92f,.34f),Vector2.zero,Vector2.zero);
            refs[progress?"progressResetConfirmButton":"resetConfirmButton"]=confirm; refs[progress?"progressResetCancelButton":"resetCancelButton"]=cancel;
        }

        private static Toggle CreateToggle(string name,Transform parent,string label,float centerY)
        {
            GameObject root=NewUiObject(name,parent); SetRect(root.GetComponent<RectTransform>(),new Vector2(.14f,centerY-.05f),new Vector2(.86f,centerY+.05f),Vector2.zero,Vector2.zero);
            Toggle toggle=root.AddComponent<Toggle>();
            Image background=Panel("Background",root.transform,_buttonDark,Color.white).GetComponent<Image>(); SetRect(background.rectTransform,new Vector2(.72f,.05f),new Vector2(.98f,.95f),Vector2.zero,Vector2.zero);
            Image check=Panel("Checkmark",background.transform,Icon("check"),new Color32(89,198,44,255)).GetComponent<Image>(); Stretch(check.rectTransform,14f);
            Text text=Label("Label",root.transform,label,34,TextAnchor.MiddleLeft,new Color32(32,36,29,255)); SetRect(text.rectTransform,new Vector2(0f,0f),new Vector2(.70f,1f),Vector2.zero,Vector2.zero);
            toggle.targetGraphic=background; toggle.graphic=check; toggle.isOn=true;
            return toggle;
        }

        private static Button PopupButton(Transform parent,string name,string label,Sprite sprite,float centerY)
        {
            Button button=CreateButton(name,parent,sprite,null,label,Color.white);
            SetRect((RectTransform)button.transform,new Vector2(.16f,centerY-.065f),new Vector2(.84f,centerY+.065f),Vector2.zero,Vector2.zero);
            return button;
        }

        private static GameObject Screen(string name,Transform parent,GameScreenId id,bool popup,List<ScreenBindingData> screens)
        {
            GameObject screen=NewUiObject(name,parent); Stretch(screen.GetComponent<RectTransform>());
            CanvasGroup group=screen.AddComponent<CanvasGroup>(); screens.Add(new ScreenBindingData(id,group,popup));
            return screen;
        }

        private static GameObject Popup(string name,Transform parent,GameScreenId id,List<ScreenBindingData> screens)
        {
            GameObject overlay=Screen(name,parent,id,true,screens);
            Image shade=overlay.AddComponent<Image>(); shade.color=new Color(0.02f,0.08f,0.12f,.68f); shade.raycastTarget=true;
            return overlay;
        }

        private static GameObject Panel(string name,Transform parent,Sprite sprite,Color color)
        {
            GameObject go=NewUiObject(name,parent); Image image=go.AddComponent<Image>(); image.sprite=sprite; image.color=color; image.type=sprite!=null&&sprite.border.sqrMagnitude>0f?Image.Type.Sliced:Image.Type.Simple; return go;
        }

        private static Button CreateButton(string name,Transform parent,Sprite sprite,Sprite icon,string label,Color textColor)
        {
            GameObject go=Panel(name,parent,sprite,Color.white); Button button=go.AddComponent<Button>(); button.targetGraphic=go.GetComponent<Image>(); button.transition=Selectable.Transition.ColorTint;
            AddMoldedShadow(go, 11f, 0.55f);
            ColorBlock colors=button.colors; colors.normalColor=Color.white; colors.highlightedColor=new Color(1.06f,1.06f,1.06f,1f); colors.pressedColor=new Color(.82f,.82f,.82f,1f); colors.disabledColor=new Color(.45f,.48f,.48f,.62f); colors.fadeDuration=.08f; button.colors=colors;
            go.AddComponent<ToyButtonFeedback>();
            if(icon!=null){ Image image=Panel("Icon",go.transform,icon,Color.white).GetComponent<Image>(); SetRect(image.rectTransform,label.Length==0?new Vector2(.24f,.24f):new Vector2(.08f,.22f),label.Length==0?new Vector2(.76f,.76f):new Vector2(.34f,.78f),Vector2.zero,Vector2.zero); image.preserveAspect=true; image.raycastTarget=false; Shadow iconShadow=image.gameObject.AddComponent<Shadow>(); iconShadow.effectColor=new Color(0f,0f,0f,.34f); iconShadow.effectDistance=new Vector2(0f,-5f); iconShadow.useGraphicAlpha=true; }
            if(label.Length>0){ Text text=Label("Label",go.transform,label,30,TextAnchor.MiddleCenter,textColor); SetRect(text.rectTransform,icon==null?new Vector2(.06f,.05f):new Vector2(.31f,.05f),new Vector2(.96f,.95f),Vector2.zero,Vector2.zero); text.raycastTarget=false; }
            return button;
        }

        private static void SetControlSize(Button button, float size)
        {
            ((RectTransform)button.transform).sizeDelta = new Vector2(size, size);
            LayoutElement layout = button.gameObject.AddComponent<LayoutElement>();
            layout.minWidth = size;
            layout.preferredWidth = size;
            layout.minHeight = size;
            layout.preferredHeight = size;
        }

        private static void AddMoldedShadow(GameObject target, float distance, float alpha)
        {
            Graphic graphic = target.GetComponent<Graphic>();
            if (graphic == null || target.GetComponent<Shadow>() != null) return;
            Shadow shadow = target.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0.025f, 0.055f, alpha);
            shadow.effectDistance = new Vector2(0f, -distance);
            shadow.useGraphicAlpha = true;
        }

        private static Text Label(string name,Transform parent,string text,int fontSize,TextAnchor alignment,Color color)
        {
            GameObject go=NewUiObject(name,parent); Text label=go.AddComponent<Text>(); label.font=_font; label.text=text; label.fontSize=fontSize; label.fontStyle=FontStyle.Bold; label.alignment=alignment; label.color=color; label.resizeTextForBestFit=true; label.resizeTextMinSize=Mathf.Max(14,fontSize/2); label.resizeTextMaxSize=fontSize; label.raycastTarget=false; Stretch(go.GetComponent<RectTransform>()); return label;
        }

        private static Text LabelStretch(string name,Transform parent,string text,int fontSize,Color color)
        {
            return Label(name,parent,text,fontSize,TextAnchor.MiddleCenter,color);
        }

        private static GameObject NewUiObject(string name,Transform parent=null)
        {
            GameObject go=new GameObject(name,typeof(RectTransform)); if(parent!=null) go.transform.SetParent(parent,false); return go;
        }

        private static void Stretch(RectTransform rect,float inset=0f)
        {
            rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one; rect.pivot=new Vector2(.5f,.5f); rect.offsetMin=new Vector2(inset,inset); rect.offsetMax=new Vector2(-inset,-inset);
        }

        private static void SetRect(RectTransform rect,Vector2 min,Vector2 max,Vector2 offsetMin,Vector2 offsetMax)
        {
            rect.anchorMin=min; rect.anchorMax=max; rect.offsetMin=offsetMin; rect.offsetMax=offsetMax;
        }

        private static void ConfigureScreenManager(ScreenManager manager,List<ScreenBindingData> screens)
        {
            SerializedObject so=new SerializedObject(manager); SerializedProperty array=so.FindProperty("screens"); array.arraySize=screens.Count;
            for(int i=0;i<screens.Count;i++){ SerializedProperty entry=array.GetArrayElementAtIndex(i); entry.FindPropertyRelative("id").enumValueIndex=(int)screens[i].id; entry.FindPropertyRelative("canvasGroup").objectReferenceValue=screens[i].group; entry.FindPropertyRelative("popup").boolValue=screens[i].popup; }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignObjectReferences(UnityEngine.Object target,Dictionary<string,UnityEngine.Object> refs)
        {
            SerializedObject so=new SerializedObject(target);
            foreach(KeyValuePair<string,UnityEngine.Object> pair in refs){ SerializedProperty property=so.FindProperty(pair.Key); if(property!=null) property.objectReferenceValue=pair.Value; }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AttachIfAvailable(GameObject target,string fullTypeName)
        {
            Type type=FindType(fullTypeName); if(type==null||!typeof(Component).IsAssignableFrom(type)||target.GetComponent(type)!=null)return; target.AddComponent(type);
        }

        private static Type FindType(string fullName)
        {
            Assembly[] assemblies=AppDomain.CurrentDomain.GetAssemblies(); for(int i=0;i<assemblies.Length;i++){ Type type=assemblies[i].GetType(fullName,false); if(type!=null)return type; } return null;
        }

        private static void InvokeOptionalMenu(string path)
        {
            if(!EditorApplication.ExecuteMenuItem(path)) Debug.Log("Optional generation step is not installed: "+path);
        }

        private static Sprite LoadSprite(string name){ return AssetDatabase.LoadAssetAtPath<Sprite>(ToyArtGenerator.UiRoot+"/"+name+".png"); }
        private static Sprite Icon(string name){ return LoadSprite("icon_"+name); }
        private static Sprite LoadEffect(string name){ return AssetDatabase.LoadAssetAtPath<Sprite>(ToyArtGenerator.EffectRoot+"/"+name+".png"); }

        private static void EnsureFolder(string path)
        {
            if(AssetDatabase.IsValidFolder(path))return; int split=path.LastIndexOf('/'); string parent=path.Substring(0,split); EnsureFolder(parent); AssetDatabase.CreateFolder(parent,path.Substring(split+1));
        }

        private readonly struct ScreenBindingData
        {
            public readonly GameScreenId id; public readonly CanvasGroup group; public readonly bool popup;
            public ScreenBindingData(GameScreenId id,CanvasGroup group,bool popup){this.id=id;this.group=group;this.popup=popup;}
        }

        private static class GraphicsSettingsProxy
        {
            public static bool HasActiveRenderPipeline(){ return UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline!=null; }
        }

    }
}
