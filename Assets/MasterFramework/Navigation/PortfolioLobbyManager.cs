using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UniRx;
using UniRx.Triggers;
using DG.Tweening;
using Cysharp.Threading.Tasks;
using MasterFramework.Services;

namespace MasterFramework.Navigation
{
    public class PortfolioLobbyManager : MonoBehaviour
    {
        [Header("Games Database")]
        [SerializeField] private List<PortfolioGameOutline> availableGames;

        [Header("UI References")]
        [SerializeField] private Transform cardParent;
        [SerializeField] private GameObject cardPrefab;

        [Header("Header Actions")]
        [SerializeField] private Button githubButton;
        [SerializeField] private Button archOverviewButton;

        private GameObject _archModalGo;
        private CompositeDisposable _disposables = new CompositeDisposable();

        private ScrollRect _scrollRect;

        private void Start()
        {
            EnsureEventSystemInputModule();
            EnsureScrollViewSetup();
            BuildDashboardLayout();
            InitializeListeners();
        }

        private void Update()
        {
            HandleMouseWheelScroll();
        }

        private void EnsureEventSystemInputModule()
        {
            var eventSystem = FindObjectOfType<EventSystem>();
            if (eventSystem == null)
            {
                var esGo = new GameObject("EventSystem", typeof(EventSystem));
                esGo.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>().AssignDefaultActions();
            }
            else
            {
                var inputSysModule = eventSystem.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                if (inputSysModule != null && inputSysModule.actionsAsset == null)
                {
                    inputSysModule.AssignDefaultActions();
                }
            }
        }

        private void EnsureScrollViewSetup()
        {
            if (cardParent == null) return;

            ScrollRect existingScroll = cardParent.GetComponentInParent<ScrollRect>();
            if (existingScroll != null)
            {
                _scrollRect = existingScroll;
                return;
            }

            // cardParent의 원래 위치와 앵커 보존
            RectTransform cpRt = cardParent.GetComponent<RectTransform>();
            Transform originalParent = cardParent.parent;
            Vector2 anchorMin = cpRt.anchorMin;
            Vector2 anchorMax = cpRt.anchorMax;
            Vector2 anchoredPosition = cpRt.anchoredPosition;
            Vector2 sizeDelta = cpRt.sizeDelta;

            // 1. ScrollView 루트 생성
            GameObject scrollViewGo = new GameObject("CardScrollView", typeof(RectTransform), typeof(ScrollRect));
            scrollViewGo.transform.SetParent(originalParent, false);
            RectTransform svRt = scrollViewGo.GetComponent<RectTransform>();
            svRt.anchorMin = anchorMin;
            svRt.anchorMax = anchorMax;
            svRt.anchoredPosition = anchoredPosition;
            svRt.sizeDelta = sizeDelta;
            svRt.pivot = new Vector2(0.5f, 0.5f);

            _scrollRect = scrollViewGo.GetComponent<ScrollRect>();
            _scrollRect.horizontal = true;
            _scrollRect.vertical = false;
            _scrollRect.movementType = ScrollRect.MovementType.Elastic;
            _scrollRect.elasticity = 0.1f;
            _scrollRect.scrollSensitivity = 40f;

            // 2. Viewport 생성 (마스크)
            GameObject viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewportGo.transform.SetParent(scrollViewGo.transform, false);
            RectTransform vpRt = viewportGo.GetComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero;
            vpRt.anchorMax = Vector2.one;
            vpRt.sizeDelta = Vector2.zero;
            vpRt.pivot = new Vector2(0, 0.5f);

            _scrollRect.viewport = vpRt;

            // 3. cardParent를 Viewport 자식으로 이전
            cardParent.SetParent(viewportGo.transform, false);
            _scrollRect.content = cpRt;

            // 4. cardParent RectTransform 셋업
            cpRt.anchorMin = new Vector2(0, 0);
            cpRt.anchorMax = new Vector2(0, 1);
            cpRt.pivot = new Vector2(0, 0.5f);
            cpRt.anchoredPosition = Vector2.zero;

            // 5. ContentSizeFitter 부착 (가로 콘텐츠 길이에 따라 자동 확장)
            var fitter = cardParent.gameObject.GetComponent<ContentSizeFitter>();
            if (fitter == null) fitter = cardParent.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            // 6. HorizontalLayoutGroup 정돈
            var hlg = cardParent.gameObject.GetComponent<HorizontalLayoutGroup>();
            if (hlg != null)
            {
                hlg.childAlignment = TextAnchor.MiddleLeft;
                hlg.padding = new RectOffset(20, 40, 0, 0);
                hlg.spacing = 20;
            }
        }

