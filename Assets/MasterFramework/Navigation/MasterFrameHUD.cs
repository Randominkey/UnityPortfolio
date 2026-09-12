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
    /// 마스터 샌드박스 상단 공통 네비게이션 헤더 및 스텝 탭 인디케이터 관리자
    /// </summary>
    public class MasterFrameHUD : MonoBehaviour
    {
        [Header("Root References")]
        [SerializeField] private RectTransform headerPanel;
        [SerializeField] private Text gameTitleText;
        [SerializeField] private Text gameSubTitleText;
        [SerializeField] private RectTransform tabsContainer;

        [Header("Toolbar Buttons")]
        [SerializeField] private Button lobbyButton;
        [SerializeField] private Button refreshButton;
        [SerializeField] private Button ruleButton;
        [SerializeField] private Button techInspectorButton;
        [SerializeField] private Button undoButton;
        [SerializeField] private Button redoButton;
        [SerializeField] private Button soundButton;

        private readonly List<Button> _tabButtons = new List<Button>();
        private readonly List<Image> _tabBackgrounds = new List<Image>();
        private readonly List<Text> _tabTexts = new List<Text>();
        private Text _soundButtonText;
        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        public Action<int> OnStepTabClicked { get; set; }
        public Action OnLobbyClicked { get; set; }
        public Action OnRefreshClicked { get; set; }
        public Action OnRuleClicked { get; set; }
        public Action OnTechInspectorClicked { get; set; }
        public Action OnUndoClicked { get; set; }
        public Action OnRedoClicked { get; set; }
        public Action OnSoundToggleClicked { get; set; }

        private Font _defaultFont;

        private void Awake()
        {
            _defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        public void Initialize(PortfolioGameOutline gameOutline, LessonOutline outline, int currentStepIndex)
        {
            _disposables.Clear();

            // 1. 게임 타이틀 및 태그 설정
            if (gameTitleText != null)
            {
                gameTitleText.font = _defaultFont;
                gameTitleText.text = gameOutline != null ? gameOutline.gameTitle : "Portfolio MiniGame";
            }

            if (gameSubTitleText != null)
            {
                gameSubTitleText.font = _defaultFont;
                string tag = gameOutline != null ? $"{gameOutline.generationTag} | {gameOutline.gameId}" : "Playable Module";
                gameSubTitleText.text = tag;
            }

            // 2. 동적 툴바 버튼 확보 (Undo, Redo, Sound)
            EnsureToolbarButtons();

            // 3. 툴바 버튼 이벤트 연결
            if (lobbyButton != null)
            {
                lobbyButton.OnClickAsObservable()
                    .Subscribe(_ => OnLobbyClicked?.Invoke())
                    .AddTo(_disposables);
            }

            if (refreshButton != null)
            {
                refreshButton.OnClickAsObservable()
                    .ThrottleFirst(TimeSpan.FromSeconds(0.5f))
                    .Subscribe(_ => OnRefreshClicked?.Invoke())
                    .AddTo(_disposables);
            }

            if (ruleButton != null)
            {
                ruleButton.OnClickAsObservable()
                    .Subscribe(_ => OnRuleClicked?.Invoke())
                    .AddTo(_disposables);
            }

            if (techInspectorButton != null)
            {
                techInspectorButton.OnClickAsObservable()
                    .Subscribe(_ => OnTechInspectorClicked?.Invoke())
                    .AddTo(_disposables);
            }

            if (undoButton != null)
            {
                undoButton.OnClickAsObservable()
                    .Subscribe(_ => OnUndoClicked?.Invoke())
                    .AddTo(_disposables);
            }

            if (redoButton != null)
            {
                redoButton.OnClickAsObservable()
                    .Subscribe(_ => OnRedoClicked?.Invoke())
                    .AddTo(_disposables);
            }

            if (soundButton != null)
            {
                soundButton.OnClickAsObservable()
                    .Subscribe(_ => OnSoundToggleClicked?.Invoke())
                    .AddTo(_disposables);
            }

            // 4. 스텝 탭 생성
            BuildStepTabs(gameOutline != null ? gameOutline.gameId : "Default", outline, currentStepIndex);
        }

        private void EnsureToolbarButtons()
        {
            Transform toolbarContainer = null;
            if (techInspectorButton != null) toolbarContainer = techInspectorButton.transform.parent;
            else if (ruleButton != null) toolbarContainer = ruleButton.transform.parent;
            else if (headerPanel != null) toolbarContainer = headerPanel;

            if (toolbarContainer == null) return;

            if (undoButton == null)
            {
                var btnGo = CreateToolbarButton(toolbarContainer, "UndoButton", "⏪", new Vector2(40, 32), new Color32(30, 41, 59, 230));
                undoButton = btnGo.GetComponent<Button>();
                undoButton.interactable = false;
                if (ruleButton != null) btnGo.transform.SetSiblingIndex(ruleButton.transform.GetSiblingIndex());
            }

            if (redoButton == null)
            {
                var btnGo = CreateToolbarButton(toolbarContainer, "RedoButton", "⏩", new Vector2(40, 32), new Color32(30, 41, 59, 230));
                redoButton = btnGo.GetComponent<Button>();
                redoButton.interactable = false;
                if (ruleButton != null) btnGo.transform.SetSiblingIndex(ruleButton.transform.GetSiblingIndex());
            }

            if (soundButton == null)
            {
                var btnGo = CreateToolbarButton(toolbarContainer, "SoundButton", "🔊", new Vector2(40, 32), new Color32(30, 41, 59, 230));
                soundButton = btnGo.GetComponent<Button>();
                _soundButtonText = btnGo.GetComponentInChildren<Text>();
                btnGo.transform.SetAsLastSibling();
            }
        }

        private GameObject CreateToolbarButton(Transform parent, string name, string label, Vector2 size, Color bgColor)
        {
            GameObject btnGo = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(parent, false);

            var rt = btnGo.GetComponent<RectTransform>();
            rt.sizeDelta = size;

            var img = btnGo.GetComponent<Image>();
            img.color = bgColor;

            var outline = btnGo.AddComponent<Outline>();
            outline.effectColor = new Color32(51, 65, 85, 200);
            outline.effectDistance = new Vector2(1, -1);

            GameObject textGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textGo.transform.SetParent(btnGo.transform, false);
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.sizeDelta = Vector2.zero;

            var txt = textGo.GetComponent<Text>();
            txt.font = _defaultFont;
            txt.text = label;
            txt.fontSize = 14;
            txt.fontStyle = FontStyle.Bold;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.raycastTarget = false;

            return btnGo;
        }

        public void SetUndoRedoState(bool canUndo, bool canRedo)
        {
            if (undoButton != null)
            {
                undoButton.interactable = canUndo;
                var img = undoButton.GetComponent<Image>();
                if (img != null) img.color = canUndo ? new Color32(37, 99, 235, 240) : new Color32(30, 41, 59, 140);
            }

            if (redoButton != null)
            {
                redoButton.interactable = canRedo;
                var img = redoButton.GetComponent<Image>();
                if (img != null) img.color = canRedo ? new Color32(37, 99, 235, 240) : new Color32(30, 41, 59, 140);
            }
        }

        public void SetSoundMuted(bool isMuted)
        {
            if (_soundButtonText != null)
            {
                _soundButtonText.text = isMuted ? "🔇" : "🔊";
            }
            if (soundButton != null)
            {
                var img = soundButton.GetComponent<Image>();
                if (img != null) img.color = isMuted ? new Color32(239, 68, 68, 200) : new Color32(30, 41, 59, 230);
            }
        }

        private void BuildStepTabs(string gameId, LessonOutline outline, int currentStepIndex)
        {
            if (tabsContainer == null || outline == null || outline.steps == null) return;

            // 기존 탭 제거
            foreach (Transform child in tabsContainer)
            {
                Destroy(child.gameObject);
            }

            _tabButtons.Clear();
            _tabBackgrounds.Clear();
            _tabTexts.Clear();

            for (int i = 0; i < outline.steps.Count; i++)
            {
                int stepIdx = i;
                var step = outline.steps[stepIdx];

                GameObject tabObj = new GameObject($"Tab_{stepIdx}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                tabObj.transform.SetParent(tabsContainer, false);

                RectTransform tabRt = tabObj.GetComponent<RectTransform>();
                tabRt.sizeDelta = new Vector2(110, 36);

                Image tabBg = tabObj.GetComponent<Image>();
                Button tabBtn = tabObj.GetComponent<Button>();

                // 텍스트 생성
                GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                txtObj.transform.SetParent(tabObj.transform, false);
                RectTransform txtRt = txtObj.GetComponent<RectTransform>();
                txtRt.anchorMin = Vector2.zero;
                txtRt.anchorMax = Vector2.one;
                txtRt.sizeDelta = Vector2.zero;

                Text txt = txtObj.GetComponent<Text>();
                txt.font = _defaultFont;
                txt.fontSize = 13;
                txt.fontStyle = FontStyle.Bold;
                txt.alignment = TextAnchor.MiddleCenter;
                txt.raycastTarget = false;

                _tabButtons.Add(tabBtn);
                _tabBackgrounds.Add(tabBg);
                _tabTexts.Add(txt);

                tabBtn.OnClickAsObservable()
                    .Subscribe(_ => OnStepTabClicked?.Invoke(stepIdx))
                    .AddTo(_disposables);
            }

            UpdateTabVisuals(gameId, currentStepIndex);
        }

        public void UpdateTabVisuals(string gameId, int currentStepIndex)
        {
            for (int i = 0; i < _tabButtons.Count; i++)
            {
                bool isCurrent = (i == currentStepIndex);
                bool isCleared = PortfolioSaveService.Instance.IsStageCleared(gameId, i);

                string starBadge = isCleared ? " ★" : "";
                _tabTexts[i].text = $"Stage {i + 1}{starBadge}";

                if (isCurrent)
                {
                    _tabBackgrounds[i].color = new Color32(0, 163, 255, 230); // Vibrant Cyan Highlight
                    _tabTexts[i].color = Color.white;
                    _tabBackgrounds[i].transform.DOScale(1.06f, 0.2f).SetEase(Ease.OutQuad);
                }
                else if (isCleared)
                {
                    _tabBackgrounds[i].color = new Color32(30, 41, 59, 200); // Slate with Gold clear
                    _tabTexts[i].color = new Color32(250, 204, 21, 255); // Gold
                    _tabBackgrounds[i].transform.DOScale(1f, 0.2f);
                }
                else
                {
                    _tabBackgrounds[i].color = new Color32(30, 35, 45, 180); // Dark Slate
                    _tabTexts[i].color = new Color32(180, 190, 200, 255);
                    _tabBackgrounds[i].transform.DOScale(1f, 0.2f);
                }
            }
        }

        private void OnDestroy()
        {
            _disposables.Dispose();
        }
    }
}
