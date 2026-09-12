#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using MasterFramework.Playable.Month10High;
using Portfolio.Playable.Gonu;
using Portfolio.Playable.Month36High;
using Portfolio.Playable.Seesaw;
using MasterFramework.Navigation;
using MasterFramework.Core;
using System.Collections.Generic;
using System.Linq;

namespace MasterFramework.Editor
{
    public static class PortfolioBuilderHelper
    {
        [MenuItem("MasterFramework/Build All Portfolio Assets")]
        public static void BuildAll()
        {
            BuildCellPrefab();
            BuildUniversalStagePrefab();
            BuildGonuStagePrefab();
            BuildMonth36HighAssets();
            BuildStandaloneLessonOutlines();
            BuildGameOutlines();
            SetupScenesAndBuildSettings();
            Debug.Log("[PortfolioBuilderHelper] Completed building all assets and scene configurations!");
        }

        [MenuItem("MasterFramework/Build Playable Cell Prefab")]
        public static void BuildCellPrefab()
        {
            GameObject go = new GameObject("PlayableRectSquareCell", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(PlayableRectSquareCell));
            var img = go.GetComponent<Image>();
            img.color = Color.white;
            
            GameObject signGo = new GameObject("SignImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            signGo.transform.SetParent(go.transform, false);
            var signRt = signGo.GetComponent<RectTransform>();
            signRt.anchorMin = Vector2.zero;
            signRt.anchorMax = Vector2.one;
            signRt.sizeDelta = Vector2.zero;
            
            var signImg = signGo.GetComponent<Image>();
            signImg.color = PlayableRectSquareCell.StateColors[0];
            
            var cell = go.GetComponent<PlayableRectSquareCell>();
            var s1 = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath("2b53b39a111345f41b58c608e87c08e1"));
            var s2 = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath("c2ced724c848f54449c0c461d190e499"));
            var s3 = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath("33cef3b078422b244a206bcd25a6e5ca"));
            
            var serializedCell = new SerializedObject(cell);
            var signSpritesProp = serializedCell.FindProperty("signSprites");
            signSpritesProp.arraySize = 3;
            signSpritesProp.GetArrayElementAtIndex(0).objectReferenceValue = s1;
            signSpritesProp.GetArrayElementAtIndex(1).objectReferenceValue = s2;
            signSpritesProp.GetArrayElementAtIndex(2).objectReferenceValue = s3;
            
            serializedCell.FindProperty("signImage").objectReferenceValue = signImg;
            serializedCell.ApplyModifiedProperties();
            
