using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using Zenject;
using MasterFramework.Core;
using MasterFramework.API;
using MasterFramework.Services;
using System.Reflection;

namespace MasterFramework.Navigation
{
    /// <summary>
    /// 마스터 샌드박스의 최상위 오케스트레이터: 마스터 프레임 HUD, 스텝 탭, 룰 모달, 피드백 오버레이 및 테크 인스펙터 총괄
    /// </summary>
    public class UniversalStageManager : MonoBehaviour
    {
        public static PortfolioGameOutline SelectedGame;

        [Header("Global Configuration")]
        [SerializeField] private LessonOutline lessonOutline;
        
        [Header("Content Container")]
        [SerializeField] private RectTransform contentRoot;
        [SerializeField] private GameObject defaultTemplatePrefab;

        // Injected Services (Zenject)
        private IAPIService _apiService;
        private LessonSessionContext _sessionContext;
        
        // Reactive State
        public readonly ReactiveProperty<int> CurrentStepIndex = new ReactiveProperty<int>(0);
        private readonly List<IStageOrganizer> _instantiatedStages = new List<IStageOrganizer>();
        private IStageOrganizer _activeStage;
        private int _activeStageIndex = -1;
        private readonly Dictionary<int, string> _stageStateCache = new Dictionary<int, string>();

        private StudyJsonData _studyJsonData;
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        
        // Master Frame Components (Auto-Created / Bound)
        private MasterFrameHUD _masterFrameHUD;
        private UniversalRuleModal _ruleModal;
        private UniversalFeedbackOverlay _feedbackOverlay;
        private TechInspectorPanel _techInspectorPanel;

        [Inject]
        public void Construct(IAPIService apiService, LessonSessionContext sessionContext)
        {
            _apiService = apiService;
            _sessionContext = sessionContext;
        }

        private async void Start()
        {
            if (SelectedGame != null)
            {
                lessonOutline = SelectedGame.lessonOutline;
                _sessionContext.lessonId = SelectedGame.gameId;
                _sessionContext.levelCode = SelectedGame.generationTag;
                Debug.Log($"[UniversalStageManager] Dynamic loading outline for: {SelectedGame.gameTitle}");
            }

            if (lessonOutline == null)
            {
                Debug.LogError("[UniversalStageManager] LessonOutline data asset is missing!");
                return;
            }

            CreateMasterFrameUI();
            await LoadInitialProgressAsync();
        }

        private void CreateMasterFrameUI()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindObjectOfType<Canvas>();
            if (canvas == null) return;

            Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // 1. Master Frame HUD (Top Navigation Bar)
            GameObject hudObj = new GameObject("MasterFrameHUD", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(MasterFrameHUD));
            hudObj.transform.SetParent(canvas.transform, false);

            RectTransform hudRt = hudObj.GetComponent<RectTransform>();
            hudRt.anchorMin = new Vector2(0, 1);
            hudRt.anchorMax = new Vector2(1, 1);
            hudRt.pivot = new Vector2(0.5f, 1);
            hudRt.anchoredPosition = Vector2.zero;
            hudRt.sizeDelta = new Vector2(0, 56);

            Image hudBg = hudObj.GetComponent<Image>();
            hudBg.color = new Color32(15, 23, 42, 235); // Dark Slate Slate-900

