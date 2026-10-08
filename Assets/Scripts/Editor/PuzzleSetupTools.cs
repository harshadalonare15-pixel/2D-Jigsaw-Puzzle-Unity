#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using JigsawPuzzle.Core;
using JigsawPuzzle.Puzzle;
using JigsawPuzzle.UI;
using JigsawPuzzle.Data;

namespace JigsawPuzzle.Editor
{
    /// <summary>
    /// Automated setup tools for the Jigsaw Puzzle project.
    /// Access via Unity menu: Tools > Jigsaw > ...
    /// </summary>
    [InitializeOnLoad]
    public static class PuzzleSetupTools
    {
        static PuzzleSetupTools()
        {
            EditorApplication.delayCall += CheckFirstTimeSetup;
        }

        private static void CheckFirstTimeSetup()
        {
            if (!File.Exists("Assets/Scenes/Game.unity"))
            {
                bool setup = EditorUtility.DisplayDialog(
                    "Welcome to 2D Jigsaw Puzzle!",
                    "Your C# scripts have compiled successfully (0 errors)!\n\n" +
                    "Would you like to automatically create the sample Farm image, levels, and Game scenes right now?",
                    "Yes, Build Everything Now!",
                    "I'll do it later from Tools menu"
                );

                if (setup)
                {
                    QuickSetupEverything();
                }
            }
        }

        [MenuItem("Tools/Jigsaw/1. Quick Setup Everything (One Click)", false, 0)]
        public static void QuickSetupEverything()
        {
            LevelData sampleLevel = CreateSampleFarmAsset();
            BuildGameScene(sampleLevel);
            BuildMainMenuScene();
            BuildLevelSelectScene();
            ConfigureBuildSettings();

            EditorUtility.DisplayDialog(
                "Jigsaw Setup Complete!",
                "Everything has been automatically created and wired up:\n\n" +
                "1. Sample Farm image & LevelData asset created.\n" +
                "2. Game scene built with Camera, Board, HUD, & WinPanel.\n" +
                "3. MainMenu & LevelSelect scenes created.\n" +
                "4. All scenes added to Build Settings.\n\n" +
                "You can now press PLAY in the Game scene to test immediately!",
                "Awesome!"
            );
        }

        [MenuItem("Tools/Jigsaw/2. Create Sample Farm Image & Level Asset", false, 10)]
        public static LevelData CreateSampleFarmAsset()
        {
            EnsureFolderExists("Assets/Sprites");
            EnsureFolderExists("Assets/Levels");

            string texturePath = "Assets/Sprites/FarmSample.png";
            string levelPath = "Assets/Levels/Level_1_Farm.asset";

            // Procedurally paint a colorful cartoon farm texture (512x512)
            int width = 512;
            int height = 512;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);

            Color skyBlue = new Color(0.40f, 0.72f, 0.95f);
            Color grassGreen = new Color(0.38f, 0.78f, 0.28f);
            Color darkGrass = new Color(0.28f, 0.62f, 0.20f);
            Color barnRed = new Color(0.85f, 0.22f, 0.20f);
            Color roofDark = new Color(0.35f, 0.15f, 0.15f);
            Color sunYellow = new Color(1.0f, 0.88f, 0.25f);
            Color fenceWhite = new Color(0.95f, 0.95f, 0.92f);