            string prefabPath = "Assets/MasterFramework/Playable/Month10High/AnyResources/PlayableRectSquareCell.prefab";
            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);
            Debug.Log("[PortfolioBuilderHelper] Created cell prefab at: " + prefabPath);
        }

        [MenuItem("MasterFramework/Build Universal Playable Stage Prefab")]
        public static void BuildUniversalStagePrefab()
        {
            GameObject go = new GameObject("UniversalPlayableStageTemplate", typeof(RectTransform), typeof(UniversalPlayableStage), typeof(RectSquareGridSystem));
            
            GameObject cellParent = new GameObject("CellParent", typeof(RectTransform), typeof(GridLayoutGroup));
            cellParent.transform.SetParent(go.transform, false);
            var cpRt = cellParent.GetComponent<RectTransform>();
            cpRt.anchorMin = new Vector2(0.1f, 0.15f);
            cpRt.anchorMax = new Vector2(0.9f, 0.85f);
            cpRt.anchoredPosition = Vector2.zero;
            cpRt.sizeDelta = Vector2.zero;
            
            var gridLayout = cellParent.GetComponent<GridLayoutGroup>();
            gridLayout.spacing = new Vector2(5, 5);
            gridLayout.childAlignment = TextAnchor.MiddleCenter;
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            
            GameObject buttonGo = new GameObject("CheckAnswerButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonGo.transform.SetParent(go.transform, false);
            var btnRt = buttonGo.GetComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(0.5f, 0f);
            btnRt.anchorMax = new Vector2(0.5f, 0f);
            btnRt.anchoredPosition = new Vector2(0, 40);
            btnRt.sizeDelta = new Vector2(160, 50);
            
            var btnImg = buttonGo.GetComponent<Image>();
            btnImg.color = new Color(0.2f, 0.6f, 0.85f, 1f);
            
            GameObject btnTextGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            btnTextGo.transform.SetParent(buttonGo.transform, false);
            var txtRt = btnTextGo.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.sizeDelta = Vector2.zero;
            var txt = btnTextGo.GetComponent<Text>();
            txt.text = "정답 확인";
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 20;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            
            var stage = go.GetComponent<UniversalPlayableStage>();
            var serializedStage = new SerializedObject(stage);
            
            var cellPrefab = AssetDatabase.LoadAssetAtPath<PlayableRectSquareCell>("Assets/MasterFramework/Playable/Month10High/AnyResources/PlayableRectSquareCell.prefab");
            var framePrefab = AssetDatabase.LoadAssetAtPath<PlayableRectSquareFrame>("Assets/MasterFramework/Playable/Month10High/AnyResources/Prefabs/Frame.prefab");
            
            serializedStage.FindProperty("cellPrefab").objectReferenceValue = cellPrefab;
            serializedStage.FindProperty("framePrefab").objectReferenceValue = framePrefab;
            serializedStage.FindProperty("cellParent").objectReferenceValue = cpRt;
            serializedStage.FindProperty("checkAnswerButton").objectReferenceValue = buttonGo.GetComponent<Button>();
            
            serializedStage.ApplyModifiedProperties();
            
            // SubContainer setup
            var ctx = go.AddComponent<Zenject.GameObjectContext>();
            var installer = go.AddComponent<Month10HighInstaller>();
            
            var serializedCtx = new SerializedObject(ctx);
            var installersProp = serializedCtx.FindProperty("_monoInstallers");
            installersProp.arraySize = 1;
            installersProp.GetArrayElementAtIndex(0).objectReferenceValue = installer;
            serializedCtx.ApplyModifiedProperties();
            
            string prefabPath = "Assets/MasterFramework/Playable/Month10High/UniversalPlayableStageTemplate.prefab";
            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);
            Debug.Log("[PortfolioBuilderHelper] Created template prefab at: " + prefabPath);
        }

        [MenuItem("MasterFramework/Build Gonu Stage Base Prefab")]
        public static void BuildGonuStagePrefab()
        {
            string sourcePath = "Assets/MasterFramework/Playable/Prefabs/Why02/i_Learning_2_11_Stage.prefab";
            var src = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
            if (src == null)
            {
                Debug.LogWarning("[PortfolioBuilderHelper] Source Gonu prefab not found at: " + sourcePath);
                return;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(src);
            
            var oldPresenter = instance.GetComponent("Why02Week11Presenter");
            if (oldPresenter != null) Object.DestroyImmediate(oldPresenter);

            var oldStages = instance.GetComponentsInChildren(typeof(Component), true)
                .Where(c => c != null && c.GetType().Name.StartsWith("Why02Week11Stage"))
                .ToList();
            foreach (var s in oldStages) Object.DestroyImmediate(s);

            var organizer = instance.GetComponent<GonuStageOrganizer>();
            if (organizer == null) organizer = instance.AddComponent<GonuStageOrganizer>();

            var ctx = instance.GetComponent<Zenject.GameObjectContext>();
            if (ctx == null) ctx = instance.AddComponent<Zenject.GameObjectContext>();

            var installer = instance.GetComponent<GonuInstaller>();
            if (installer == null) installer = instance.AddComponent<GonuInstaller>();

            var serializedCtx = new SerializedObject(ctx);
            var monoInstallersProp = serializedCtx.FindProperty("_monoInstallers");
            monoInstallersProp.arraySize = 1;
            monoInstallersProp.GetArrayElementAtIndex(0).objectReferenceValue = installer;
            serializedCtx.ApplyModifiedProperties();

            string targetFolder = "Assets/MasterFramework/Playable/Gonu";
            if (!AssetDatabase.IsValidFolder(targetFolder))
            {
                AssetDatabase.CreateFolder("Assets/MasterFramework/Playable", "Gonu");
            }
            string destPath = $"{targetFolder}/GonuStage_BasePrefab.prefab";
            PrefabUtility.SaveAsPrefabAsset(instance, destPath);
            Object.DestroyImmediate(instance);

            Debug.Log("[PortfolioBuilderHelper] Gonu Base Prefab cleaned and saved successfully at: " + destPath);
        }

        [MenuItem("MasterFramework/Build Month 36 High Assets")]
        public static void BuildMonth36HighAssets()
        {
            string folderPath = "Assets/MasterFramework/Playable/Month36High";
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder("Assets/MasterFramework/Playable", "Month36High");
            }
            if (!AssetDatabase.IsValidFolder($"{folderPath}/Prefabs"))
            {
                AssetDatabase.CreateFolder(folderPath, "Prefabs");
            }

            // 1. Create LevelData ScriptableObjects for Stage 1 ~ 4 (4x6, 6x6, 7x7, 8x8)
            CreateOrUpdateLevelData($"{folderPath}/CircuitBuildingLevelData_Stage1.asset", 4, 6, 116f, 2, new List<SpecialCellInfo>
            {
                new SpecialCellInfo(0, 0, 2, 0),
                new SpecialCellInfo(1, 3, 8, 1)
            });

            CreateOrUpdateLevelData($"{folderPath}/CircuitBuildingLevelData_Stage2.asset", 6, 6, 116f, 5, new List<SpecialCellInfo>
            {
                new SpecialCellInfo(1, 2, 4, 1),
                new SpecialCellInfo(1, 5, 4, 1),
                new SpecialCellInfo(3, 2, 8, 1),
                new SpecialCellInfo(4, 1, 8, 1),
                new SpecialCellInfo(5, 3, 8, 1)
            });

            CreateOrUpdateLevelData($"{folderPath}/CircuitBuildingLevelData_Stage3.asset", 7, 7, 100f, 6, new List<SpecialCellInfo>
            {
                new SpecialCellInfo(0, 0, 2, 1),
                new SpecialCellInfo(1, 6, 4, 0),
                new SpecialCellInfo(3, 3, 6, 0),
                new SpecialCellInfo(5, 1, 6, 2),
                new SpecialCellInfo(6, 4, 8, 2)
            });

            CreateOrUpdateLevelData($"{folderPath}/CircuitBuildingLevelData_Stage4.asset", 8, 8, 88f, 8, new List<SpecialCellInfo>
            {
                new SpecialCellInfo(1, 5, 2, 1),
                new SpecialCellInfo(2, 3, 6, 1),
                new SpecialCellInfo(3, 1, 8, 1),
                new SpecialCellInfo(4, 4, 5, 0),
                new SpecialCellInfo(4, 6, 4, 1),
                new SpecialCellInfo(5, 4, 8, 2),
                new SpecialCellInfo(6, 2, 4, 1)
            });

            // 2. Open Original Scene to Extract Authentic Stage 1 Hierarchy
            string originalScenePath = "Assets/Noisy/Scenes/ChallengePuzzle/Month36/Month36HighScene.unity";
            var origScene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(originalScenePath, UnityEditor.SceneManagement.OpenSceneMode.Single);

            var stage1Source = GameObject.Find("[Root]/CanvasGroup/[MainUI]/Stage1");
            if (stage1Source == null)
            {
                Debug.LogError("[PortfolioBuilderHelper] Source Stage1 not found in Month36HighScene!");
                return;
            }

            // Clone Stage 1
            GameObject clonedStage = Object.Instantiate(stage1Source);
            clonedStage.name = "CircuitBuilding_BasePrefab";

            // Remove legacy stage component
            var legacyMiniGameStage = clonedStage.GetComponent<CMS.WeeklyGame.MiniGameStage>();
            if (legacyMiniGameStage != null) Object.DestroyImmediate(legacyMiniGameStage);

            // Find authentic UI child elements
            var levelHeaderImg = clonedStage.transform.Find("Level Header")?.GetComponent<Image>();
            var bgTransform = clonedStage.transform.Find("Background");
            var boardTransform = bgTransform?.Find("Board");
            var gridLayout = boardTransform?.GetComponent<GridLayoutGroup>();
            var legacyBoardComp = boardTransform?.GetComponent<MiniGame.CircuitBuilding.CircuitBuildingBoard>();
            if (legacyBoardComp != null) Object.DestroyImmediate(legacyBoardComp);

            // Setup CircuitParent inside Board if not already present
            var circuitParentTransform = boardTransform?.Find("CircuitParent");
            if (circuitParentTransform == null && boardTransform != null)
            {
                GameObject cpGo = new GameObject("CircuitParent", typeof(RectTransform), typeof(CanvasGroup));
                cpGo.transform.SetParent(boardTransform, false);
                var cpRt = cpGo.GetComponent<RectTransform>();
                cpRt.anchorMin = Vector2.zero;
                cpRt.anchorMax = Vector2.one;
                cpRt.sizeDelta = Vector2.zero;
                circuitParentTransform = cpGo.transform;
            }

            var circuitToggle = clonedStage.transform.Find("Circuit Mode Toggle")?.GetComponent<Toggle>();
            var pillarToggle = clonedStage.transform.Find("Pillar Mode Toggle")?.GetComponent<Toggle>();
            var checkBtn = clonedStage.transform.Find("CheckTheAnswerButton")?.GetComponent<Button>();
            var ruleBtn = clonedStage.transform.Find("RuleButton")?.GetComponent<Button>();
            var popupGo = clonedStage.transform.Find("Popup")?.gameObject;
            var popupCloseBtn = popupGo?.transform.Find("Window/X Button")?.GetComponent<Button>();

            // Load Original Prefabs & Level Sprites
            var originalCellPrefab = AssetDatabase.LoadAssetAtPath<MiniGame.CircuitBuilding.CircuitBuildingCell>("Assets/Noisy/CMS/Exclude build/ChallengePuzzle/Month36/HighGrade/AnyResources/Cell.prefab");
            var originalCircuitPrefab = AssetDatabase.LoadAssetAtPath<MiniGame.CircuitBuilding.CircuitBuildingCircuit>("Assets/Noisy/CMS/Exclude build/ChallengePuzzle/Month36/HighGrade/AnyResources/Circuit.prefab");

            var sprL1 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Noisy/CMS/Exclude build/ChallengePuzzle/Month36/HighGrade/AnyResources/level1.png");
            var sprL2 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Noisy/CMS/Exclude build/ChallengePuzzle/Month36/HighGrade/AnyResources/level2.png");
            var sprL3 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Noisy/CMS/Exclude build/ChallengePuzzle/Month36/HighGrade/AnyResources/level3.png");
            var sprL4 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Noisy/CMS/Exclude build/ChallengePuzzle/Month36/HighGrade/AnyResources/level4.png");

            // Attach & Configure CircuitBuildingStage
            var stage = clonedStage.GetComponent<CircuitBuildingStage>();
            if (stage == null) stage = clonedStage.AddComponent<CircuitBuildingStage>();

            var serializedStage = new SerializedObject(stage);
            serializedStage.FindProperty("cellPrefab").objectReferenceValue = originalCellPrefab;
            serializedStage.FindProperty("circuitPrefab").objectReferenceValue = originalCircuitPrefab;
            serializedStage.FindProperty("levelHeaderImage").objectReferenceValue = levelHeaderImg;
            
            var levelSpritesProp = serializedStage.FindProperty("levelSprites");
            levelSpritesProp.arraySize = 4;
            levelSpritesProp.GetArrayElementAtIndex(0).objectReferenceValue = sprL1;
            levelSpritesProp.GetArrayElementAtIndex(1).objectReferenceValue = sprL2;
            levelSpritesProp.GetArrayElementAtIndex(2).objectReferenceValue = sprL3;
            levelSpritesProp.GetArrayElementAtIndex(3).objectReferenceValue = sprL4;

            serializedStage.FindProperty("boardRoot").objectReferenceValue = boardTransform as RectTransform;
            serializedStage.FindProperty("gridLayoutGroup").objectReferenceValue = gridLayout;
            serializedStage.FindProperty("circuitParent").objectReferenceValue = circuitParentTransform;
            serializedStage.FindProperty("checkAnswerButton").objectReferenceValue = checkBtn;
            serializedStage.FindProperty("circuitModeToggle").objectReferenceValue = circuitToggle;
            serializedStage.FindProperty("pillarModeToggle").objectReferenceValue = pillarToggle;
            serializedStage.FindProperty("ruleButton").objectReferenceValue = ruleBtn;
            serializedStage.FindProperty("popupObject").objectReferenceValue = popupGo;
            serializedStage.FindProperty("popupCloseButton").objectReferenceValue = popupCloseBtn;
            serializedStage.ApplyModifiedProperties();

            // SubContainer setup
            var ctx = clonedStage.GetComponent<Zenject.GameObjectContext>();
            if (ctx == null) ctx = clonedStage.AddComponent<Zenject.GameObjectContext>();

            var installer = clonedStage.GetComponent<Month36HighInstaller>();
            if (installer == null) installer = clonedStage.AddComponent<Month36HighInstaller>();

            var serializedCtx = new SerializedObject(ctx);
            var installersProp = serializedCtx.FindProperty("_monoInstallers");
            installersProp.arraySize = 1;
            installersProp.GetArrayElementAtIndex(0).objectReferenceValue = installer;
            serializedCtx.ApplyModifiedProperties();

            // Save Authentic Base Prefab
            string prefabPath = $"{folderPath}/Prefabs/CircuitBuilding_BasePrefab.prefab";
            PrefabUtility.SaveAsPrefabAsset(clonedStage, prefabPath);
            Object.DestroyImmediate(clonedStage);

            Debug.Log("[PortfolioBuilderHelper] Successfully extracted authentic Stage1 hierarchy and saved Month 36 High Base Prefab at: " + prefabPath);
        }

        private static void CreateOrUpdateLevelData(string assetPath, int row, int col, float sideLen, int fullPillar, List<SpecialCellInfo> specialCells)
        {
            var data = AssetDatabase.LoadAssetAtPath<CircuitBuildingLevelData>(assetPath);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<CircuitBuildingLevelData>();
                AssetDatabase.CreateAsset(data, assetPath);
            }
            data.rowCount = row;
            data.columnCount = col;
            data.cellSideLength = sideLen;
            data.fullPillarCount = fullPillar;
            data.specialCells = specialCells;
            EditorUtility.SetDirty(data);
        }

        [MenuItem("MasterFramework/Build Standalone LessonOutlines")]
        public static void BuildStandaloneLessonOutlines()
        {
            string folderPath = "Assets/MasterFramework/Navigation/Outlines";
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                if (!AssetDatabase.IsValidFolder("Assets/MasterFramework/Navigation"))
                {
                    AssetDatabase.CreateFolder("Assets/MasterFramework", "Navigation");
                }
                AssetDatabase.CreateFolder("Assets/MasterFramework/Navigation", "Outlines");
            }

            // 1. Month 10 High Outline
            string m10Path = $"{folderPath}/LessonOutline_Month10High.asset";
            var m10Outline = AssetDatabase.LoadAssetAtPath<LessonOutline>(m10Path);
            if (m10Outline == null)
            {
                m10Outline = ScriptableObject.CreateInstance<LessonOutline>();
                AssetDatabase.CreateAsset(m10Outline, m10Path);
            }
            m10Outline.lessonId = "Month10High";
            m10Outline.steps = new List<PageStepInfo>();

            var m10TemplatePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MasterFramework/Playable/Month10High/UniversalPlayableStageTemplate.prefab");
            for (int i = 1; i <= 4; i++)
            {
                var levelData = AssetDatabase.LoadAssetAtPath<PlayableLevelData>($"Assets/MasterFramework/Playable/Month10High/PlayableLevelData_Stage{i}.asset");
                var step = new PageStepInfo
                {
                    stepIndex = i - 1,
                    actCode = $"ACT0{i}",
                    stepName = $"Stage {i}",
                    isDynamicGeneration = true,
                    stageTemplatePrefab = m10TemplatePrefab,
                    stagePrefab = m10TemplatePrefab,
                    levelData = levelData,
                    requiresVideo = false,
                    videoSlotIndex = -1
                };
                m10Outline.steps.Add(step);
            }
            EditorUtility.SetDirty(m10Outline);

            // 2. Gonu Outline
            string gonuPath = $"{folderPath}/LessonOutline_Gonu.asset";
            var gonuOutline = AssetDatabase.LoadAssetAtPath<LessonOutline>(gonuPath);
            if (gonuOutline == null)
            {
                gonuOutline = ScriptableObject.CreateInstance<LessonOutline>();
                AssetDatabase.CreateAsset(gonuOutline, gonuPath);
            }
            gonuOutline.levelCode = "Gonu";
            gonuOutline.lessonId = "Gonu_AI";
            gonuOutline.steps = new List<PageStepInfo>();

            var gonuPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MasterFramework/Playable/Gonu/GonuStage_BasePrefab.prefab");
            string[] gonuStepNames = new string[] { "1단계: 유불리 판별 퍼즐 1", "2단계: 유불리 판별 퍼즐 2", "3단계: 우물고누 AI 실전 대전" };
            for (int i = 0; i < 3; i++)
            {
                var step = new PageStepInfo
                {
                    stepIndex = i,
                    actCode = $"ACT0{i+1}",
                    stepName = gonuStepNames[i],
                    isDynamicGeneration = false,
                    stagePrefab = gonuPrefab,
                    requiresVideo = false,
                    videoSlotIndex = -1
                };
                gonuOutline.steps.Add(step);
            }
            EditorUtility.SetDirty(gonuOutline);

            // 3. Month 36 High (CircuitBuilding) Outline
            string m36Path = $"{folderPath}/LessonOutline_Month36High.asset";
            var m36Outline = AssetDatabase.LoadAssetAtPath<LessonOutline>(m36Path);
            if (m36Outline == null)
            {
                m36Outline = ScriptableObject.CreateInstance<LessonOutline>();
                AssetDatabase.CreateAsset(m36Outline, m36Path);
            }
            m36Outline.lessonId = "Month36High";
            m36Outline.steps = new List<PageStepInfo>();

            var m36BasePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MasterFramework/Playable/Month36High/Prefabs/CircuitBuilding_BasePrefab.prefab");
            string[] m36StepNames = new string[] { "Stage 1 (4x6)", "Stage 2 (6x6)", "Stage 3 (7x7)", "Stage 4 (8x8)" };
            for (int i = 1; i <= 4; i++)
            {
                var lData = AssetDatabase.LoadAssetAtPath<CircuitBuildingLevelData>($"Assets/MasterFramework/Playable/Month36High/CircuitBuildingLevelData_Stage{i}.asset");
                var step = new PageStepInfo
                {
                    stepIndex = i - 1,
                    actCode = $"ACT0{i}",
                    stepName = m36StepNames[i - 1],
                    isDynamicGeneration = true,
                    stageTemplatePrefab = m36BasePrefab,
                    stagePrefab = m36BasePrefab,
                    levelData = lData,
                    requiresVideo = false,
                    videoSlotIndex = -1
                };
                m36Outline.steps.Add(step);
            }
            EditorUtility.SetDirty(m36Outline);

            // 4. Seesaw Outline
            string m35Path = $"{folderPath}/LessonOutline_Month35High.asset";
            var m35Outline = AssetDatabase.LoadAssetAtPath<LessonOutline>(m35Path);
            if (m35Outline == null)
            {
                m35Outline = ScriptableObject.CreateInstance<LessonOutline>();
                AssetDatabase.CreateAsset(m35Outline, m35Path);
            }
            m35Outline.levelCode = "Seesaw";
            m35Outline.lessonId = "Seesaw_Balance";
            m35Outline.steps = new List<PageStepInfo>();

            var m35BasePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/Month35High.prefab");
            string[] m35StepNames = new string[] { "Stage 1 (4x5)", "Stage 2 (4x6)", "Stage 3 (4x5)", "Stage 4 (5x5)" };
            for (int i = 1; i <= 4; i++)
            {
                var lData = AssetDatabase.LoadAssetAtPath<PlaySeesawLevelData>($"Assets/MasterFramework/Playable/Seesaw/PlaySeesawLevelData_Stage{i}.asset");
                var step = new PageStepInfo
                {
                    stepIndex = i - 1,
                    actCode = $"ACT0{i}",
                    stepName = m35StepNames[i - 1],
                    isDynamicGeneration = true,
                    stageTemplatePrefab = m35BasePrefab,
                    stagePrefab = m35BasePrefab,
                    levelData = lData,
                    requiresVideo = false,
                    videoSlotIndex = -1
                };
                m35Outline.steps.Add(step);
            }
            EditorUtility.SetDirty(m35Outline);

            AssetDatabase.SaveAssets();
            Debug.Log("[PortfolioBuilderHelper] Standalone LessonOutlines built successfully!");
        }

        [MenuItem("MasterFramework/Build Game Outlines")]
        public static void BuildGameOutlines()
        {
            string folderPath = "Assets/MasterFramework/Navigation/Outlines";
            var m10LessonOutline = AssetDatabase.LoadAssetAtPath<LessonOutline>($"{folderPath}/LessonOutline_Month10High.asset");
            var gonuLessonOutline = AssetDatabase.LoadAssetAtPath<LessonOutline>($"{folderPath}/LessonOutline_Gonu.asset");
            var m36LessonOutline = AssetDatabase.LoadAssetAtPath<LessonOutline>($"{folderPath}/LessonOutline_Month36High.asset");
            var m35LessonOutline = AssetDatabase.LoadAssetAtPath<LessonOutline>($"{folderPath}/LessonOutline_Month35High.asset");

            // 1. Seesaw GameOutline
            string path1 = $"{folderPath}/GameOutline_Month35High.asset";
            var outline1 = AssetDatabase.LoadAssetAtPath<PortfolioGameOutline>(path1);
            if (outline1 == null)
            {
                outline1 = ScriptableObject.CreateInstance<PortfolioGameOutline>();
                AssetDatabase.CreateAsset(outline1, path1);
            }
            outline1.gameId = "Seesaw_Balance";
            outline1.gameTitle = "시소 균형 논리 퍼즐";
            outline1.subtitle = "Seesaw Balance Logic Puzzle";
            outline1.generationTag = "Logic Puzzle";
            outline1.systemSummary = "지레 모멘트(거리 × 무게)에 따른 수평 평형 공식과 행/열/프레임 제약 조건에 맞춰 숫자 블록을 배치하는 격자 논리 퍼즐입니다.";
            outline1.featurePoints = new string[]
            {
                "지레 모멘트(거리 × 무게) 계산 기반 실시간 수평 균형 판정",
                "격자형 키패드 입력, 행/열/프레임 제약 검증 및 Auto-Solve"
            };
            outline1.hasAutoSolve = true;
            outline1.totalStages = 4;
            outline1.lessonOutline = m35LessonOutline;
            EditorUtility.SetDirty(outline1);

            // 2. Month 36 High GameOutline
            string path2 = $"{folderPath}/GameOutline_Month36High.asset";
            var outline2 = AssetDatabase.LoadAssetAtPath<PortfolioGameOutline>(path2);
            if (outline2 == null)
            {
                outline2 = ScriptableObject.CreateInstance<PortfolioGameOutline>();
                AssetDatabase.CreateAsset(outline2, path2);
            }
            outline2.gameId = "Month36High";
            outline2.gameTitle = "회로 연결 퍼즐";
            outline2.subtitle = "Circuit Building";
            outline2.generationTag = "Logic Puzzle";
            outline2.systemSummary = "기둥 시선 힌트를 만족하며 모든 회로 타일을 끊김 없는 단일 폐회로로 연결하는 논리 퍼즐입니다.";
            outline2.featurePoints = new string[]
            {
                "4방향 회로 타일의 단자 연결 상태 및 폐회로 실시간 검증",
                "전구-배터리 간 경로 완성 시 점등 및 클리어 시각화"
            };
            outline2.hasAutoSolve = true;
            outline2.totalStages = 4;
            outline2.lessonOutline = m36LessonOutline;
            EditorUtility.SetDirty(outline2);

            // 3. Gonu AI GameOutline
            string path3 = $"{folderPath}/GameOutline_Gonu.asset";
            var outline3 = AssetDatabase.LoadAssetAtPath<PortfolioGameOutline>(path3);
            if (outline3 == null)
            {
                outline3 = ScriptableObject.CreateInstance<PortfolioGameOutline>();
                AssetDatabase.CreateAsset(outline3, path3);
            }
            outline3.gameId = "Gonu_AI";
            outline3.gameTitle = "전통 고누 AI 대전";
            outline3.subtitle = "Traditional Board Game & Minimax AI";
            outline3.generationTag = "Strategy AI";
            outline3.systemSummary = "전통 말판 놀이(삼목/우물/세기) 규칙에 따라 말을 이동시켜 상대방의 경로를 차단하거나 승리 조건을 달성하는 턴제 전략 보드게임 및 Minimax 기반 로컬 AI입니다.";
            outline3.featurePoints = new string[]
            {
                "말판 노드 기반 유효 이동 경로 검증 및 포위 승패 판정",
                "턴 제어 FSM 및 Minimax AI 휴리스틱 의사결정 엔진"
            };
            outline3.hasAutoSolve = false;
            outline3.totalStages = 3;
            outline3.lessonOutline = gonuLessonOutline;
            EditorUtility.SetDirty(outline3);

            // 4. Month 10 High GameOutline
            string path4 = $"{folderPath}/GameOutline_Month10High.asset";
            var outline4 = AssetDatabase.LoadAssetAtPath<PortfolioGameOutline>(path4);
            if (outline4 == null)
            {
                outline4 = ScriptableObject.CreateInstance<PortfolioGameOutline>();
                AssetDatabase.CreateAsset(outline4, path4);
            }
            outline4.gameId = "Month10High";
            outline4.gameTitle = "직사각형 분할 퍼즐";
            outline4.subtitle = "Rectangle Division";
            outline4.generationTag = "Gen 3 Puzzle";
            outline4.systemSummary = "숫자 힌트를 포함하는 직사각형 및 정사각형 영역으로 격자판을 분할하는 기하학 퍼즐입니다.";
            outline4.featurePoints = new string[]
            {
                "영역 드래그 선택 및 직사각형 면적 일치 유효성 검증",
                "격자 중복 영역 방지 및 전체 타일 분할 완성 판정"
            };
            outline4.hasAutoSolve = true;
            outline4.totalStages = 4;
            outline4.lessonOutline = m10LessonOutline;
            EditorUtility.SetDirty(outline4);

            AssetDatabase.SaveAssets();
            Debug.Log("[PortfolioBuilderHelper] Successfully created/updated PortfolioGameOutline assets linked to dedicated LessonOutlines!");
        }

        [MenuItem("MasterFramework/Setup Scenes and Build Settings")]
        public static void SetupScenesAndBuildSettings()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("[PortfolioBuilderHelper] Skipping scene setup during play mode.");
                return;
            }

            string sandboxPath = "Assets/Scenes/MasterSandbox.unity";
            var sandboxScene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(sandboxPath, UnityEditor.SceneManagement.OpenSceneMode.Single);

            var stageMgr = Object.FindObjectOfType<UniversalStageManager>();
            if (stageMgr != null)
            {
                var serializedStageMgr = new SerializedObject(stageMgr);
                var outlineProp = serializedStageMgr.FindProperty("lessonOutline");
                var defaultOutline = AssetDatabase.LoadAssetAtPath<LessonOutline>("Assets/MasterFramework/Navigation/Outlines/LessonOutline_Month35High.asset");
                if (outlineProp != null && defaultOutline != null)
                {
                    outlineProp.objectReferenceValue = defaultOutline;
                    serializedStageMgr.ApplyModifiedProperties();
                }
            }

            var allTransforms = Object.FindObjectsOfType<Transform>(true);
            foreach (var t in allTransforms)
            {
                if (t.name == "PrevButton" || t.name == "NextButton")
                {
                    t.gameObject.SetActive(false);
                    EditorUtility.SetDirty(t.gameObject);
                }
            }

            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(sandboxScene);

            // Create or Update LobbyScene.unity
            string lobbyPath = "Assets/Scenes/LobbyScene.unity";
            var lobbyScene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.DefaultGameObjects, UnityEditor.SceneManagement.NewSceneMode.Single);

            Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // 1. Canvas Root
            GameObject canvasGo = new GameObject("LobbyCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            // Background
            GameObject bgGo = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bgGo.transform.SetParent(canvasGo.transform, false);
            var bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.sizeDelta = Vector2.zero;
            var bgImg = bgGo.GetComponent<Image>();
            bgImg.color = new Color32(10, 14, 23, 255); // Deep midnight navy

            // 2. Header Container
            GameObject headerGo = new GameObject("HeaderContainer", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            headerGo.transform.SetParent(canvasGo.transform, false);
            var headerRt = headerGo.GetComponent<RectTransform>();
            headerRt.anchorMin = new Vector2(0.04f, 0.82f);
            headerRt.anchorMax = new Vector2(0.96f, 0.96f);
            headerRt.sizeDelta = Vector2.zero;
            headerRt.anchoredPosition = Vector2.zero;
            var headerImg = headerGo.GetComponent<Image>();
            headerImg.color = new Color32(18, 24, 38, 240); // Dark glass header

            var headerOutline = headerGo.AddComponent<Outline>();
            headerOutline.effectColor = new Color32(51, 65, 85, 200);
            headerOutline.effectDistance = new Vector2(1, -1);

            // Header Title
            GameObject titleGo = new GameObject("TitleText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            titleGo.transform.SetParent(headerGo.transform, false);
            var titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0.02f, 0.45f);
            titleRt.anchorMax = new Vector2(0.7f, 0.95f);
            titleRt.sizeDelta = Vector2.zero;
            var titleTxt = titleGo.GetComponent<Text>();
            titleTxt.font = defaultFont;
            titleTxt.text = "🏛️  UNITY CLIENT & ARCHITECTURE SHOWCASE";
            titleTxt.fontSize = 24;
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.color = new Color32(248, 250, 252, 255);
            titleTxt.alignment = TextAnchor.MiddleLeft;

            // Header Subtitle
            GameObject subGo = new GameObject("SubtitleText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            subGo.transform.SetParent(headerGo.transform, false);
            var subRt = subGo.GetComponent<RectTransform>();
            subRt.anchorMin = new Vector2(0.02f, 0.05f);
            subRt.anchorMax = new Vector2(0.7f, 0.48f);
            subRt.sizeDelta = Vector2.zero;
            var subTxt = subGo.GetComponent<Text>();
            subTxt.font = defaultFont;
            subTxt.text = "다양한 퍼즐 시스템을 데이터 주도(ScriptableObject)와 단일 공용 프레임워크로 통합한 포트폴리오";
            subTxt.fontSize = 14;
            subTxt.fontStyle = FontStyle.Normal;
            subTxt.color = new Color32(148, 163, 184, 255);
            subTxt.alignment = TextAnchor.MiddleLeft;

            // GitHub Button (Top Right)
            GameObject gitBtnGo = new GameObject("GitHubButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            gitBtnGo.transform.SetParent(headerGo.transform, false);
            var gitRt = gitBtnGo.GetComponent<RectTransform>();
            gitRt.anchorMin = new Vector2(0.86f, 0.2f);
            gitRt.anchorMax = new Vector2(0.98f, 0.8f);
            gitRt.sizeDelta = Vector2.zero;
            var gitImg = gitBtnGo.GetComponent<Image>();
            gitImg.color = new Color32(37, 99, 235, 255);

            GameObject gitTxtGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            gitTxtGo.transform.SetParent(gitBtnGo.transform, false);
            var gitTxtRt = gitTxtGo.GetComponent<RectTransform>();
            gitTxtRt.anchorMin = Vector2.zero;
            gitTxtRt.anchorMax = Vector2.one;
            gitTxtRt.sizeDelta = Vector2.zero;
            var gitTxt = gitTxtGo.GetComponent<Text>();
            gitTxt.font = defaultFont;
            gitTxt.text = "🔗  GitHub ↗";
            gitTxt.fontSize = 14;
            gitTxt.fontStyle = FontStyle.Bold;
            gitTxt.color = Color.white;
            gitTxt.alignment = TextAnchor.MiddleCenter;

            // Architecture Overview Button (Top Right 2)
            GameObject archBtnGo = new GameObject("ArchButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            archBtnGo.transform.SetParent(headerGo.transform, false);
            var archRt = archBtnGo.GetComponent<RectTransform>();
            archRt.anchorMin = new Vector2(0.72f, 0.2f);
            archRt.anchorMax = new Vector2(0.85f, 0.8f);
            archRt.sizeDelta = Vector2.zero;
            var archImg = archBtnGo.GetComponent<Image>();
            archImg.color = new Color32(51, 65, 85, 255);

            GameObject archTxtGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            archTxtGo.transform.SetParent(archBtnGo.transform, false);
            var archTxtRt = archTxtGo.GetComponent<RectTransform>();
            archTxtRt.anchorMin = Vector2.zero;
            archTxtRt.anchorMax = Vector2.one;
            archTxtRt.sizeDelta = Vector2.zero;
            var archTxt = archTxtGo.GetComponent<Text>();
            archTxt.font = defaultFont;
            archTxt.text = "ℹ️  아키텍처 개요";
            archTxt.fontSize = 14;
            archTxt.fontStyle = FontStyle.Bold;
            archTxt.color = Color.white;
            archTxt.alignment = TextAnchor.MiddleCenter;

            // 3. Card Parent (Horizontal Flex Layout)
            GameObject cardParentGo = new GameObject("CardParent", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            cardParentGo.transform.SetParent(canvasGo.transform, false);
            var cpRt = cardParentGo.GetComponent<RectTransform>();
            cpRt.anchorMin = new Vector2(0.04f, 0.1f);
            cpRt.anchorMax = new Vector2(0.96f, 0.78f);
            cpRt.anchoredPosition = Vector2.zero;
            cpRt.sizeDelta = Vector2.zero;

            var layout = cardParentGo.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 24;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;

            // 4. Footer Note
            GameObject footerGo = new GameObject("FooterNote", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            footerGo.transform.SetParent(canvasGo.transform, false);
            var footerRt = footerGo.GetComponent<RectTransform>();
            footerRt.anchorMin = new Vector2(0.04f, 0.02f);
            footerRt.anchorMax = new Vector2(0.96f, 0.07f);
            footerRt.sizeDelta = Vector2.zero;
            var footerTxt = footerGo.GetComponent<Text>();
            footerTxt.font = defaultFont;
            footerTxt.text = "MasterFramework v2.0  •  Built with Unity 2022 LTS  •  Zenject DI  •  UniRx Reactive Streams  •  UniTask  •  DOTween";
            footerTxt.fontSize = 13;
            footerTxt.color = new Color32(100, 116, 139, 255);
            footerTxt.alignment = TextAnchor.MiddleCenter;

            // 5. Manager Setup
            GameObject lobbyMgrGo = new GameObject("PortfolioLobbyManager", typeof(PortfolioLobbyManager));
            var lobbyMgr = lobbyMgrGo.GetComponent<PortfolioLobbyManager>();

            var m35Outline = AssetDatabase.LoadAssetAtPath<PortfolioGameOutline>("Assets/MasterFramework/Navigation/Outlines/GameOutline_Month35High.asset");
            var gonuOutline = AssetDatabase.LoadAssetAtPath<PortfolioGameOutline>("Assets/MasterFramework/Navigation/Outlines/GameOutline_Gonu.asset");

            var serializedLobby = new SerializedObject(lobbyMgr);
            serializedLobby.FindProperty("cardParent").objectReferenceValue = cardParentGo.transform;
            serializedLobby.FindProperty("githubButton").objectReferenceValue = gitBtnGo.GetComponent<Button>();
            serializedLobby.FindProperty("archOverviewButton").objectReferenceValue = archBtnGo.GetComponent<Button>();
            
            var gamesProp = serializedLobby.FindProperty("availableGames");
            gamesProp.arraySize = 2;
            gamesProp.GetArrayElementAtIndex(0).objectReferenceValue = m35Outline;
            gamesProp.GetArrayElementAtIndex(1).objectReferenceValue = gonuOutline;
            serializedLobby.ApplyModifiedProperties();

            bool saved = UnityEditor.SceneManagement.EditorSceneManager.SaveScene(lobbyScene, lobbyPath);
            AssetDatabase.Refresh();
            Debug.Log($"[PortfolioBuilderHelper] SaveScene LobbyScene result: {saved}");

            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(sandboxPath, UnityEditor.SceneManagement.OpenSceneMode.Single);

            var scenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene(lobbyPath, true),
                new EditorBuildSettingsScene(sandboxPath, true)
            };
            EditorBuildSettings.scenes = scenes;

            Debug.Log("[PortfolioBuilderHelper] Successfully created rich LobbyScene.unity and updated MasterSandbox.unity & Build Settings!");
        }
    }
}
#endif
