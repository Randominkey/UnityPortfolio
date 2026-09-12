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

        private void Start()
        {
            EnsureEventSystemInputModule();
            BuildDashboardLayout();
            InitializeListeners();
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

        private void BuildDashboardLayout()
        {
            if (cardParent == null) return;

            // Clear old children
            for (int i = cardParent.childCount - 1; i >= 0; i--)
            {
                Destroy(cardParent.GetChild(i).gameObject);
            }

            if (availableGames == null || availableGames.Count == 0) return;

            Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            foreach (var game in availableGames)
            {
                if (game == null) continue;

                // Create Card Container
                GameObject cardGo = new GameObject($"Card_{game.gameId}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                cardGo.transform.SetParent(cardParent, false);

                var cardRt = cardGo.GetComponent<RectTransform>();
                cardRt.sizeDelta = new Vector2(350, 420);

                var cardImg = cardGo.GetComponent<Image>();
                cardImg.color = new Color32(24, 30, 46, 250); // Dark sleek blue-gray
                cardImg.raycastTarget = true;

                // Card Outline / Border
                var outline = cardGo.AddComponent<Outline>();
                outline.effectColor = new Color32(51, 65, 85, 255);
                outline.effectDistance = new Vector2(2, -2);

                // Vertical Layout inside Card
                var vlg = cardGo.AddComponent<VerticalLayoutGroup>();
                vlg.padding = new RectOffset(20, 20, 20, 20);
                vlg.spacing = 10;
                vlg.childAlignment = TextAnchor.UpperCenter;
                vlg.childControlWidth = true;
                vlg.childControlHeight = false;

                // 1. Generation Tag + Stage Count Badge Header
                int totalStages = game.totalStages > 0 ? game.totalStages : (game.lessonOutline != null ? game.lessonOutline.steps.Count : 4);
                int clearedCount = PortfolioSaveService.Instance != null ? PortfolioSaveService.Instance.GetClearedStageCount(game.gameId, totalStages) : 0;
                bool isCompleted = PortfolioSaveService.Instance != null && PortfolioSaveService.Instance.IsGameCompleted(game.gameId, totalStages);

                CreateText(cardGo.transform, $"{game.generationTag.ToUpper()}  •  {totalStages} STAGES", 12, FontStyle.Bold, new Color32(56, 189, 248, 255), TextAnchor.UpperLeft, 20);

                // 2. Game Title & Subtitle
                CreateText(cardGo.transform, game.gameTitle, 19, FontStyle.Bold, Color.white, TextAnchor.UpperLeft, 28);
                if (!string.IsNullOrEmpty(game.subtitle))
                {
                    CreateText(cardGo.transform, game.subtitle, 13, FontStyle.Italic, new Color32(148, 163, 184, 255), TextAnchor.UpperLeft, 18);
                }

                // 3. Clear Progress Stars Bar
                string starDisplay = isCompleted ? "★★★★  🏆 [ALL CLEARED]" : (clearedCount > 0 ? $"{new string('★', clearedCount)}{new string('☆', totalStages - clearedCount)}  ({clearedCount}/{totalStages})" : $"{new string('☆', totalStages)}  (0/{totalStages})");
                Color starColor = isCompleted ? new Color32(250, 204, 21, 255) : (clearedCount > 0 ? new Color32(234, 179, 8, 255) : new Color32(100, 116, 139, 255));
                CreateText(cardGo.transform, starDisplay, 14, FontStyle.Bold, starColor, TextAnchor.MiddleLeft, 22);

                // 4. Divider Line
                CreateDivider(cardGo.transform, new Color32(51, 65, 85, 200), 2);

                // 5. System & Implementation Summary Header
                CreateText(cardGo.transform, "🎯 시스템 구현 요약", 13, FontStyle.Bold, new Color32(226, 232, 240, 255), TextAnchor.MiddleLeft, 20);

                // 6. Feature Bullet Points
                string bullet1 = (game.featurePoints != null && game.featurePoints.Length > 0) ? game.featurePoints[0] : game.systemSummary;
                string bullet2 = (game.featurePoints != null && game.featurePoints.Length > 1) ? game.featurePoints[1] : "";

                CreateText(cardGo.transform, $"• {bullet1}", 12, FontStyle.Normal, new Color32(203, 213, 225, 255), TextAnchor.UpperLeft, 44);
                if (!string.IsNullOrEmpty(bullet2))
                {
                    CreateText(cardGo.transform, $"• {bullet2}", 12, FontStyle.Normal, new Color32(203, 213, 225, 255), TextAnchor.UpperLeft, 44);
                }

                // 7. Auto-Solve Badge (if enabled)
                if (game.hasAutoSolve)
                {
                    CreateText(cardGo.transform, "⚡ Auto-Solve 자동 풀이 지원", 12, FontStyle.Bold, new Color32(34, 197, 94, 255), TextAnchor.MiddleLeft, 20);
                }
                else
                {
                    CreateText(cardGo.transform, "🕹️ Interactive Turn Play", 12, FontStyle.Bold, new Color32(168, 85, 247, 255), TextAnchor.MiddleLeft, 20);
                }

                // 8. Play / Enter Button
                GameObject btnGo = new GameObject("EnterButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                btnGo.transform.SetParent(cardGo.transform, false);
                var btnRt = btnGo.GetComponent<RectTransform>();
                btnRt.sizeDelta = new Vector2(310, 44);

                var btnImg = btnGo.GetComponent<Image>();
                btnImg.color = new Color32(37, 99, 235, 255); // Royal blue

                var btnOutline = btnGo.AddComponent<Outline>();
                btnOutline.effectColor = new Color32(96, 165, 250, 200);
                btnOutline.effectDistance = new Vector2(1, -1);

                CreateText(btnGo.transform, "▶  스테이지 입장", 15, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter, 44);

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
                    cardGo.transform.DOScale(1.03f, 0.15f).SetEase(Ease.OutQuad);
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

        private Text CreateText(Transform parent, string text, int fontSize, FontStyle style, Color color, TextAnchor alignment, float height)
        {
            GameObject go = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(310, height);

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
            rt.sizeDelta = new Vector2(310, height);

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