            for (int y = 0; y < height; y++)
            {
                float ny = (float)y / height;
                for (int x = 0; x < width; x++)
                {
                    float nx = (float)x / width;
                    Color col = skyBlue;

                    // Rolling hill line
                    float hillLine = 0.42f + Mathf.Sin(nx * Mathf.PI * 2f) * 0.08f;

                    if (ny < hillLine)
                    {
                        // Grass
                        col = Color.Lerp(darkGrass, grassGreen, ny / hillLine);
                    }
                    else
                    {
                        // Sun circle in top-left
                        float sunDist = Vector2.Distance(new Vector2(nx, ny), new Vector2(0.22f, 0.80f));
                        if (sunDist < 0.12f)
                        {
                            col = sunYellow;
                        }
                    }

                    // Red Barn (nx in [0.55, 0.82], ny in [0.28, 0.58])
                    if (nx >= 0.55f && nx <= 0.82f && ny >= 0.28f && ny <= 0.55f)
                    {
                        col = barnRed;
                        // White barn door
                        if (nx >= 0.65f && nx <= 0.72f && ny >= 0.28f && ny <= 0.42f)
                        {
                            col = Color.white;
                        }
                    }

                    // Triangular barn roof
                    if (nx >= 0.53f && nx <= 0.84f && ny > 0.55f && ny <= 0.68f)
                    {
                        float roofSlope = 1f - Mathf.Abs((nx - 0.685f) / 0.155f);
                        if ((ny - 0.55f) / 0.13f <= roofSlope)
                        {
                            col = roofDark;
                        }
                    }

                    // White fence posts along foreground
                    if (ny >= 0.14f && ny <= 0.24f)
                    {
                        // Horizontal fence rail
                        if ((ny >= 0.16f && ny <= 0.18f) || (ny >= 0.21f && ny <= 0.23f))
                        {
                            if (nx < 0.52f) col = fenceWhite;
                        }
                        // Vertical fence pickets every 40 pixels
                        if ((x % 40) < 8 && nx < 0.52f)
                        {
                            col = fenceWhite;
                        }
                    }

                    tex.SetPixel(x, y, col);
                }
            }

            tex.Apply();
            byte[] pngBytes = tex.EncodeToPNG();
            File.WriteAllBytes(texturePath, pngBytes);
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceUpdate);

            // Configure Texture Importer for Sprite + Read/Write Enabled
            TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.isReadable = true; // Essential for pixel slicing!
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.filterMode = FilterMode.Bilinear;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(texturePath);

            // Create LevelData ScriptableObject
            LevelData level = AssetDatabase.LoadAssetAtPath<LevelData>(levelPath);
            if (level == null)
            {
                level = ScriptableObject.CreateInstance<LevelData>();
                AssetDatabase.CreateAsset(level, levelPath);
            }

            level.levelName = "Farm Sunshine";
            level.puzzleSprite = sprite;
            level.rows = 3;
            level.columns = 3;
            level.snapTolerance = 0.28f;
            level.silhouetteColor = new Color(0.56f, 0.83f, 0.93f, 0.65f); // #8FD3EC

