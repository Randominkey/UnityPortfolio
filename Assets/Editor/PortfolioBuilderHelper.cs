#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using MasterFramework.Playable.Month10High;
using MasterFramework.Playable.Month34High;
using Portfolio.Why02.Gonu;
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
            BuildMonth34HighAssets();
            ModernizeMonth35HighUI();
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
            
            var stage = go.GetComponent<UniversalPlayableStage>();
            var serializedStage = new SerializedObject(stage);
            
            var cellPrefab = AssetDatabase.LoadAssetAtPath<PlayableRectSquareCell>("Assets/MasterFramework/Playable/Month10High/AnyResources/PlayableRectSquareCell.prefab");
            var framePrefab = AssetDatabase.LoadAssetAtPath<PlayableRectSquareFrame>("Assets/MasterFramework/Playable/Month10High/AnyResources/PlayableRectSquareFrame.prefab");
            
            serializedStage.FindProperty("cellPrefab").objectReferenceValue = cellPrefab;
            serializedStage.FindProperty("framePrefab").objectReferenceValue = framePrefab;
            serializedStage.FindProperty("cellParent").objectReferenceValue = cpRt;
            serializedStage.FindProperty("checkAnswerButton").objectReferenceValue = null;
            
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

            // 이전 회사 IP 캐릭터 스프라이트 제거 및 모던 턴 인디케이터 UI로 교체
            CleanAndModernizePlayerToggles(instance);

            string targetFolder = "Assets/MasterFramework/Playable/Why02_Gonu";
            if (!AssetDatabase.IsValidFolder(targetFolder))
            {
                AssetDatabase.CreateFolder("Assets/MasterFramework/Playable", "Why02_Gonu");
            }
            string destPath = $"{targetFolder}/GonuStage_BasePrefab.prefab";
            PrefabUtility.SaveAsPrefabAsset(instance, destPath);
            Object.DestroyImmediate(instance);

            Debug.Log("[PortfolioBuilderHelper] Gonu Base Prefab cleaned and saved successfully at: " + destPath);
        }

        private static void CleanAndModernizePlayerToggles(GameObject instance)
        {
            Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            Transform pt0 = instance.transform.Find("PlayerToggle0");
            Transform pt1 = instance.transform.Find("PlayerToggle1");

            if (pt0 != null)
            {
                SetupPlayerCard(pt0, "1P  PLAYER", "● 선공 (내 턴)", new Color32(37, 99, 235, 255), defaultFont);
            }

            if (pt1 != null)
            {
                SetupPlayerCard(pt1, "2P  AI OPPONENT", "○ 후공 (인공지능)", new Color32(225, 29, 72, 255), defaultFont);
            }
        }

        private static void SetupPlayerCard(Transform pt, string title, string sub, Color32 themeColor, Font font)
        {
            var rt = pt.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(280, 160);

            Transform offTrans = pt.Find(pt.name.Replace("PlayerToggle", "PlayerToggleOff"));
            Transform onTrans = offTrans != null ? offTrans.Find(pt.name.Replace("PlayerToggle", "PlayerToggleOn")) : null;

            if (offTrans != null)
            {
                var offRt = offTrans.GetComponent<RectTransform>();
                offRt.anchorMin = Vector2.zero;
                offRt.anchorMax = Vector2.one;
                offRt.sizeDelta = Vector2.zero;

                var offImg = offTrans.GetComponent<Image>();
                var serializedOffImg = new SerializedObject(offImg);
                serializedOffImg.FindProperty("m_Sprite").objectReferenceValue = null;
                serializedOffImg.ApplyModifiedProperties();
                offImg.color = new Color32(18, 24, 38, 230); // Dark sleek slate

                var outline = offTrans.GetComponent<Outline>() ?? offTrans.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color32(51, 65, 85, 200);
                outline.effectDistance = new Vector2(2, -2);

                // Text title
                Transform titleTrans = offTrans.Find("TitleText");
                GameObject titleGo = titleTrans != null ? titleTrans.gameObject : new GameObject("TitleText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                titleGo.transform.SetParent(offTrans, false);
                var tRt = titleGo.GetComponent<RectTransform>();
                tRt.anchorMin = new Vector2(0.05f, 0.48f);
                tRt.anchorMax = new Vector2(0.95f, 0.92f);
                tRt.sizeDelta = Vector2.zero;
                var tTxt = titleGo.GetComponent<Text>();
                tTxt.font = font;
                tTxt.text = title;
                tTxt.fontSize = 20;
                tTxt.fontStyle = FontStyle.Bold;
                tTxt.color = Color.white;
                tTxt.alignment = TextAnchor.MiddleCenter;

                // Text subtitle
                Transform subTrans = offTrans.Find("SubText");
                GameObject subGo = subTrans != null ? subTrans.gameObject : new GameObject("SubText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                subGo.transform.SetParent(offTrans, false);
                var sRt = subGo.GetComponent<RectTransform>();
                sRt.anchorMin = new Vector2(0.05f, 0.12f);
                sRt.anchorMax = new Vector2(0.95f, 0.48f);
                sRt.sizeDelta = Vector2.zero;
                var sTxt = subGo.GetComponent<Text>();
                sTxt.font = font;
                sTxt.text = sub;
                sTxt.fontSize = 15;
                sTxt.fontStyle = FontStyle.Normal;
                sTxt.color = new Color32(148, 163, 184, 255);
                sTxt.alignment = TextAnchor.MiddleCenter;
            }

            if (onTrans != null)
            {
                var onRt = onTrans.GetComponent<RectTransform>();
                onRt.anchorMin = Vector2.zero;
                onRt.anchorMax = Vector2.one;
                onRt.sizeDelta = Vector2.zero;

                var onImg = onTrans.GetComponent<Image>();
                var serializedOnImg = new SerializedObject(onImg);
                serializedOnImg.FindProperty("m_Sprite").objectReferenceValue = null;
                serializedOnImg.ApplyModifiedProperties();
                onImg.color = new Color32(themeColor.r, themeColor.g, themeColor.b, 60);

                var onOutline = onTrans.GetComponent<Outline>() ?? onTrans.gameObject.AddComponent<Outline>();
                onOutline.effectColor = themeColor;
                onOutline.effectDistance = new Vector2(3, -3);
            }
        }

        [MenuItem("MasterFramework/Build Month 34 High Assets")]
        public static void BuildMonth34HighAssets()
        {
            string folderPath = "Assets/MasterFramework/Playable/Month34High";
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder("Assets/MasterFramework/Playable", "Month34High");
            }
            if (!AssetDatabase.IsValidFolder($"{folderPath}/Prefabs"))
            {
                AssetDatabase.CreateFolder(folderPath, "Prefabs");
            }

            // 1. Create LevelData ScriptableObjects for Stage 1 ~ 4
            // Stage 1 (8x8)
            CreateOrUpdateIslandLevelData($"{folderPath}/NumberIslandLevelData_Stage1.asset", 8, 8, new Vector3Int[]
            {
                new Vector3Int(0, 6, 3), new Vector3Int(1, 1, 8), new Vector3Int(2, 7, 2),
                new Vector3Int(3, 6, 1), new Vector3Int(4, 2, 3), new Vector3Int(4, 4, 2),
                new Vector3Int(5, 3, 1), new Vector3Int(5, 7, 2), new Vector3Int(6, 1, 6),
                new Vector3Int(7, 6, 6)
            });

            // Stage 2 (5x5)
            CreateOrUpdateIslandLevelData($"{folderPath}/NumberIslandLevelData_Stage2.asset", 5, 5, new Vector3Int[]
            {
                new Vector3Int(0, 2, 3), new Vector3Int(0, 4, 1), new Vector3Int(1, 3, 1),
                new Vector3Int(2, 1, 4), new Vector3Int(4, 0, 1), new Vector3Int(4, 4, 2)
            });

            // Stage 3 (6x6)
            CreateOrUpdateIslandLevelData($"{folderPath}/NumberIslandLevelData_Stage3.asset", 6, 6, new Vector3Int[]
            {
                new Vector3Int(0, 0, 4), new Vector3Int(0, 5, 1), new Vector3Int(1, 4, 1),
                new Vector3Int(3, 2, 6), new Vector3Int(4, 0, 2), new Vector3Int(4, 5, 4)
            });

            // Stage 4 (10x10)
            CreateOrUpdateIslandLevelData($"{folderPath}/NumberIslandLevelData_Stage4.asset", 10, 10, new Vector3Int[]
            {
                new Vector3Int(0, 6, 4), new Vector3Int(0, 8, 1), new Vector3Int(1, 2, 1),
                new Vector3Int(2, 0, 6), new Vector3Int(2, 5, 4), new Vector3Int(2, 9, 2),
                new Vector3Int(3, 3, 2), new Vector3Int(3, 8, 2), new Vector3Int(5, 3, 12),
                new Vector3Int(6, 0, 1), new Vector3Int(7, 1, 2), new Vector3Int(8, 5, 4),
                new Vector3Int(9, 0, 1), new Vector3Int(9, 2, 1), new Vector3Int(9, 9, 6)
            });

            // 2. Build Base Prefab
            Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            GameObject rootGo = new GameObject("NumberIsland_BasePrefab", typeof(RectTransform), typeof(CanvasGroup));
            var rootRt = rootGo.GetComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.sizeDelta = Vector2.zero;

            // Background Frame
            GameObject bgGo = new GameObject("BoardFrame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bgGo.transform.SetParent(rootGo.transform, false);
            var bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = new Vector2(0.5f, 0.5f);
            bgRt.anchorMax = new Vector2(0.5f, 0.5f);
            bgRt.sizeDelta = new Vector2(680, 680);
            bgRt.anchoredPosition = new Vector2(0, 20);
            var bgImg = bgGo.GetComponent<Image>();
            bgImg.color = new Color32(20, 27, 45, 255); // Dark navy board background

            var bgOutline = bgGo.AddComponent<Outline>();
            bgOutline.effectColor = new Color32(71, 85, 105, 180);
            bgOutline.effectDistance = new Vector2(2, -2);

            // CellParent (GridLayoutGroup)
            GameObject cellParentGo = new GameObject("CellParent", typeof(RectTransform), typeof(GridLayoutGroup));
            cellParentGo.transform.SetParent(bgGo.transform, false);
            var cpRt = cellParentGo.GetComponent<RectTransform>();
            cpRt.anchorMin = Vector2.zero;
            cpRt.anchorMax = Vector2.one;
            cpRt.sizeDelta = new Vector2(-24, -24); // 12px padding all sides
            cpRt.anchoredPosition = Vector2.zero;

            var gridLayout = cellParentGo.GetComponent<GridLayoutGroup>();
            gridLayout.spacing = new Vector2(4, 4);
            gridLayout.childAlignment = TextAnchor.MiddleCenter;

            // Attach NumberIslandStage (정답 확인은 상단 헤더 툴바로 일원화됨)
            var stage = rootGo.AddComponent<NumberIslandStage>();
            var cellPrefab = AssetDatabase.LoadAssetAtPath<NumberIslandCell>("Assets/MasterFramework/Playable/Month34High/Prefabs/NumberIslandCell.prefab");
            var defaultLevelData = AssetDatabase.LoadAssetAtPath<NumberIslandLevelData>($"{folderPath}/NumberIslandLevelData_Stage1.asset");

            var serializedStage = new SerializedObject(stage);
            serializedStage.FindProperty("cellParent").objectReferenceValue = gridLayout;
            serializedStage.FindProperty("cellPrefab").objectReferenceValue = cellPrefab;
            serializedStage.FindProperty("levelData").objectReferenceValue = defaultLevelData;
            serializedStage.FindProperty("checkAnswerButton").objectReferenceValue = null;
            serializedStage.FindProperty("clearTriggerMode").enumValueIndex = (int)StageClearTriggerMode.ManualSubmitButton;
            serializedStage.FindProperty("validationCapability").enumValueIndex = (int)ValidationCapability.Flexible;
            serializedStage.ApplyModifiedProperties();

            // Zenject SubContainer setup
            var ctx = rootGo.AddComponent<Zenject.GameObjectContext>();
            var installer = rootGo.AddComponent<Month34HighInstaller>();
            var serializedCtx = new SerializedObject(ctx);
            var installersProp = serializedCtx.FindProperty("_monoInstallers");
            installersProp.arraySize = 1;
            installersProp.GetArrayElementAtIndex(0).objectReferenceValue = installer;
            serializedCtx.ApplyModifiedProperties();

            // Save Prefab
            string prefabPath = $"{folderPath}/Prefabs/NumberIsland_BasePrefab.prefab";
            PrefabUtility.SaveAsPrefabAsset(rootGo, prefabPath);
            Object.DestroyImmediate(rootGo);

            AssetDatabase.SaveAssets();
            Debug.Log("[PortfolioBuilderHelper] Successfully built Month 34 High assets and BasePrefab at: " + prefabPath);
        }

        [MenuItem("MasterFramework/Modernize Month 35 High UI")]
        public static void ModernizeMonth35HighUI()
        {
            string prefabPath = "Assets/Resources/Prefabs/Month35High.prefab";
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            if (root == null)
            {
                Debug.LogWarning("[PortfolioBuilderHelper] Month35High prefab not found at: " + prefabPath);
                return;
            }

            // 1. Remove bottom duplicate panels and buttons (상단 헤더 툴바로 일원화)
            Transform panelTrans = root.transform.Find("ControlPanel");
            if (panelTrans != null)
            {
                Object.DestroyImmediate(panelTrans.gameObject);
            }

            Transform ruleTrans = root.transform.Find("RuleButton");
            if (ruleTrans != null)
            {
                Object.DestroyImmediate(ruleTrans.gameObject);
            }

            Transform checkTrans = root.transform.Find("CheckTheAnswerButton");
            if (checkTrans != null)
            {
                Object.DestroyImmediate(checkTrans.gameObject);
            }

            // 2. Update UniversalMinigameStage references
            var stage = root.GetComponent<MasterFramework.Playable.Common.UniversalMinigameStage>();
            if (stage != null)
            {
                var serializedStage = new SerializedObject(stage);
                serializedStage.FindProperty("checkAnswerButton").objectReferenceValue = null;
                serializedStage.FindProperty("ruleButton").objectReferenceValue = null;
                serializedStage.ApplyModifiedProperties();
            }

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            PrefabUtility.UnloadPrefabContents(root);
            Debug.Log("[PortfolioBuilderHelper] Successfully modernized Month 35 High UI in: " + prefabPath);
        }

        private static void SetupButtonLabel(Transform btnTrans, string labelText, Font font)
        {
            Transform textTrans = btnTrans.Find("Text");
            GameObject textGo;
            if (textTrans == null)
            {
                textGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                textGo.transform.SetParent(btnTrans, false);
            }
            else
            {
                textGo = textTrans.gameObject;
            }

            var txtRt = textGo.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.sizeDelta = Vector2.zero;

            var txt = textGo.GetComponent<Text>();
            txt.font = font;
            txt.text = labelText;
            txt.fontSize = 15;
            txt.fontStyle = FontStyle.Bold;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.raycastTarget = false;
        }

        private static Button CreateControlButton(Transform parent, string name, string label, Color32 color, Font font)
        {
            GameObject btnGo = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(parent, false);
            var rt = btnGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(150, 48);

            var img = btnGo.GetComponent<Image>();
            img.color = color;

            var outline = btnGo.AddComponent<Outline>();
            outline.effectColor = new Color32(255, 255, 255, 30);
            outline.effectDistance = new Vector2(1, -1);

            GameObject txtGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            txtGo.transform.SetParent(btnGo.transform, false);
            var txtRt = txtGo.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.sizeDelta = Vector2.zero;

            var txt = txtGo.GetComponent<Text>();
            txt.font = font;
            txt.text = label;
            txt.fontSize = 15;
            txt.fontStyle = FontStyle.Bold;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;

            return btnGo.GetComponent<Button>();
        }

        private static Toggle CreateToggleControlButton(Transform parent, string name, string label, Font font)
        {
            GameObject toggleGo = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Toggle));
            toggleGo.transform.SetParent(parent, false);
            var rt = toggleGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(165, 48);

            var bgImg = toggleGo.GetComponent<Image>();
            bgImg.color = new Color32(30, 41, 59, 255); // Dark slate background

            var outline = toggleGo.AddComponent<Outline>();
            outline.effectColor = new Color32(99, 102, 241, 150);
            outline.effectDistance = new Vector2(1, -1);

            // Checkmark overlay (Emerald green when ON)
            GameObject checkmarkGo = new GameObject("Checkmark", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            checkmarkGo.transform.SetParent(toggleGo.transform, false);
            var cmRt = checkmarkGo.GetComponent<RectTransform>();
            cmRt.anchorMin = Vector2.zero;
            cmRt.anchorMax = Vector2.one;
            cmRt.sizeDelta = Vector2.zero;
            var cmImg = checkmarkGo.GetComponent<Image>();
            cmImg.color = new Color32(16, 185, 129, 200);

            GameObject txtGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            txtGo.transform.SetParent(toggleGo.transform, false);
            var txtRt = txtGo.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.sizeDelta = Vector2.zero;

            var txt = txtGo.GetComponent<Text>();
            txt.font = font;
            txt.text = label;
            txt.fontSize = 14;
            txt.fontStyle = FontStyle.Bold;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;

            var toggle = toggleGo.GetComponent<Toggle>();
            toggle.targetGraphic = bgImg;
            toggle.graphic = cmImg;
            toggle.isOn = false;

            return toggle;
        }

        private static void CreateOrUpdateIslandLevelData(string assetPath, int row, int col, Vector3Int[] hints)
        {
            var data = AssetDatabase.LoadAssetAtPath<NumberIslandLevelData>(assetPath);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<NumberIslandLevelData>();
                AssetDatabase.CreateAsset(data, assetPath);
            }
            data.SetData(row, col, hints);
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
            gonuOutline.lessonId = "Why02_Week11_Gonu";
            gonuOutline.steps = new List<PageStepInfo>();

            var gonuPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MasterFramework/Playable/Why02_Gonu/GonuStage_BasePrefab.prefab");
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

            // 3. Month 34 High (NumberIsland) Outline
            string m34Path = $"{folderPath}/LessonOutline_Month34High.asset";
            var m34Outline = AssetDatabase.LoadAssetAtPath<LessonOutline>(m34Path);
            if (m34Outline == null)
            {
                m34Outline = ScriptableObject.CreateInstance<LessonOutline>();
                AssetDatabase.CreateAsset(m34Outline, m34Path);
            }
            m34Outline.lessonId = "Month34High";
            m34Outline.steps = new List<PageStepInfo>();

            var m34BasePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MasterFramework/Playable/Month34High/Prefabs/NumberIsland_BasePrefab.prefab");
            string[] m34StepNames = new string[] { "Stage 1 (8x8)", "Stage 2 (5x5)", "Stage 3 (6x6)", "Stage 4 (10x10)" };
            for (int i = 1; i <= 4; i++)
            {
                var lData = AssetDatabase.LoadAssetAtPath<NumberIslandLevelData>($"Assets/MasterFramework/Playable/Month34High/NumberIslandLevelData_Stage{i}.asset");
                var step = new PageStepInfo
                {
                    stepIndex = i - 1,
                    actCode = $"ACT0{i}",
                    stepName = m34StepNames[i - 1],
                    isDynamicGeneration = true,
                    stageTemplatePrefab = m34BasePrefab,
                    stagePrefab = m34BasePrefab,
                    levelData = lData,
                    requiresVideo = false,
                    videoSlotIndex = -1
                };
                m34Outline.steps.Add(step);
            }
            EditorUtility.SetDirty(m34Outline);

            // 4. Month 35 High (PlaySeesaw) Outline
            string m35Path = $"{folderPath}/LessonOutline_Month35High.asset";
            var m35Outline = AssetDatabase.LoadAssetAtPath<LessonOutline>(m35Path);
            if (m35Outline == null)
            {
                m35Outline = ScriptableObject.CreateInstance<LessonOutline>();
                AssetDatabase.CreateAsset(m35Outline, m35Path);
            }
            m35Outline.lessonId = "Month35High";
            m35Outline.steps = new List<PageStepInfo>();

            var m35BasePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/Month35High.prefab");
            string[] m35StepNames = new string[] { "Stage 1 (4x5)", "Stage 2 (4x6)", "Stage 3 (4x5)", "Stage 4 (5x5)" };
            for (int i = 1; i <= 4; i++)
            {
                var lData = AssetDatabase.LoadAssetAtPath<Portfolio.Playable.Month35High.PlaySeesawLevelData>($"Assets/MasterFramework/Playable/Month35High/PlaySeesawLevelData_Stage{i}.asset");
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
            var m34LessonOutline = AssetDatabase.LoadAssetAtPath<LessonOutline>($"{folderPath}/LessonOutline_Month34High.asset");
            var m35LessonOutline = AssetDatabase.LoadAssetAtPath<LessonOutline>($"{folderPath}/LessonOutline_Month35High.asset");

            // 1. Month 35 High GameOutline
            string path1 = $"{folderPath}/GameOutline_Month35High.asset";
            var outline1 = AssetDatabase.LoadAssetAtPath<PortfolioGameOutline>(path1);
            if (outline1 == null)
            {
                outline1 = ScriptableObject.CreateInstance<PortfolioGameOutline>();
                AssetDatabase.CreateAsset(outline1, path1);
            }
            outline1.gameId = "Month35High";
            outline1.gameTitle = "시소 논리 퍼즐";
            outline1.subtitle = "Play Seesaw";
            outline1.generationTag = "Gen 2 Minigame";
            outline1.systemSummary = "시소의 수평 평형 공식과 행/열/프레임 조건에 맞춰 숫자 블록을 배치하는 격자 논리 퍼즐입니다.";
            outline1.featurePoints = new string[]
            {
                "지레 모멘트(거리 × 무게) 계산 기반 실시간 수평 균형 판정",
                "격자형 키패드 입력 및 행/열/프레임 제약 조건 검증"
            };
            outline1.hasAutoSolve = true;
            outline1.totalStages = 4;
            outline1.lessonOutline = m35LessonOutline;
            EditorUtility.SetDirty(outline1);

            // 2. Month 34 High GameOutline
            string path2 = $"{folderPath}/GameOutline_Month34High.asset";
            var outline2 = AssetDatabase.LoadAssetAtPath<PortfolioGameOutline>(path2);
            if (outline2 == null)
            {
                outline2 = ScriptableObject.CreateInstance<PortfolioGameOutline>();
                AssetDatabase.CreateAsset(outline2, path2);
            }
            outline2.gameId = "Month34High";
            outline2.gameTitle = "숫자 섬 나누기 퍼즐";
            outline2.subtitle = "Number Island";
            outline2.generationTag = "Gen 2 Minigame";
            outline2.systemSummary = "각 숫자를 면적으로 갖는 직사각형 섬으로 격자를 분할하고, 바다와 섬의 연결성을 검증하는 논리 퍼즐입니다.";
            outline2.featurePoints = new string[]
            {
                "단일 숫자 포함 직사각형 섬 유효성 및 2x2 바다 영역 방지 알고리즘",
                "BFS 기반 섬 대각선 네트워크 연결성 판정 및 Undo/Redo Memento 시스템"
            };
            outline2.hasAutoSolve = true;
            outline2.totalStages = 4;
            outline2.lessonOutline = m34LessonOutline;
            EditorUtility.SetDirty(outline2);

            // 3. Gonu AI GameOutline
            string path3 = $"{folderPath}/GameOutline_Gonu.asset";
            var outline3 = AssetDatabase.LoadAssetAtPath<PortfolioGameOutline>(path3);
            if (outline3 == null)
            {
                outline3 = ScriptableObject.CreateInstance<PortfolioGameOutline>();
                AssetDatabase.CreateAsset(outline3, path3);
            }
            outline3.gameId = "Why02_Gonu";
            outline3.gameTitle = "우물고누 전통놀이";
            outline3.subtitle = "Traditional Board Game";
            outline3.generationTag = "Why 02";
            outline3.systemSummary = "전통 말판 놀이 규칙에 따라 말을 이동시켜 상대방의 이동 경로를 차단하는 턴제 보드게임입니다.";
            outline3.featurePoints = new string[]
            {
                "말판 노드 기반 유효 이동 경로 검증 및 포위 승패 판정",
                "턴 제어 FSM 및 말 위치 스냅샷 복원 시스템"
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
                var defaultOutline = AssetDatabase.LoadAssetAtPath<LessonOutline>("Assets/MasterFramework/Navigation/Outlines/LessonOutline_Month10High.asset");
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

            // 3. Card Parent (Wrapped in Horizontal ScrollView)
            GameObject scrollViewGo = new GameObject("CardScrollView", typeof(RectTransform), typeof(ScrollRect));
            scrollViewGo.transform.SetParent(canvasGo.transform, false);
            var svRt = scrollViewGo.GetComponent<RectTransform>();
            svRt.anchorMin = new Vector2(0.04f, 0.12f);
            svRt.anchorMax = new Vector2(0.96f, 0.78f);
            svRt.anchoredPosition = Vector2.zero;
            svRt.sizeDelta = Vector2.zero;

            var scrollRect = scrollViewGo.GetComponent<ScrollRect>();
            scrollRect.horizontal = true;
            scrollRect.vertical = false;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;
            scrollRect.elasticity = 0.1f;
            scrollRect.scrollSensitivity = 40f;

            GameObject viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewportGo.transform.SetParent(scrollViewGo.transform, false);
            var vpRt = viewportGo.GetComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero;
            vpRt.anchorMax = Vector2.one;
            vpRt.sizeDelta = Vector2.zero;
            vpRt.pivot = new Vector2(0, 0.5f);

            scrollRect.viewport = vpRt;

            GameObject cardParentGo = new GameObject("CardParent", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
            cardParentGo.transform.SetParent(viewportGo.transform, false);
            var cpRt = cardParentGo.GetComponent<RectTransform>();
            cpRt.anchorMin = new Vector2(0, 0);
            cpRt.anchorMax = new Vector2(0, 1);
            cpRt.pivot = new Vector2(0, 0.5f);
            cpRt.anchoredPosition = Vector2.zero;

            var fitter = cardParentGo.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            var layout = cardParentGo.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 20;
            layout.padding = new RectOffset(20, 40, 0, 0);
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = false;

            scrollRect.content = cpRt;

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
            var m34Outline = AssetDatabase.LoadAssetAtPath<PortfolioGameOutline>("Assets/MasterFramework/Navigation/Outlines/GameOutline_Month34High.asset");
            var gonuOutline = AssetDatabase.LoadAssetAtPath<PortfolioGameOutline>("Assets/MasterFramework/Navigation/Outlines/GameOutline_Gonu.asset");
            var m10Outline = AssetDatabase.LoadAssetAtPath<PortfolioGameOutline>("Assets/MasterFramework/Navigation/Outlines/GameOutline_Month10High.asset");
            var m22Outline = AssetDatabase.LoadAssetAtPath<PortfolioGameOutline>("Assets/MasterFramework/Navigation/Outlines/GameOutline_Month22Low.asset");

            var serializedLobby = new SerializedObject(lobbyMgr);
            serializedLobby.FindProperty("cardParent").objectReferenceValue = cardParentGo.transform;
            serializedLobby.FindProperty("githubButton").objectReferenceValue = gitBtnGo.GetComponent<Button>();
            serializedLobby.FindProperty("archOverviewButton").objectReferenceValue = archBtnGo.GetComponent<Button>();
            
            var gamesProp = serializedLobby.FindProperty("availableGames");
            gamesProp.arraySize = 5;
            gamesProp.GetArrayElementAtIndex(0).objectReferenceValue = m35Outline;
            gamesProp.GetArrayElementAtIndex(1).objectReferenceValue = m34Outline;
            gamesProp.GetArrayElementAtIndex(2).objectReferenceValue = gonuOutline;
            gamesProp.GetArrayElementAtIndex(3).objectReferenceValue = m10Outline;
            gamesProp.GetArrayElementAtIndex(4).objectReferenceValue = m22Outline;
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
