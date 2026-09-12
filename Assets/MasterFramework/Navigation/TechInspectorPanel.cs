using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using DG.Tweening;
using MasterFramework.Core;
using MasterFramework.Services;

namespace MasterFramework.Navigation
{
    /// <summary>
    /// 포트폴리오 면접관을 위한 아키텍처 기술 스택 뷰어 및 Auto-Solve 시연 패널
    /// </summary>
    public class TechInspectorPanel : MonoBehaviour
    {
        [Header("Panel Animation")]
        [SerializeField] private RectTransform drawerPanel;
        [SerializeField] private Image overlayBackground;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button overlayButton;

        [Header("Tech Stack Display")]
        [SerializeField] private Text gameTitleText;
        [SerializeField] private Text generationText;
        [SerializeField] private RectTransform badgesContainer;

        [Header("Live Status Display")]
        [SerializeField] private Text activeStageText;
        [SerializeField] private Text ruleStateText;

        [Header("Demo Action Buttons")]
        [SerializeField] private Button autoSolveButton;
        [SerializeField] private Button completeAllButton;
        [SerializeField] private Button resetProgressButton;

        private readonly List<GameObject> _spawnedBadges = new List<GameObject>();
        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        public Action OnAutoSolveRequested { get; set; }
        public Action OnCompleteAllRequested { get; set; }
        public Action OnResetProgressRequested { get; set; }

        private Font _defaultFont;
        private bool _isOpen = false;

        private void Awake()
        {
            _defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            BindButtons();
            gameObject.SetActive(false);
        }

        public void BindButtons()
        {
            _disposables.Clear();

            if (closeButton != null)
            {
                closeButton.OnClickAsObservable()
                    .Subscribe(_ => Close())
                    .AddTo(_disposables);
            }

            if (overlayButton != null)
            {
                overlayButton.OnClickAsObservable()
                    .Subscribe(_ => Close())
                    .AddTo(_disposables);
            }

            if (autoSolveButton != null)
            {
                autoSolveButton.OnClickAsObservable()
                    .ThrottleFirst(TimeSpan.FromSeconds(0.5f))
                    .Subscribe(_ => OnAutoSolveRequested?.Invoke())
                    .AddTo(_disposables);
            }

            if (completeAllButton != null)
            {
                completeAllButton.OnClickAsObservable()
                    .Subscribe(_ => OnCompleteAllRequested?.Invoke())
                    .AddTo(_disposables);
            }

            if (resetProgressButton != null)
            {
                resetProgressButton.OnClickAsObservable()
                    .Subscribe(_ => OnResetProgressRequested?.Invoke())
                    .AddTo(_disposables);
            }
        }

        public void Toggle(PortfolioGameOutline gameOutline, int currentStepIndex, string stepName)
        {
            BindButtons();
            if (_isOpen) Close();
            else Open(gameOutline, currentStepIndex, stepName);
        }

        public void Open(PortfolioGameOutline gameOutline, int currentStepIndex, string stepName)
        {
            BindButtons();
            _isOpen = true;
            gameObject.SetActive(true);

            // 1. 텍스트 바인딩
            if (gameTitleText != null)
            {
                gameTitleText.font = _defaultFont;
                gameTitleText.text = gameOutline != null ? gameOutline.gameTitle : "Portfolio MiniGame";
            }

            if (generationText != null)
            {
                generationText.font = _defaultFont;
                generationText.text = gameOutline != null ? $"Generation: {gameOutline.generationTag}" : "Master Framework";
            }

            if (activeStageText != null)
            {
                activeStageText.font = _defaultFont;
                activeStageText.text = $"Stage {currentStepIndex + 1}: {stepName}";
            }

            // 2. 기술 뱃지 생성
            BuildTechBadges(gameOutline);

            // 3. 슬라이드 애니메이션
            if (drawerPanel != null)
            {
                drawerPanel.DOKill();
                drawerPanel.anchoredPosition = new Vector2(380, 0);
                drawerPanel.DOAnchorPosX(0, 0.3f).SetEase(Ease.OutCubic);
            }

            if (overlayBackground != null)
            {
                overlayBackground.DOKill();
                overlayBackground.color = new Color(0, 0, 0, 0);
                overlayBackground.DOFade(0.5f, 0.3f);
            }
        }

        public void Close()
        {
            _isOpen = false;

            if (drawerPanel != null)
            {
                drawerPanel.DOKill();
                drawerPanel.DOAnchorPosX(380, 0.2f).SetEase(Ease.InCubic);
            }

            if (overlayBackground != null)
            {
                overlayBackground.DOKill();
                overlayBackground.DOFade(0f, 0.2f).OnComplete(() =>
                {
                    gameObject.SetActive(false);
                });
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        private void BuildTechBadges(PortfolioGameOutline gameOutline)
        {
            if (badgesContainer == null) return;

            foreach (var badge in _spawnedBadges)
            {
                Destroy(badge);
            }
            _spawnedBadges.Clear();

            List<string> tags = new List<string> { "Zenject SubContainer", "UniRx Reactive", "Data-Driven SO" };
            if (gameOutline != null && gameOutline.techTags != null)
            {
                foreach (var t in gameOutline.techTags)
                {
                    if (!tags.Contains(t)) tags.Add(t);
                }
            }

            Color32[] badgeColors = new Color32[]
            {
                new Color32(14, 165, 233, 230), // Sky Blue
                new Color32(168, 85, 247, 230), // Purple
                new Color32(34, 197, 94, 230),  // Emerald
                new Color32(249, 115, 22, 230), // Orange
                new Color32(236, 72, 153, 230)  // Pink
            };

            for (int i = 0; i < tags.Count; i++)
            {
                string tagText = tags[i];
                Color32 color = badgeColors[i % badgeColors.Length];

                GameObject badgeObj = new GameObject($"Badge_{i}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                badgeObj.transform.SetParent(badgesContainer, false);

                RectTransform badgeRt = badgeObj.GetComponent<RectTransform>();
                badgeRt.sizeDelta = new Vector2(0, 28);

                Image badgeImg = badgeObj.GetComponent<Image>();
                badgeImg.color = color;

                GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(ContentSizeFitter));
                txtObj.transform.SetParent(badgeObj.transform, false);

                RectTransform txtRt = txtObj.GetComponent<RectTransform>();
                txtRt.anchorMin = Vector2.zero;
                txtRt.anchorMax = Vector2.one;
                txtRt.sizeDelta = Vector2.zero;

                Text txt = txtObj.GetComponent<Text>();
                txt.font = _defaultFont;
                txt.text = $"#{tagText}";
                txt.fontSize = 12;
                txt.fontStyle = FontStyle.Bold;
                txt.color = Color.white;
                txt.alignment = TextAnchor.MiddleCenter;

                _spawnedBadges.Add(badgeObj);
            }
        }

        public void UpdateLiveRuleState(string stateMessage, bool isValid)
        {
            if (ruleStateText != null)
            {
                ruleStateText.font = _defaultFont;
                ruleStateText.text = stateMessage;
                ruleStateText.color = isValid ? new Color32(34, 197, 94, 255) : new Color32(251, 146, 60, 255);
            }
        }

        private void OnDestroy()
        {
            _disposables.Dispose();
        }
    }
}