        private void HandleMouseWheelScroll()
        {
            if (_scrollRect == null) return;

            float wheel = 0f;
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Mouse.current != null)
            {
                wheel = UnityEngine.InputSystem.Mouse.current.scroll.ReadValue().y;
            }
#endif
            if (Mathf.Abs(wheel) > 0.01f)
            {
                // 마우스 휠 아래(-): 오른쪽으로 스크롤, 휠 위(+): 왼쪽으로 스크롤
                float step = (wheel > 0 ? -0.08f : 0.08f);
                _scrollRect.horizontalNormalizedPosition = Mathf.Clamp01(_scrollRect.horizontalNormalizedPosition + step);
            }
        }

        private void BuildDashboardLayout()
        {
            if (cardParent == null) return;

            // Clear old children
            for (int i = cardParent.childCount - 1; i >= 0; i--)
            {
                Destroy(cardParent.GetChild(i).gameObject);
            }

            if (availableGames == null || availableGames.Count == 0) return;

            foreach (var game in availableGames)
            {
                if (game == null) continue;

                // Create Card Container
                GameObject cardGo = new GameObject($"Card_{game.gameId}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                cardGo.transform.SetParent(cardParent, false);

                var cardRt = cardGo.GetComponent<RectTransform>();
                cardRt.sizeDelta = new Vector2(330, 410);

                var cardImg = cardGo.GetComponent<Image>();
                cardImg.color = new Color32(24, 30, 46, 250); // Dark sleek blue-gray
                cardImg.raycastTarget = true;

                // Card Outline / Border
                var outline = cardGo.AddComponent<Outline>();
                outline.effectColor = new Color32(51, 65, 85, 255);
                outline.effectDistance = new Vector2(2, -2);

                // Vertical Layout inside Card
                var vlg = cardGo.AddComponent<VerticalLayoutGroup>();
                vlg.padding = new RectOffset(18, 18, 18, 18);
                vlg.spacing = 8;
                vlg.childAlignment = TextAnchor.UpperCenter;
                vlg.childControlWidth = true;
                vlg.childControlHeight = false;

                // 1. Generation Tag & Clear Stars Bar (Header Row)
                int totalStages = game.totalStages > 0 ? game.totalStages : (game.lessonOutline != null ? game.lessonOutline.steps.Count : 4);
                int clearedCount = PortfolioSaveService.Instance != null ? PortfolioSaveService.Instance.GetClearedStageCount(game.gameId, totalStages) : 0;
                bool isCompleted = PortfolioSaveService.Instance != null && PortfolioSaveService.Instance.IsGameCompleted(game.gameId, totalStages);

                string starDisplay = isCompleted ? "★★★★ ALL" : (clearedCount > 0 ? $"{new string('★', clearedCount)}{new string('☆', totalStages - clearedCount)} ({clearedCount}/{totalStages})" : $"{new string('☆', totalStages)} (0/{totalStages})");
                Color starColor = isCompleted ? new Color32(250, 204, 21, 255) : (clearedCount > 0 ? new Color32(234, 179, 8, 255) : new Color32(100, 116, 139, 255));

                CreateHeaderRow(cardGo.transform, $"{game.generationTag.ToUpper()}  •  {totalStages} STAGES", starDisplay, starColor);

                // 2. Game Title & Subtitle
                CreateText(cardGo.transform, game.gameTitle, 18, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft, 26);
                if (!string.IsNullOrEmpty(game.subtitle))
                {
                    CreateText(cardGo.transform, game.subtitle, 12, FontStyle.Italic, new Color32(148, 163, 184, 255), TextAnchor.MiddleLeft, 16);
                }

                // 3. Divider Line
                CreateDivider(cardGo.transform, new Color32(51, 65, 85, 200), 1.5f);

                // 4. Core Mechanism Summary
                CreateText(cardGo.transform, "💡 핵심 규칙 & 메커니즘", 12, FontStyle.Bold, new Color32(226, 232, 240, 255), TextAnchor.MiddleLeft, 18);
                string summaryText = !string.IsNullOrEmpty(game.systemSummary) ? game.systemSummary : "규칙 제약 조건을 만족하는 논리 퍼즐입니다.";
                CreateText(cardGo.transform, summaryText, 11, FontStyle.Normal, new Color32(203, 213, 225, 255), TextAnchor.UpperLeft, 42);

                // 5. Tech Spec Badges (Verified CS/Engineering Chips)
                CreateText(cardGo.transform, "🏷️ 구현 엔지니어링 키워드", 12, FontStyle.Bold, new Color32(147, 197, 253, 255), TextAnchor.MiddleLeft, 18);
                CreateTechBadgeContainer(cardGo.transform, game);

                // 6. Flexible Spacer (UI 하단 정렬 보정)
                CreateSpacer(cardGo.transform, 6);

                // 7. Play / Enter Button
                GameObject btnGo = new GameObject("EnterButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                btnGo.transform.SetParent(cardGo.transform, false);
                var btnRt = btnGo.GetComponent<RectTransform>();
                btnRt.sizeDelta = new Vector2(294, 42);

                var btnImg = btnGo.GetComponent<Image>();
                btnImg.color = new Color32(37, 99, 235, 255); // Royal blue

                var btnOutline = btnGo.AddComponent<Outline>();
                btnOutline.effectColor = new Color32(96, 165, 250, 200);
                btnOutline.effectDistance = new Vector2(1, -1);

                CreateText(btnGo.transform, "▶  스테이지 입장", 14, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter, 42);

                // Button Click -> Start Game
                var targetGame = game;
                var btn = btnGo.GetComponent<Button>();
                btn.OnClickAsObservable()
                    .Subscribe(_ =>
                    {
                        btnGo.transform.DOPunchScale(new Vector3(0.08f, 0.08f, 0), 0.15f)
                            .OnComplete(() => StartSelectedGame(targetGame));
                    })
                    .AddTo(_disposables);

                // Card Hover Punch Animation
                var trigger = cardGo.AddComponent<EventTrigger>();
                var enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
                enterEntry.callback.AddListener((_) =>
                {
                    cardGo.transform.DOScale(1.025f, 0.15f).SetEase(Ease.OutQuad);
                    outline.effectColor = new Color32(56, 189, 248, 255); // Cyan glow
                });
                trigger.triggers.Add(enterEntry);

                var exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
                exitEntry.callback.AddListener((_) =>
                {
                    cardGo.transform.DOScale(1.0f, 0.15f).SetEase(Ease.OutQuad);
                    outline.effectColor = new Color32(51, 65, 85, 255);
                });
                trigger.triggers.Add(exitEntry);
            }
        }

        private void CreateHeaderRow(Transform parent, string tagText, string starText, Color starColor)
        {
            GameObject rowGo = new GameObject("HeaderRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            rowGo.transform.SetParent(parent, false);

            var rt = rowGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(294, 20);

            var hlg = rowGo.GetComponent<HorizontalLayoutGroup>();
            hlg.childControlWidth = false;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;

            var tagTxt = CreateText(rowGo.transform, tagText, 11, FontStyle.Bold, new Color32(56, 189, 248, 255), TextAnchor.MiddleLeft, 20);
            tagTxt.GetComponent<RectTransform>().sizeDelta = new Vector2(170, 20);

            var starTxt = CreateText(rowGo.transform, starText, 11, FontStyle.Bold, starColor, TextAnchor.MiddleRight, 20);
            starTxt.GetComponent<RectTransform>().sizeDelta = new Vector2(120, 20);
        }

        private void CreateTechBadgeContainer(Transform parent, PortfolioGameOutline game)
        {
            GameObject badgeRow = new GameObject("TechBadgeRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            badgeRow.transform.SetParent(parent, false);

            var rt = badgeRow.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(294, 30);

            var hlg = badgeRow.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = false;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;

            // 뱃지 텍스트 결정 (검증된 알고리즘 태그 매핑)
            string chip1 = "알고리즘 검증";
            string chip2 = "상태 머신 FSM";

            if (game.featurePoints != null && game.featurePoints.Length > 0 && !string.IsNullOrEmpty(game.featurePoints[0]))
            {
                chip1 = game.featurePoints[0];
            }
            if (game.featurePoints != null && game.featurePoints.Length > 1 && !string.IsNullOrEmpty(game.featurePoints[1]))
            {
                chip2 = game.featurePoints[1];
            }

            CreateSingleBadge(badgeRow.transform, chip1, new Color32(2, 132, 199, 255), new Color32(56, 189, 248, 255));
            CreateSingleBadge(badgeRow.transform, chip2, new Color32(124, 58, 237, 255), new Color32(192, 132, 252, 255));
        }

        private void CreateSingleBadge(Transform parent, string label, Color borderColor, Color textColor)
        {
            GameObject chipGo = new GameObject("Chip", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            chipGo.transform.SetParent(parent, false);

            var rt = chipGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(142, 28);

            var img = chipGo.GetComponent<Image>();
            img.color = new Color32(30, 41, 59, 255); // Dark Slate Blue

            var outline = chipGo.AddComponent<Outline>();
            outline.effectColor = borderColor;
            outline.effectDistance = new Vector2(1, -1);

            var txt = CreateText(chipGo.transform, label, 10, FontStyle.Bold, textColor, TextAnchor.MiddleCenter, 28);
            txt.GetComponent<RectTransform>().sizeDelta = new Vector2(138, 28);
        }

        private void CreateSpacer(Transform parent, float height)
        {
            GameObject spGo = new GameObject("Spacer", typeof(RectTransform));
            spGo.transform.SetParent(parent, false);
            var rt = spGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(294, height);
        }

        private Text CreateText(Transform parent, string text, int fontSize, FontStyle style, Color color, TextAnchor alignment, float height)
        {
            GameObject go = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(294, height);

            var txt = go.GetComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.text = text;
            txt.fontSize = fontSize;
            txt.fontStyle = style;
            txt.color = color;
            txt.alignment = alignment;
            txt.raycastTarget = false;
            return txt;
        }

        private void CreateDivider(Transform parent, Color color, float height)
        {
            GameObject go = new GameObject("Divider", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(294, height);

            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
        }

        private void InitializeListeners()
        {
            if (githubButton != null)
            {
                githubButton.OnClickAsObservable()
                    .Subscribe(_ => Application.OpenURL("https://github.com/Randominkey/UnityPortfolio"))
                    .AddTo(_disposables);
            }

            if (archOverviewButton != null)
            {
                archOverviewButton.OnClickAsObservable()
                    .Subscribe(_ => ShowArchitectureModal())
                    .AddTo(_disposables);
            }
        }

        private void StartSelectedGame(PortfolioGameOutline game)
        {
            if (game == null || game.lessonOutline == null)
            {
                Debug.LogWarning("[PortfolioLobbyManager] No game selected or lesson outline is missing!");
                return;
            }

            UniversalStageManager.SelectedGame = game;
            Debug.Log($"[PortfolioLobbyManager] Starting game: {game.gameTitle}");
            UnityEngine.SceneManagement.SceneManager.LoadScene("MasterSandbox");
        }

        public void ShowArchitectureModal()
        {
            if (_archModalGo != null)
            {
                _archModalGo.SetActive(true);
                return;
            }

            var canvas = FindObjectOfType<Canvas>();
            if (canvas == null) return;

            _archModalGo = new GameObject("ArchitectureOverviewModal", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            _archModalGo.transform.SetParent(canvas.transform, false);

            var rt = _archModalGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;

            var bgImg = _archModalGo.GetComponent<Image>();
            bgImg.color = new Color32(10, 15, 26, 235); // Dark semi-transparent overlay

            // Center Panel
            GameObject panelGo = new GameObject("Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panelGo.transform.SetParent(_archModalGo.transform, false);

            var panelRt = panelGo.GetComponent<RectTransform>();
            panelRt.sizeDelta = new Vector2(680, 520);

            var panelImg = panelGo.GetComponent<Image>();
            panelImg.color = new Color32(24, 30, 46, 255);

            var panelOutline = panelGo.AddComponent<Outline>();
            panelOutline.effectColor = new Color32(56, 189, 248, 200);
            panelOutline.effectDistance = new Vector2(2, -2);

            var vlg = panelGo.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(30, 30, 25, 25);
            vlg.spacing = 14;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;

            CreateText(panelGo.transform, "🏗️ Master Framework 아키텍처 개요", 20, FontStyle.Bold, new Color32(56, 189, 248, 255), TextAnchor.MiddleCenter, 30);
            CreateDivider(panelGo.transform, new Color32(51, 65, 85, 255), 2);

            string archDescription =
                "1. 단일 생명주기 관리 (Universal Lifecycle)\n" +
                "   • 서로 다른 구조의 미니게임들을 UniversalPlayableStage로 추상화하여 단일 네비게이션 HUD에서 완벽 통합.\n\n" +
                "2. 데이터 주도 레벨 설계 (Data-Driven SO)\n" +
                "   • 하드코딩된 스테이지/정답 데이터를 ScriptableObject로 규격화하여 유지보수성 및 확장성 극대화.\n\n" +
                "3. 자동 풀이 및 정답 검증 엔진 (Auto-Solve Architecture)\n" +
                "   • 백트래킹/수학적 탐색 알고리즘을 통한 실시간 정답 시연 및 회귀 테스트 자동화 지원.\n\n" +
                "4. 무손실 리팩토링 & 모듈화 (Option A Zero-Loss Migration)\n" +
                "   • 레거시 에셋의 GUID 및 의존성을 모듈 내부로 독립 캡슐화하여 완결된 포트폴리오 패키징 구축.";

            CreateText(panelGo.transform, archDescription, 14, FontStyle.Normal, new Color32(226, 232, 240, 255), TextAnchor.UpperLeft, 280);

            // Close Button
            GameObject closeBtnGo = new GameObject("CloseButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            closeBtnGo.transform.SetParent(panelGo.transform, false);

            var closeRt = closeBtnGo.GetComponent<RectTransform>();
            closeRt.sizeDelta = new Vector2(200, 44);

            var closeImg = closeBtnGo.GetComponent<Image>();
            closeImg.color = new Color32(51, 65, 85, 255);

            CreateText(closeBtnGo.transform, "닫 기 (Close)", 15, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter, 44);

            closeBtnGo.GetComponent<Button>().OnClickAsObservable()
                .Subscribe(_ => _archModalGo.SetActive(false))
                .AddTo(_disposables);
        }

        private void OnDestroy()
        {
            _disposables.Dispose();
        }
    }
}