            // 1-A. Left Container (Lobby Button & Titles)
            GameObject leftContainer = new GameObject("LeftContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            leftContainer.transform.SetParent(hudObj.transform, false);
            RectTransform leftRt = leftContainer.GetComponent<RectTransform>();
            leftRt.anchorMin = new Vector2(0, 0);
            leftRt.anchorMax = new Vector2(0, 1);
            leftRt.pivot = new Vector2(0, 0.5f);
            leftRt.anchoredPosition = new Vector2(16, 0);
            leftRt.sizeDelta = new Vector2(340, 0);

            HorizontalLayoutGroup leftHlg = leftContainer.GetComponent<HorizontalLayoutGroup>();
            leftHlg.spacing = 12;
            leftHlg.childAlignment = TextAnchor.MiddleLeft;
            leftHlg.childControlWidth = false;
            leftHlg.childControlHeight = false;
            leftHlg.childForceExpandWidth = false;
            leftHlg.childForceExpandHeight = false;

            // Lobby Button
            GameObject lobbyBtnObj = new GameObject("LobbyButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            lobbyBtnObj.transform.SetParent(leftContainer.transform, false);
            lobbyBtnObj.GetComponent<RectTransform>().sizeDelta = new Vector2(92, 34);
            lobbyBtnObj.GetComponent<Image>().color = new Color32(239, 68, 68, 220); // Rose-500

            GameObject lobbyTxtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            lobbyTxtObj.transform.SetParent(lobbyBtnObj.transform, false);
            RectTransform lTxtRt = lobbyTxtObj.GetComponent<RectTransform>();
            lTxtRt.anchorMin = Vector2.zero; lTxtRt.anchorMax = Vector2.one; lTxtRt.sizeDelta = Vector2.zero;
            Text lTxt = lobbyTxtObj.GetComponent<Text>();
            lTxt.font = defaultFont; lTxt.text = "🏠 로비"; lTxt.fontSize = 13; lTxt.fontStyle = FontStyle.Bold;
            lTxt.color = Color.white; lTxt.alignment = TextAnchor.MiddleCenter;

            // Title Column
            GameObject titleCol = new GameObject("TitleColumn", typeof(RectTransform), typeof(VerticalLayoutGroup));
            titleCol.transform.SetParent(leftContainer.transform, false);
            titleCol.GetComponent<RectTransform>().sizeDelta = new Vector2(220, 44);
            VerticalLayoutGroup titleVlg = titleCol.GetComponent<VerticalLayoutGroup>();
            titleVlg.spacing = 2; titleVlg.childAlignment = TextAnchor.MiddleLeft;
            titleVlg.childControlWidth = true; titleVlg.childControlHeight = false;
            titleVlg.childForceExpandWidth = true; titleVlg.childForceExpandHeight = false;

            GameObject titleTxtObj = new GameObject("GameTitle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            titleTxtObj.transform.SetParent(titleCol.transform, false);
            Text titleTxt = titleTxtObj.GetComponent<Text>();
            titleTxt.font = defaultFont; titleTxt.fontSize = 14; titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.color = Color.white;

            GameObject subTxtObj = new GameObject("GameSubTitle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            subTxtObj.transform.SetParent(titleCol.transform, false);
            Text subTxt = subTxtObj.GetComponent<Text>();
            subTxt.font = defaultFont; subTxt.fontSize = 11;
            subTxt.color = new Color32(148, 163, 184, 255); // Slate-400

            // 1-B. Center Container (Step Tabs)
            GameObject centerContainer = new GameObject("TabsContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            centerContainer.transform.SetParent(hudObj.transform, false);
            RectTransform centerRt = centerContainer.GetComponent<RectTransform>();
            centerRt.anchorMin = new Vector2(0.5f, 0.5f);
            centerRt.anchorMax = new Vector2(0.5f, 0.5f);
            centerRt.pivot = new Vector2(0.5f, 0.5f);
            centerRt.anchoredPosition = Vector2.zero;
            centerRt.sizeDelta = new Vector2(500, 40);

            HorizontalLayoutGroup centerHlg = centerContainer.GetComponent<HorizontalLayoutGroup>();
            centerHlg.spacing = 8;
            centerHlg.childAlignment = TextAnchor.MiddleCenter;
            centerHlg.childControlWidth = false;
            centerHlg.childControlHeight = false;
            centerHlg.childForceExpandWidth = false;
            centerHlg.childForceExpandHeight = false;

            // 1-C. Right Container (Toolbar Buttons)
            GameObject rightContainer = new GameObject("RightContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            rightContainer.transform.SetParent(hudObj.transform, false);
            RectTransform rightRt = rightContainer.GetComponent<RectTransform>();
            rightRt.anchorMin = new Vector2(1, 0);
            rightRt.anchorMax = new Vector2(1, 1);
            rightRt.pivot = new Vector2(1, 0.5f);
            rightRt.anchoredPosition = new Vector2(-16, 0);
            rightRt.sizeDelta = new Vector2(340, 0);

            HorizontalLayoutGroup rightHlg = rightContainer.GetComponent<HorizontalLayoutGroup>();
            rightHlg.spacing = 8;
            rightHlg.childAlignment = TextAnchor.MiddleRight;
            rightHlg.childControlWidth = false;
            rightHlg.childControlHeight = false;
            rightHlg.childForceExpandWidth = false;
            rightHlg.childForceExpandHeight = false;

            // Refresh Button
            GameObject refBtnObj = CreateToolbarButton(rightContainer.transform, "RefreshButton", "🔄 새로고침", new Color32(51, 65, 85, 220), new Vector2(90, 34));
            // Rule Button
            GameObject ruleBtnObj = CreateToolbarButton(rightContainer.transform, "RuleButton", "📖 규칙", new Color32(30, 41, 59, 220), new Vector2(76, 34));
            // Tech Inspector Button
            GameObject techBtnObj = CreateToolbarButton(rightContainer.transform, "TechButton", "⚙️ 테크", new Color32(14, 165, 233, 220), new Vector2(76, 34));

            // Bind MasterFrameHUD fields via reflection/serialization
            _masterFrameHUD = hudObj.GetComponent<MasterFrameHUD>();
            var hudType = typeof(MasterFrameHUD);
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            hudType.GetField("headerPanel", flags)?.SetValue(_masterFrameHUD, hudRt);
            hudType.GetField("gameTitleText", flags)?.SetValue(_masterFrameHUD, titleTxt);
            hudType.GetField("gameSubTitleText", flags)?.SetValue(_masterFrameHUD, subTxt);
            hudType.GetField("tabsContainer", flags)?.SetValue(_masterFrameHUD, centerRt);
            hudType.GetField("lobbyButton", flags)?.SetValue(_masterFrameHUD, lobbyBtnObj.GetComponent<Button>());
            hudType.GetField("refreshButton", flags)?.SetValue(_masterFrameHUD, refBtnObj.GetComponent<Button>());
            hudType.GetField("ruleButton", flags)?.SetValue(_masterFrameHUD, ruleBtnObj.GetComponent<Button>());
            hudType.GetField("techInspectorButton", flags)?.SetValue(_masterFrameHUD, techBtnObj.GetComponent<Button>());

            _masterFrameHUD.OnStepTabClicked = (stepIdx) =>
            {
                AudioService.Instance.PlaySfx(SfxType.TabSwitch);
                TransitionToStepAsync(stepIdx).Forget();
            };
            _masterFrameHUD.OnLobbyClicked = () =>
            {
                AudioService.Instance.PlaySfx(SfxType.Click);
                ReturnToLobby();
            };
            _masterFrameHUD.OnRefreshClicked = () =>
            {
                AudioService.Instance.PlaySfx(SfxType.Click);
                RefreshActiveStage();
            };
            _masterFrameHUD.OnRuleClicked = () =>
            {
                AudioService.Instance.PlaySfx(SfxType.Click);
                ShowRuleModal();
            };
            _masterFrameHUD.OnTechInspectorClicked = () =>
            {
                AudioService.Instance.PlaySfx(SfxType.Click);
                ToggleTechInspector();
            };
            _masterFrameHUD.OnUndoClicked = () => PerformUndo();
            _masterFrameHUD.OnRedoClicked = () => PerformRedo();
            _masterFrameHUD.OnSoundToggleClicked = () => ToggleSound();
            _masterFrameHUD.SetSoundMuted(AudioService.Instance.IsMuted);

            // 2. Universal Rule Modal
            CreateRuleModalUI(canvas.transform);

            // 3. Universal Feedback Overlay
            CreateFeedbackOverlayUI(canvas.transform);

            // 4. Tech Inspector Panel
            CreateTechInspectorUI(canvas.transform);
        }

        private GameObject CreateToolbarButton(Transform parent, string name, string label, Color32 color, Vector2 size)
        {
            Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(parent, false);
            btnObj.GetComponent<RectTransform>().sizeDelta = size;
            btnObj.GetComponent<Image>().color = color;

            GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            txtObj.transform.SetParent(btnObj.transform, false);
            RectTransform txtRt = txtObj.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one; txtRt.sizeDelta = Vector2.zero;
            Text txt = txtObj.GetComponent<Text>();
            txt.font = defaultFont; txt.text = label; txt.fontSize = 12; txt.fontStyle = FontStyle.Bold;
            txt.color = Color.white; txt.alignment = TextAnchor.MiddleCenter;

            return btnObj;
        }

        private void CreateRuleModalUI(Transform canvasTransform)
        {
            Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            GameObject modalObj = new GameObject("UniversalRuleModal", typeof(RectTransform), typeof(CanvasRenderer), typeof(UniversalRuleModal));
            modalObj.transform.SetParent(canvasTransform, false);
            RectTransform modalRootRt = modalObj.GetComponent<RectTransform>();
            modalRootRt.anchorMin = Vector2.zero; modalRootRt.anchorMax = Vector2.one; modalRootRt.sizeDelta = Vector2.zero;

            // Overlay Background Button
            GameObject overlayObj = new GameObject("OverlayBackdrop", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            overlayObj.transform.SetParent(modalObj.transform, false);
            RectTransform ovRt = overlayObj.GetComponent<RectTransform>();
            ovRt.anchorMin = Vector2.zero; ovRt.anchorMax = Vector2.one; ovRt.sizeDelta = Vector2.zero;
            Image ovImg = overlayObj.GetComponent<Image>();
            ovImg.color = new Color(0, 0, 0, 0.7f);

            // Centered Modal Container
            GameObject containerObj = new GameObject("ModalContainer", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            containerObj.transform.SetParent(modalObj.transform, false);
            RectTransform cRt = containerObj.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0.5f, 0.5f); cRt.anchorMax = new Vector2(0.5f, 0.5f);
            cRt.pivot = new Vector2(0.5f, 0.5f); cRt.sizeDelta = new Vector2(560, 420);
            containerObj.GetComponent<Image>().color = new Color32(15, 23, 42, 250);

            // Title
            GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            titleObj.transform.SetParent(containerObj.transform, false);
            RectTransform tRt = titleObj.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0.5f, 1); tRt.anchorMax = new Vector2(0.5f, 1);
            tRt.pivot = new Vector2(0.5f, 1); tRt.anchoredPosition = new Vector2(0, -20);
            tRt.sizeDelta = new Vector2(500, 32);
            Text titleTxt = titleObj.GetComponent<Text>();
            titleTxt.font = defaultFont; titleTxt.fontSize = 18; titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.color = Color.white; titleTxt.alignment = TextAnchor.MiddleCenter;

            // Description
            GameObject descObj = new GameObject("DescText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            descObj.transform.SetParent(containerObj.transform, false);
            RectTransform dRt = descObj.GetComponent<RectTransform>();
            dRt.anchorMin = new Vector2(0.5f, 0.5f); dRt.anchorMax = new Vector2(0.5f, 0.5f);
            dRt.pivot = new Vector2(0.5f, 0.5f); dRt.anchoredPosition = new Vector2(0, -10);
            dRt.sizeDelta = new Vector2(480, 260);
            Text descTxt = descObj.GetComponent<Text>();
            descTxt.font = defaultFont; descTxt.fontSize = 15;
            descTxt.color = new Color32(226, 232, 240, 255); descTxt.alignment = TextAnchor.UpperLeft;

            // Close Button
            GameObject closeBtnObj = CreateToolbarButton(containerObj.transform, "CloseButton", "닫기", new Color32(51, 65, 85, 230), new Vector2(100, 36));
            RectTransform closeRt = closeBtnObj.GetComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(0.5f, 0); closeRt.anchorMax = new Vector2(0.5f, 0);
            closeRt.pivot = new Vector2(0.5f, 0); closeRt.anchoredPosition = new Vector2(0, 18);

            _ruleModal = modalObj.GetComponent<UniversalRuleModal>();
            var modalType = typeof(UniversalRuleModal);
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            modalType.GetField("modalContainer", flags)?.SetValue(_ruleModal, cRt);
            modalType.GetField("overlayBackground", flags)?.SetValue(_ruleModal, ovImg);
            modalType.GetField("titleText", flags)?.SetValue(_ruleModal, titleTxt);
            modalType.GetField("descriptionText", flags)?.SetValue(_ruleModal, descTxt);
            modalType.GetField("closeButton", flags)?.SetValue(_ruleModal, closeBtnObj.GetComponent<Button>());
            modalType.GetField("overlayButton", flags)?.SetValue(_ruleModal, overlayObj.GetComponent<Button>());
            _ruleModal.BindButtons();

            modalObj.SetActive(false);
        }

        private void CreateFeedbackOverlayUI(Transform canvasTransform)
        {
            Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            GameObject fbObj = new GameObject("UniversalFeedbackOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(CanvasGroup), typeof(UniversalFeedbackOverlay));
            fbObj.transform.SetParent(canvasTransform, false);
            RectTransform fbRt = fbObj.GetComponent<RectTransform>();
            fbRt.anchorMin = Vector2.zero; fbRt.anchorMax = Vector2.one; fbRt.sizeDelta = Vector2.zero;

            // Centered Stamp Box
            GameObject stampBox = new GameObject("StampBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            stampBox.transform.SetParent(fbObj.transform, false);
            RectTransform sRt = stampBox.GetComponent<RectTransform>();
            sRt.anchorMin = new Vector2(0.5f, 0.5f); sRt.anchorMax = new Vector2(0.5f, 0.5f);
            sRt.pivot = new Vector2(0.5f, 0.5f); sRt.sizeDelta = new Vector2(360, 160);
            Image sImg = stampBox.GetComponent<Image>();
            sImg.color = new Color32(20, 83, 45, 240);

            // Stamp Text
            GameObject stTxtObj = new GameObject("StampText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            stTxtObj.transform.SetParent(stampBox.transform, false);
            RectTransform stRt = stTxtObj.GetComponent<RectTransform>();
            stRt.anchorMin = new Vector2(0.5f, 0.5f); stRt.anchorMax = new Vector2(0.5f, 0.5f);
            stRt.pivot = new Vector2(0.5f, 0.5f); stRt.anchoredPosition = new Vector2(0, 16);
            stRt.sizeDelta = new Vector2(320, 48);
            Text stTxt = stTxtObj.GetComponent<Text>();
            stTxt.font = defaultFont; stTxt.text = "STAGE CLEAR!"; stTxt.fontSize = 28; stTxt.fontStyle = FontStyle.Bold;
            stTxt.color = new Color32(74, 222, 128, 255); stTxt.alignment = TextAnchor.MiddleCenter;

            // Sub Text
            GameObject subTxtObj = new GameObject("SubText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            subTxtObj.transform.SetParent(stampBox.transform, false);
            RectTransform subRt = subTxtObj.GetComponent<RectTransform>();
            subRt.anchorMin = new Vector2(0.5f, 0.5f); subRt.anchorMax = new Vector2(0.5f, 0.5f);
            subRt.pivot = new Vector2(0.5f, 0.5f); subRt.anchoredPosition = new Vector2(0, -26);
            subRt.sizeDelta = new Vector2(320, 30);
            Text subTxt = subTxtObj.GetComponent<Text>();
            subTxt.font = defaultFont; subTxt.text = "PERFECT SOLUTION ★★★"; subTxt.fontSize = 14;
            subTxt.color = Color.white; subTxt.alignment = TextAnchor.MiddleCenter;

            _feedbackOverlay = fbObj.GetComponent<UniversalFeedbackOverlay>();
            var fbType = typeof(UniversalFeedbackOverlay);
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            fbType.GetField("stampContainer", flags)?.SetValue(_feedbackOverlay, sRt);
            fbType.GetField("stampImage", flags)?.SetValue(_feedbackOverlay, sImg);
            fbType.GetField("stampText", flags)?.SetValue(_feedbackOverlay, stTxt);
            fbType.GetField("stampSubText", flags)?.SetValue(_feedbackOverlay, subTxt);
            fbType.GetField("canvasGroup", flags)?.SetValue(_feedbackOverlay, fbObj.GetComponent<CanvasGroup>());

            _feedbackOverlay.OnStampShown = (type) =>
            {
                if (type == StampType.Complete)
                {
                    string gId = SelectedGame != null ? SelectedGame.gameId : "Default";
                    PortfolioSaveService.Instance.SetStageCleared(gId, CurrentStepIndex.Value);
                    PortfolioSaveService.Instance.Save();
                    if (_masterFrameHUD != null)
                    {
                        _masterFrameHUD.UpdateTabVisuals(gId, CurrentStepIndex.Value);
                    }
                }
            };

            fbObj.SetActive(false);
        }

        private void CreateTechInspectorUI(Transform canvasTransform)
        {
            Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            GameObject inspObj = new GameObject("TechInspectorPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(TechInspectorPanel));
            inspObj.transform.SetParent(canvasTransform, false);
            RectTransform inspRt = inspObj.GetComponent<RectTransform>();
            inspRt.anchorMin = Vector2.zero; inspRt.anchorMax = Vector2.one; inspRt.sizeDelta = Vector2.zero;

            // Backdrop
            GameObject ovObj = new GameObject("Backdrop", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            ovObj.transform.SetParent(inspObj.transform, false);
            RectTransform ovRt = ovObj.GetComponent<RectTransform>();
            ovRt.anchorMin = Vector2.zero; ovRt.anchorMax = Vector2.one; ovRt.sizeDelta = Vector2.zero;
            Image ovImg = ovObj.GetComponent<Image>();
            ovImg.color = new Color(0, 0, 0, 0.5f);

            // Right Slide Drawer Panel
            GameObject drawerObj = new GameObject("DrawerPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            drawerObj.transform.SetParent(inspObj.transform, false);
            RectTransform dRt = drawerObj.GetComponent<RectTransform>();
            dRt.anchorMin = new Vector2(1, 0); dRt.anchorMax = new Vector2(1, 1);
            dRt.pivot = new Vector2(1, 0.5f); dRt.anchoredPosition = new Vector2(380, 0);
            dRt.sizeDelta = new Vector2(360, 0);
            drawerObj.GetComponent<Image>().color = new Color32(15, 23, 42, 250);

            // Header Row
            GameObject headerTxtObj = new GameObject("HeaderTitle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            headerTxtObj.transform.SetParent(drawerObj.transform, false);
            RectTransform hRt = headerTxtObj.GetComponent<RectTransform>();
            hRt.anchorMin = new Vector2(0.5f, 1); hRt.anchorMax = new Vector2(0.5f, 1);
            hRt.pivot = new Vector2(0.5f, 1); hRt.anchoredPosition = new Vector2(0, -18);
            hRt.sizeDelta = new Vector2(320, 30);
            Text hTxt = headerTxtObj.GetComponent<Text>();
            hTxt.font = defaultFont; hTxt.text = "⚙️ TECH INSPECTOR"; hTxt.fontSize = 17; hTxt.fontStyle = FontStyle.Bold;
            hTxt.color = new Color32(56, 189, 248, 255); hTxt.alignment = TextAnchor.MiddleLeft;

            // Badges Container
            GameObject badgesObj = new GameObject("BadgesContainer", typeof(RectTransform), typeof(VerticalLayoutGroup));
            badgesObj.transform.SetParent(drawerObj.transform, false);
            RectTransform bRt = badgesObj.GetComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0.5f, 1); bRt.anchorMax = new Vector2(0.5f, 1);
            bRt.pivot = new Vector2(0.5f, 1); bRt.anchoredPosition = new Vector2(0, -60);
            bRt.sizeDelta = new Vector2(320, 140);
            VerticalLayoutGroup bVlg = badgesObj.GetComponent<VerticalLayoutGroup>();
            bVlg.spacing = 6; bVlg.childAlignment = TextAnchor.UpperLeft;
            bVlg.childControlWidth = true; bVlg.childControlHeight = false;

            // Live Rule Status Row
            GameObject ruleStatusObj = new GameObject("RuleStatusText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            ruleStatusObj.transform.SetParent(drawerObj.transform, false);
            RectTransform rRt = ruleStatusObj.GetComponent<RectTransform>();
            rRt.anchorMin = new Vector2(0.5f, 1); rRt.anchorMax = new Vector2(0.5f, 1);
            rRt.pivot = new Vector2(0.5f, 1); rRt.anchoredPosition = new Vector2(0, -210);
            rRt.sizeDelta = new Vector2(320, 30);
            Text rTxt = ruleStatusObj.GetComponent<Text>();
            rTxt.font = defaultFont; rTxt.text = "Rule Validation: IN PROGRESS"; rTxt.fontSize = 13;
            rTxt.color = new Color32(251, 146, 60, 255);

            // Action Buttons
            GameObject autoBtn = CreateToolbarButton(drawerObj.transform, "AutoSolveBtn", "⚡ Auto Solve (정답 시연)", new Color32(16, 185, 129, 230), new Vector2(320, 42));
            RectTransform autoRt = autoBtn.GetComponent<RectTransform>();
            autoRt.anchorMin = new Vector2(0.5f, 0); autoRt.anchorMax = new Vector2(0.5f, 0);
            autoRt.pivot = new Vector2(0.5f, 0); autoRt.anchoredPosition = new Vector2(0, 110);

            GameObject compAllBtn = CreateToolbarButton(drawerObj.transform, "CompAllBtn", "🏆 올클리어 (All Stages Clear)", new Color32(59, 130, 246, 230), new Vector2(320, 38));
            RectTransform compRt = compAllBtn.GetComponent<RectTransform>();
            compRt.anchorMin = new Vector2(0.5f, 0); compRt.anchorMax = new Vector2(0.5f, 0);
            compRt.pivot = new Vector2(0.5f, 0); compRt.anchoredPosition = new Vector2(0, 62);

            GameObject resetBtn = CreateToolbarButton(drawerObj.transform, "ResetBtn", "🗑️ 진행도 초기화", new Color32(100, 116, 139, 200), new Vector2(320, 34));
            RectTransform resetRt = resetBtn.GetComponent<RectTransform>();
            resetRt.anchorMin = new Vector2(0.5f, 0); resetRt.anchorMax = new Vector2(0.5f, 0);
            resetRt.pivot = new Vector2(0.5f, 0); resetRt.anchoredPosition = new Vector2(0, 20);

            _techInspectorPanel = inspObj.GetComponent<TechInspectorPanel>();
            var inspType = typeof(TechInspectorPanel);
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            inspType.GetField("drawerPanel", flags)?.SetValue(_techInspectorPanel, dRt);
            inspType.GetField("overlayBackground", flags)?.SetValue(_techInspectorPanel, ovImg);
            inspType.GetField("closeButton", flags)?.SetValue(_techInspectorPanel, ovObj.GetComponent<Button>());
            inspType.GetField("overlayButton", flags)?.SetValue(_techInspectorPanel, ovObj.GetComponent<Button>());
            inspType.GetField("gameTitleText", flags)?.SetValue(_techInspectorPanel, hTxt);
            inspType.GetField("generationText", flags)?.SetValue(_techInspectorPanel, rTxt);
            inspType.GetField("badgesContainer", flags)?.SetValue(_techInspectorPanel, bRt);
            inspType.GetField("activeStageText", flags)?.SetValue(_techInspectorPanel, rTxt);
            inspType.GetField("ruleStateText", flags)?.SetValue(_techInspectorPanel, rTxt);
            inspType.GetField("autoSolveButton", flags)?.SetValue(_techInspectorPanel, autoBtn.GetComponent<Button>());
            inspType.GetField("completeAllButton", flags)?.SetValue(_techInspectorPanel, compAllBtn.GetComponent<Button>());
            inspType.GetField("resetProgressButton", flags)?.SetValue(_techInspectorPanel, resetBtn.GetComponent<Button>());

            _techInspectorPanel.OnAutoSolveRequested = () => TriggerAutoSolve();
            _techInspectorPanel.OnCompleteAllRequested = () => CompleteAllStages();
            _techInspectorPanel.OnResetProgressRequested = () => ResetAllProgress();
            _techInspectorPanel.BindButtons();

            inspObj.SetActive(false);
        }

        private void ShowRuleModal()
        {
            // 저작권 보호 및 포트폴리오 UI 일관성을 위해 표준 모던 글래스모피즘 모달을 전용으로 사용
            if (_ruleModal != null)
            {
                string title = SelectedGame != null ? SelectedGame.gameTitle : "미니게임";
                string desc = SelectedGame != null ? SelectedGame.description : "";
                _ruleModal.Show(title, desc);
            }
        }

        private void ToggleTechInspector()
        {
            if (_techInspectorPanel != null && lessonOutline != null && CurrentStepIndex.Value < lessonOutline.steps.Count)
            {
                string stepName = lessonOutline.steps[CurrentStepIndex.Value].stepName;
                _techInspectorPanel.Toggle(SelectedGame, CurrentStepIndex.Value, stepName);
            }
        }

        private void TriggerAutoSolve()
        {
            if (_activeStage != null)
            {
                var stageType = _activeStage.GetType();
                var autoMethod = stageType.GetMethod("AutoSolve", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (autoMethod != null)
                {
                    autoMethod.Invoke(_activeStage, null);
                }
                else
                {
                    // Fallback: If no auto-solve method, trigger Stage Clear
                    HandleStageClearAsync(CurrentStepIndex.Value).Forget();
                    if (_feedbackOverlay != null)
                    {
                        _feedbackOverlay.ShowStampAsync(StampType.Complete).Forget();
                    }
                }
            }
            if (_techInspectorPanel != null) _techInspectorPanel.Close();
        }

        private void CompleteAllStages()
        {
            if (lessonOutline == null || lessonOutline.steps == null) return;
            string gId = SelectedGame != null ? SelectedGame.gameId : "Default";

            for (int i = 0; i < lessonOutline.steps.Count; i++)
            {
                PortfolioSaveService.Instance.SetStageCleared(gId, i);
            }
            PortfolioSaveService.Instance.Save();

            if (_masterFrameHUD != null)
            {
                _masterFrameHUD.UpdateTabVisuals(gId, CurrentStepIndex.Value);
            }

            if (_feedbackOverlay != null)
            {
                _feedbackOverlay.ShowStampAsync(StampType.Complete).Forget();
            }
            if (_techInspectorPanel != null) _techInspectorPanel.Close();
        }

        private void ResetAllProgress()
        {
            string gId = SelectedGame != null ? SelectedGame.gameId : "Default";
            PortfolioSaveService.Instance.ResetAllProgress();
            PortfolioSaveService.Instance.Save();

            if (_masterFrameHUD != null)
            {
                _masterFrameHUD.UpdateTabVisuals(gId, CurrentStepIndex.Value);
            }
            if (_techInspectorPanel != null) _techInspectorPanel.Close();
        }

        private async UniTask LoadInitialProgressAsync()
        {
            try
            {
                var lessonData = await _apiService.GetLessonInfoAsync(_sessionContext);
                _studyJsonData = new StudyJsonData();
                _stageStateCache.Clear();

                CurrentStepIndex.Value = 0;
                await TransitionToStepAsync(CurrentStepIndex.Value);

                if (_masterFrameHUD != null)
                {
                    _masterFrameHUD.Initialize(SelectedGame, lessonOutline, CurrentStepIndex.Value);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UniversalStageManager] Initialization error: {ex.Message}");
            }
        }

        public async UniTask TransitionToStepAsync(int stepIndex)
        {
            if (stepIndex < 0 || stepIndex >= lessonOutline.steps.Count) return;

            var stepInfo = lessonOutline.steps[stepIndex];

            // 1. Save and cleanup previous active stage
            if (_activeStage != null && _activeStageIndex >= 0)
            {
                if (_activeStage is IStateRestorable restorableStage)
                {
                    try
                    {
                        string state = restorableStage.SerializeState();
                        _stageStateCache[_activeStageIndex] = state;
                        UpdatePersistentStageState(_activeStageIndex, state);

                        string gId = SelectedGame != null ? SelectedGame.gameId : "Default";
                        PortfolioSaveService.Instance.SetStageState(gId, _activeStageIndex, state);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[UniversalStageManager] State serialization skipped: {ex.Message}");
                    }
                }

                await _activeStage.ExitStageAsync();
                var activeGo = ((MonoBehaviour)_activeStage).gameObject;
                if (activeGo != null)
                {
                    Destroy(activeGo);
                }
                _activeStage = null;
            }

            // 2. Instantiate new stage
            GameObject instance = null;
            if (stepInfo.isDynamicGeneration)
            {
                var template = stepInfo.stageTemplatePrefab != null 
                    ? stepInfo.stageTemplatePrefab 
                    : (stepInfo.stagePrefab != null ? stepInfo.stagePrefab : defaultTemplatePrefab);
                if (template != null)
                {
                    instance = Instantiate(template, contentRoot);
                    _activeStage = instance.GetComponent<IStageOrganizer>();
                    if (_activeStage != null)
                    {
                        _activeStage.SetStageData(stepInfo.levelData);
                    }
                }
            }
            else if (stepInfo.stagePrefab != null)
            {
                instance = Instantiate(stepInfo.stagePrefab, contentRoot);
                _activeStage = instance.GetComponent<IStageOrganizer>();
            }

            if (instance != null)
            {
                var rect = instance.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.one;
                    rect.anchoredPosition = Vector2.zero;
                    rect.sizeDelta = Vector2.zero;
                    rect.localScale = Vector3.one;
                }
            }

            if (_activeStage != null)
            {
                await _activeStage.InitializeStageAsync(stepIndex);

                // Restore previous state if cached
                if (_stageStateCache.TryGetValue(stepIndex, out string cachedState))
                {
                    if (_activeStage is IStateRestorable restorable)
                    {
                        restorable.DeserializeAndRestoreState(cachedState);
                    }
                }

                await _activeStage.EnterStageAsync();
                HookActiveStageUndo(_activeStage);

                _activeStage.IsStageCleared
                    .Where(isCleared => isCleared)
                    .Take(1)
                    .Subscribe(_ =>
                    {
                        AudioService.Instance.PlaySfx(SfxType.StageClear);
                        HandleStageClearAsync(stepIndex).Forget();
                        if (_feedbackOverlay != null)
                        {
                            _feedbackOverlay.ShowStampAsync(StampType.Complete).Forget();
                        }
                    })
                    .AddTo(_disposables);
            }

            _activeStageIndex = stepIndex;
            CurrentStepIndex.Value = stepIndex;

            string gameId = SelectedGame != null ? SelectedGame.gameId : "Default";
            if (_masterFrameHUD != null)
            {
                _masterFrameHUD.UpdateTabVisuals(gameId, stepIndex);
            }
        }

        private IDisposable _undoHistorySub;

        private void HookActiveStageUndo(IStageOrganizer stage)
        {
            _undoHistorySub?.Dispose();
            _undoHistorySub = null;

            if (stage is IUndoableStage undoable)
            {
                _undoHistorySub = undoable.OnHistoryChanged
                    .Subscribe(_ =>
                    {
                        if (_masterFrameHUD != null)
                        {
                            _masterFrameHUD.SetUndoRedoState(undoable.CanUndo, undoable.CanRedo);
                        }
                    });

                if (_masterFrameHUD != null)
                {
                    _masterFrameHUD.SetUndoRedoState(undoable.CanUndo, undoable.CanRedo);
                }
            }
            else
            {
                if (_masterFrameHUD != null)
                {
                    _masterFrameHUD.SetUndoRedoState(false, false);
                }
            }
        }

        private void PerformUndo()
        {
            if (_activeStage is IUndoableStage undoable && undoable.CanUndo)
            {
                undoable.Undo();
                AudioService.Instance.PlaySfx(SfxType.Undo);
            }
        }

        private void PerformRedo()
        {
            if (_activeStage is IUndoableStage undoable && undoable.CanRedo)
            {
                undoable.Redo();
                AudioService.Instance.PlaySfx(SfxType.Redo);
            }
        }

        private void ToggleSound()
        {
            AudioService.Instance.ToggleMute();
            if (_masterFrameHUD != null)
            {
                _masterFrameHUD.SetSoundMuted(AudioService.Instance.IsMuted);
            }
            if (!AudioService.Instance.IsMuted)
            {
                AudioService.Instance.PlaySfx(SfxType.Click);
            }
        }

        private void Update()
        {
            // Keyboard shortcuts using Unity.InputSystem if available, fallback gracefully
            // All shortcuts require holding Ctrl to avoid interfering with puzzle gameplay inputs
#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null)
            {
                bool isCtrl = kb.leftCtrlKey.isPressed || kb.rightCtrlKey.isPressed;
                if (isCtrl)
                {
                    if (kb.zKey.wasPressedThisFrame)
                    {
                        PerformUndo();
                    }
                    else if (kb.yKey.wasPressedThisFrame)
                    {
                        PerformRedo();
                    }
                    else if (kb.mKey.wasPressedThisFrame)
                    {
                        ToggleSound();
                    }
                    else if (kb.tabKey.wasPressedThisFrame || kb.tKey.wasPressedThisFrame)
                    {
                        ToggleTechInspector();
                    }
                    else if (kb.rKey.wasPressedThisFrame)
                    {
                        RefreshActiveStage();
                    }
                    else if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame)
                    {
                        TransitionToStepAsync(0).Forget();
                    }
                    else if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame)
                    {
                        TransitionToStepAsync(1).Forget();
                    }
                    else if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame)
                    {
                        TransitionToStepAsync(2).Forget();
                    }
                    else if (kb.digit4Key.wasPressedThisFrame || kb.numpad4Key.wasPressedThisFrame)
                    {
                        TransitionToStepAsync(3).Forget();
                    }
                }
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            // Legacy Input Fallback (All shortcuts require Ctrl)
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
                if (Input.GetKeyDown(KeyCode.Z)) PerformUndo();
                else if (Input.GetKeyDown(KeyCode.Y)) PerformRedo();
                else if (Input.GetKeyDown(KeyCode.M)) ToggleSound();
                else if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.T)) ToggleTechInspector();
                else if (Input.GetKeyDown(KeyCode.R)) RefreshActiveStage();
                else if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) TransitionToStepAsync(0).Forget();
                else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) TransitionToStepAsync(1).Forget();
                else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) TransitionToStepAsync(2).Forget();
                else if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4)) TransitionToStepAsync(3).Forget();
            }
#endif
        }

        private void UpdatePersistentStageState(int stageIndex, string stateData)
        {
            if (_studyJsonData.stage_states == null)
            {
                _studyJsonData.stage_states = new List<StageStateItem>();
            }

            var existing = _studyJsonData.stage_states.Find(x => x.stageIndex == stageIndex);
            if (existing != null)
            {
                existing.stateData = stateData;
            }
            else
            {
                _studyJsonData.stage_states.Add(new StageStateItem(stageIndex, stateData));
            }
        }

        private void RefreshActiveStage()
        {
            if (_activeStage != null)
            {
                _activeStage.RefreshStage();
            }
        }

        private async UniTask HandleStageClearAsync(int stepIndex)
        {
            string gId = SelectedGame != null ? SelectedGame.gameId : "Default";
            PortfolioSaveService.Instance.SetStageCleared(gId, stepIndex);
            PortfolioSaveService.Instance.Save();

            if (_masterFrameHUD != null)
            {
                _masterFrameHUD.UpdateTabVisuals(gId, stepIndex);
            }

            await UniTask.CompletedTask;
        }

        public void ReturnToLobby()
        {
            if (_activeStage != null && _activeStageIndex >= 0 && _activeStage is IStateRestorable restorableStage)
            {
                try
                {
                    string serialized = restorableStage.SerializeState();
                    string gId = SelectedGame != null ? SelectedGame.gameId : "Default";
                    PortfolioSaveService.Instance.SetStageState(gId, _activeStageIndex, serialized);
                }
                catch {}
            }

            PortfolioSaveService.Instance.Save();
            UnityEngine.SceneManagement.SceneManager.LoadScene("LobbyScene");
        }

        private void OnDestroy()
        {
            _undoHistorySub?.Dispose();
            _disposables.Dispose();
        }
    }
}