            EditorUtility.SetDirty(level);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[PuzzleSetupTools] Created Farm image and LevelData asset successfully at: " + levelPath);
            return level;
        }

        [MenuItem("Tools/Jigsaw/3. Build Game Scene", false, 11)]
        public static void BuildGameSceneMenu()
        {
            LevelData sample = AssetDatabase.LoadAssetAtPath<LevelData>("Assets/Levels/Level_1_Farm.asset");
            if (sample == null) sample = CreateSampleFarmAsset();
            BuildGameScene(sample);
        }

        public static void BuildGameScene(LevelData levelData)
        {
            EnsureFolderExists("Assets/Scenes");
            string scenePath = "Assets/Scenes/Game.unity";

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Camera
            GameObject camObj = new GameObject("Main Camera");
            Camera cam = camObj.AddComponent<Camera>();
            camObj.tag = "MainCamera";
            cam.orthographic = true;
            cam.orthographicSize = 6f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.18f, 0.22f, 0.28f); // Soft dark slate
            camObj.transform.position = new Vector3(0, 0, -10f);
            camObj.AddComponent<AudioListener>();

            // 2. Event System
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            // 3. Audio Manager
            GameObject audioObj = new GameObject("AudioManager");
            AudioManager audioMgr = audioObj.AddComponent<AudioManager>();

            // 4. Level Manager
            GameObject levelMgrObj = new GameObject("LevelManager");
            LevelManager levelMgr = levelMgrObj.AddComponent<LevelManager>();
            if (levelData != null)
            {
                levelMgr.levels.Add(levelData);
            }

            // 5. Canvas
            GameObject canvasObj = new GameObject("Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();

            // 6. HUD
            GameObject hudObj = CreateUIElement("HUD", canvasObj.transform);
            RectTransform hudRect = hudObj.GetComponent<RectTransform>();
            hudRect.anchorMin = Vector2.zero;
            hudRect.anchorMax = Vector2.one;
            hudRect.offsetMin = Vector2.zero;
            hudRect.offsetMax = Vector2.zero;
            HUDController hudCtrl = hudObj.AddComponent<HUDController>();

            // Top Bar Background
            GameObject topBar = CreateUIElement("TopBar", hudObj.transform);
            RectTransform topBarRect = topBar.GetComponent<RectTransform>();
            topBarRect.anchorMin = new Vector2(0, 1);
            topBarRect.anchorMax = new Vector2(1, 1);
            topBarRect.pivot = new Vector2(0.5f, 1);
            topBarRect.sizeDelta = new Vector2(0, 90);
            Image topBarImg = topBar.AddComponent<Image>();
            topBarImg.color = new Color(0.10f, 0.12f, 0.16f, 0.85f);

            // Pieces Counter Text
            GameObject counterObj = CreateTextElement("CounterText", topBar.transform, "Pieces: 0 / 9", 28, TextAlignmentOptions.MidlineLeft);
            RectTransform counterRect = counterObj.GetComponent<RectTransform>();
            counterRect.anchorMin = new Vector2(0, 0.5f);
            counterRect.anchorMax = new Vector2(0, 0.5f);
            counterRect.pivot = new Vector2(0, 0.5f);
            counterRect.anchoredPosition = new Vector2(30, 0);
            counterRect.sizeDelta = new Vector2(260, 50);
            hudCtrl.counterText = counterObj.GetComponent<TextMeshProUGUI>();

            // Timer Text
            GameObject timerObj = CreateTextElement("TimerText", topBar.transform, "Time: 00:00", 28, TextAlignmentOptions.MidlineLeft);
            RectTransform timerRect = timerObj.GetComponent<RectTransform>();
            timerRect.anchorMin = new Vector2(0, 0.5f);
            timerRect.anchorMax = new Vector2(0, 0.5f);
            timerRect.pivot = new Vector2(0, 0.5f);
            timerRect.anchoredPosition = new Vector2(320, 0);
            timerRect.sizeDelta = new Vector2(220, 50);
            hudCtrl.timerText = timerObj.GetComponent<TextMeshProUGUI>();

            // Level Title Text
            GameObject titleObj = CreateTextElement("TitleText", topBar.transform, "Farm Sunshine", 32, TextAlignmentOptions.Center);
            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.5f);
            titleRect.anchorMax = new Vector2(0.5f, 0.5f);
            titleRect.pivot = new Vector2(0.5f, 0.5f);
            titleRect.sizeDelta = new Vector2(400, 50);
            hudCtrl.levelTitleText = titleObj.GetComponent<TextMeshProUGUI>();

            // SOLVE Button (Vibrant Green)
            GameObject solveBtnObj = CreateButtonElement("SolveButton", topBar.transform, "SOLVE", new Color(0.20f, 0.70f, 0.30f), new Vector2(-280, 0), new Vector2(120, 55));
            hudCtrl.solveButton = solveBtnObj.GetComponent<Button>();

            // Preview Button
            GameObject previewBtnObj = CreateButtonElement("PreviewButton", topBar.transform, "Preview", new Color(0.25f, 0.55f, 0.85f), new Vector2(-140, 0), new Vector2(130, 55));
            hudCtrl.previewButton = previewBtnObj.GetComponent<Button>();

            // Restart Button
            GameObject restartBtnObj = CreateButtonElement("RestartButton", topBar.transform, "Restart", new Color(0.85f, 0.40f, 0.20f), new Vector2(-30, 0), new Vector2(110, 55));
            hudCtrl.restartButton = restartBtnObj.GetComponent<Button>();

            // Preview Dialog Panel
            GameObject previewPanelObj = CreateUIElement("PreviewPanel", hudObj.transform);
            RectTransform prevPanelRect = previewPanelObj.GetComponent<RectTransform>();
            prevPanelRect.anchorMin = Vector2.zero;
            prevPanelRect.anchorMax = Vector2.one;
            prevPanelRect.sizeDelta = Vector2.zero;
            Image prevPanelBg = previewPanelObj.AddComponent<Image>();
            prevPanelBg.color = new Color(0, 0, 0, 0.75f);

            GameObject prevImgObj = CreateUIElement("PreviewImage", previewPanelObj.transform);
            RectTransform prevImgRect = prevImgObj.GetComponent<RectTransform>();
            prevImgRect.sizeDelta = new Vector2(500, 500);
            Image prevImg = prevImgObj.AddComponent<Image>();
            if (levelData != null) prevImg.sprite = levelData.puzzleSprite;
            prevImg.preserveAspect = true;

            GameObject closePrevBtn = CreateButtonElement("ClosePreviewBtn", previewPanelObj.transform, "Close Preview", new Color(0.8f, 0.3f, 0.3f), new Vector2(0, -290), new Vector2(180, 50));
            closePrevBtn.GetComponent<Button>().onClick.AddListener(hudCtrl.TogglePreview);

            hudCtrl.previewPanel = previewPanelObj;
            hudCtrl.previewImage = prevImg;
            previewPanelObj.SetActive(false);

            // 7. Win Panel
            GameObject winPanelObj = CreateUIElement("WinPanel", canvasObj.transform);
            RectTransform winRect = winPanelObj.GetComponent<RectTransform>();
            winRect.anchorMin = Vector2.zero;
            winRect.anchorMax = Vector2.one;
            winRect.sizeDelta = Vector2.zero;
            Image winBg = winPanelObj.AddComponent<Image>();
            winBg.color = new Color(0, 0, 0, 0.75f);
            WinPanel winCtrl = winPanelObj.AddComponent<WinPanel>();

            // Modal Card
            GameObject cardObj = CreateUIElement("Card", winPanelObj.transform);
            RectTransform cardRect = cardObj.GetComponent<RectTransform>();
            cardRect.sizeDelta = new Vector2(500, 380);
            Image cardImg = cardObj.AddComponent<Image>();
            cardImg.color = new Color(0.15f, 0.18f, 0.24f);

            // Win Title
            GameObject winTitleObj = CreateTextElement("WinTitle", cardObj.transform, "You Win!", 48, TextAlignmentOptions.Center);
            RectTransform winTitleRect = winTitleObj.GetComponent<RectTransform>();
            winTitleRect.anchoredPosition = new Vector2(0, 110);
            winTitleRect.sizeDelta = new Vector2(400, 70);
            winCtrl.titleText = winTitleObj.GetComponent<TextMeshProUGUI>();

            // Win Time
            GameObject winTimeObj = CreateTextElement("WinTime", cardObj.transform, "Completed in 00:00", 26, TextAlignmentOptions.Center);
            RectTransform winTimeRect = winTimeObj.GetComponent<RectTransform>();
            winTimeRect.anchoredPosition = new Vector2(0, 45);
            winTimeRect.sizeDelta = new Vector2(400, 50);
            winCtrl.timeText = winTimeObj.GetComponent<TextMeshProUGUI>();

            // Next Level Button
            GameObject nextBtnObj = CreateButtonElement("NextLevelBtn", cardObj.transform, "Next Level", new Color(0.20f, 0.70f, 0.30f), new Vector2(0, -30), new Vector2(280, 55));
            winCtrl.nextLevelButton = nextBtnObj.GetComponent<Button>();

            // Replay Button
            GameObject replayBtnObj = CreateButtonElement("ReplayBtn", cardObj.transform, "Play Again", new Color(0.25f, 0.55f, 0.85f), new Vector2(0, -100), new Vector2(280, 55));
            winCtrl.replayButton = replayBtnObj.GetComponent<Button>();

            winPanelObj.SetActive(false);

            // 8. Puzzle Board
            GameObject boardObj = new GameObject("PuzzleBoard");
            PuzzleBoard board = boardObj.AddComponent<PuzzleBoard>();
            board.fallbackLevelData = levelData;
            board.scatter = boardObj.AddComponent<PieceScatter>();

            // 9. GameManager
            GameObject gmObj = new GameObject("GameManager");
            GameManager gm = gmObj.AddComponent<GameManager>();
            gm.puzzleBoard = board;
            gm.hud = hudCtrl;
            gm.winPanel = winCtrl;

            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log("[PuzzleSetupTools] Game scene built and saved successfully at: " + scenePath);
        }

        [MenuItem("Tools/Jigsaw/4. Build Main Menu Scene", false, 12)]
        public static void BuildMainMenuScene()
        {
            EnsureFolderExists("Assets/Scenes");
            string scenePath = "Assets/Scenes/MainMenu.unity";

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Camera
            GameObject camObj = new GameObject("Main Camera");
            Camera cam = camObj.AddComponent<Camera>();
            camObj.tag = "MainCamera";
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.14f, 0.16f, 0.22f);
            camObj.transform.position = new Vector3(0, 0, -10f);
            camObj.AddComponent<AudioListener>();

            // Event System
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            // Canvas
            GameObject canvasObj = new GameObject("Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();

            MainMenuUI menuUI = canvasObj.AddComponent<MainMenuUI>();

            // Title
            GameObject titleObj = CreateTextElement("Title", canvasObj.transform, "JIGSAW PUZZLE", 64, TextAlignmentOptions.Center);
            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchoredPosition = new Vector2(0, 200);
            titleRect.sizeDelta = new Vector2(800, 100);

            // Play Button
            GameObject playBtn = CreateButtonElement("PlayBtn", canvasObj.transform, "PLAY GAME", new Color(0.20f, 0.70f, 0.30f), new Vector2(0, 40), new Vector2(320, 70));
            menuUI.playButton = playBtn.GetComponent<Button>();

            // Level Select Button
            GameObject levelsBtn = CreateButtonElement("LevelsBtn", canvasObj.transform, "LEVEL SELECT", new Color(0.25f, 0.55f, 0.85f), new Vector2(0, -60), new Vector2(320, 70));
            menuUI.levelSelectButton = levelsBtn.GetComponent<Button>();

            // Quit Button
            GameObject quitBtn = CreateButtonElement("QuitBtn", canvasObj.transform, "QUIT", new Color(0.80f, 0.30f, 0.30f), new Vector2(0, -160), new Vector2(320, 70));
            menuUI.quitButton = quitBtn.GetComponent<Button>();

            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log("[PuzzleSetupTools] Main Menu scene built and saved successfully at: " + scenePath);
        }

        [MenuItem("Tools/Jigsaw/5. Build Level Select Scene", false, 13)]
        public static void BuildLevelSelectScene()
        {
            EnsureFolderExists("Assets/Scenes");
            string scenePath = "Assets/Scenes/LevelSelect.unity";

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Camera
            GameObject camObj = new GameObject("Main Camera");
            Camera cam = camObj.AddComponent<Camera>();
            camObj.tag = "MainCamera";
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.14f, 0.16f, 0.22f);
            camObj.transform.position = new Vector3(0, 0, -10f);
            camObj.AddComponent<AudioListener>();

            // Event System
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            // Canvas
            GameObject canvasObj = new GameObject("Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();

            LevelSelectUI levelUI = canvasObj.AddComponent<LevelSelectUI>();

            // Title
            GameObject titleObj = CreateTextElement("Title", canvasObj.transform, "SELECT A PUZZLE", 54, TextAlignmentOptions.Center);
            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchoredPosition = new Vector2(0, 360);
            titleRect.sizeDelta = new Vector2(800, 80);

            // Grid Container
            GameObject gridObj = CreateUIElement("GridContainer", canvasObj.transform);
            RectTransform gridRect = gridObj.GetComponent<RectTransform>();
            gridRect.anchoredPosition = new Vector2(0, 50);
            gridRect.sizeDelta = new Vector2(900, 500);

            GridLayoutGroup grid = gridObj.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(260, 130);
            grid.spacing = new Vector2(30, 30);
            grid.childAlignment = TextAnchor.MiddleCenter;

            // Template Button
            GameObject tmplBtn = CreateButtonElement("LevelBtnTemplate", gridObj.transform, "Level 1", new Color(0.25f, 0.55f, 0.85f), Vector2.zero, new Vector2(260, 130));
            levelUI.gridContainer = gridObj.transform;
            levelUI.levelButtonPrefab = tmplBtn;

            // Back Button
            GameObject backBtn = CreateButtonElement("BackBtn", canvasObj.transform, "BACK TO MENU", new Color(0.5f, 0.5f, 0.5f), new Vector2(0, -380), new Vector2(260, 60));
            levelUI.backButton = backBtn.GetComponent<Button>();

            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log("[PuzzleSetupTools] Level Select scene built and saved successfully at: " + scenePath);
        }

        [MenuItem("Tools/Jigsaw/6. Configure Build Settings", false, 14)]
        public static void ConfigureBuildSettings()
        {
            string[] scenePaths = new string[]
            {
                "Assets/Scenes/MainMenu.unity",
                "Assets/Scenes/LevelSelect.unity",
                "Assets/Scenes/Game.unity"
            };

            EditorBuildSettingsScene[] scenes = new EditorBuildSettingsScene[scenePaths.Length];
            for (int i = 0; i < scenePaths.Length; i++)
            {
                scenes[i] = new EditorBuildSettingsScene(scenePaths[i], true);
            }

            EditorBuildSettings.scenes = scenes;
            Debug.Log("[PuzzleSetupTools] Configured Build Settings with MainMenu (0), LevelSelect (1), Game (2).");
        }

        [MenuItem("Tools/Jigsaw/Enable Read-Write on Selected Textures", false, 30)]
        public static void EnableReadWriteOnSelected()
        {
            Object[] selected = Selection.objects;
            int count = 0;
            foreach (Object obj in selected)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null)
                {
                    importer.isReadable = true;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.SaveAndReimport();
                    count++;
                }
            }
            EditorUtility.DisplayDialog("Texture Helper", $"Enabled Read/Write on {count} selected texture(s).", "OK");
        }

        #region Helper Methods

        private static GameObject CreateUIElement(string name, Transform parent)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            return obj;
        }

        private static GameObject CreateTextElement(string name, Transform parent, string text, float fontSize, TextAlignmentOptions align)
        {
            GameObject obj = CreateUIElement(name, parent);
            TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = align;
            tmp.color = Color.white;
            return obj;
        }

        private static GameObject CreateButtonElement(string name, Transform parent, string label, Color color, Vector2 anchoredPos, Vector2 size)
        {
            GameObject obj = CreateUIElement(name, parent);
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1, 0.5f);
            rect.anchorMax = new Vector2(1, 0.5f);
            rect.pivot = new Vector2(1, 0.5f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;

            Image img = obj.AddComponent<Image>();
            img.color = color;

            Button btn = obj.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.highlightedColor = color * 1.15f;
            cb.pressedColor = color * 0.85f;
            btn.colors = cb;

            GameObject textObj = CreateTextElement("Label", obj.transform, label, 20, TextAlignmentOptions.Center);
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI textTmp = textObj.GetComponent<TextMeshProUGUI>();
            textTmp.fontStyle = FontStyles.Bold;

            return obj;
        }

        private static void EnsureFolderExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        #endregion
    }
}
#endif
