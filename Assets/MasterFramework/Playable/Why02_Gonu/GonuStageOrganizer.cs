using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using Zenject;
using Cysharp.Threading.Tasks;
using MasterFramework.Core;
using MasterFramework.Navigation;

namespace Portfolio.Why02.Gonu
{
    public class GonuStageOrganizer : BaseStageOrganizer, IStageOrganizer, IStateRestorable, ICheckableStage
    {
        [Header("Gonu Grid Setup")]
        public Action<bool> pageClearAction;
        public List<GonuStateButtonComponent> stateButtonComponents;
        [SerializeField] private GonuRuleValidator.GonuRuleType ruleNumber;
        public GonuRuleValidator.GonuRuleType Rule { get => ruleNumber; set => ruleNumber = value; }
        [SerializeField] private int myPlayerIndex = 0; // 0: Player (Blue), 1: AI / Opponent (Red)
        [SerializeField] private List<Toggle> playerToggles;
        [SerializeField] private GameObject blocker;

        [Header("Win/Lose Objects")]
        [SerializeField] private GameObject winObject;
        [SerializeField] private GameObject loseObject;
        [SerializeField] private GameObject victoryPopup;
        [SerializeField] private Button victoryPopupGameEndButton;

        [Header("Quiz UI (Stage 1 & 2)")]
        private GameObject _quizContainerGo;
        private Toggle _winToggle;
        private Toggle _loseToggle;
        private bool _isQuizMode = false;
        private int _currentStageIndex = 0;

        [Header("AI Settings")]
        [SerializeField] private bool singlePlayAssistAIExist = true;

        // Private States
        private bool userInputBlocked = false;
        private Coroutine aIActionCoroutine;
        private int currentSelectedButtonIndex = -1;
        private CompositeDisposable _buttonDisposables = new CompositeDisposable();

        // Services & Dependencies (Injected via Zenject SubContainer)
        private GonuRuleValidator _gonuRuleValidator;
        private GonuAIService _aiService;

        public GonuRuleValidator RuleValidator => _gonuRuleValidator ?? (_gonuRuleValidator = new GonuRuleValidator());
        public GonuAIService AIService => _aiService ?? (_aiService = new GonuAIService(SoundService));

        [Inject]
        public void ConstructGonu(
            [InjectOptional] GonuRuleValidator gonuRuleValidator,
            [InjectOptional] GonuAIService aiService)
        {
            _gonuRuleValidator = gonuRuleValidator ?? new GonuRuleValidator();
            _aiService = aiService ?? new GonuAIService(SoundService);
        }

        public override async UniTask InitializeStageAsync(int index)
        {
            await base.InitializeStageAsync(index);
            if (_gonuRuleValidator == null) _gonuRuleValidator = new GonuRuleValidator();
            if (_aiService == null) _aiService = new GonuAIService(SoundService);

            _currentStageIndex = Mathf.Clamp(index, 0, 3);
            ruleNumber = GonuRuleValidator.GonuRuleType.RuleTwo; // 우물고누 Rule

            SortStateButtonsNaturally();
            EnsureButtonAdjacencyGraph();

            for (int i = 0; i < stateButtonComponents.Count; i++)
            {
                stateButtonComponents[i].Ready(i);
            }

            SetupStageState(_currentStageIndex);
            SetupQuizUI(_currentStageIndex);

            SubscribeButtons();

            if (victoryPopupGameEndButton != null)
            {
                victoryPopupGameEndButton.OnClickAsObservable()
                    .Subscribe(_ => ResetGame())
                    .AddTo(this);
            }

            await UniTask.CompletedTask;
        }

        private void SortStateButtonsNaturally()
        {
            if (stateButtonComponents == null || stateButtonComponents.Count == 0) return;

            stateButtonComponents = stateButtonComponents
                .Where(b => b != null)
                .OrderBy(b => ExtractButtonIndex(b.gameObject.name))
                .ToList();
        }

        private int ExtractButtonIndex(string goName)
        {
            var match = System.Text.RegularExpressions.Regex.Match(goName, @"\((\d+)\)");
            if (match.Success)
            {
                return int.Parse(match.Groups[1].Value);
            }
            return 0; // StateButton without number is index 0
        }

        private void SetupStageState(int stageIdx)
        {
            // Exact piece configurations:
            // -1: Empty, 0: Player 1 (Blue), 1: Player 2 / AI (Red)
            int[] pieceSetup;

            switch (stageIdx)
            {
                case 0:
                    // Stage 1: Player=[3, 4, 9], AI=[1, 5, 7], Empty=[0, 2, 6, 8, 10]
                    pieceSetup = new int[] { -1, 1, -1, 0, 0, 1, -1, 1, -1, 0, -1 };
                    _isQuizMode = true;
                    break;
                case 1:
                    // Stage 2: Player=[3, 6, 9], AI=[1, 4, 7], Empty=[0, 2, 5, 8, 10]
                    pieceSetup = new int[] { -1, 1, -1, 0, 1, -1, 0, 1, -1, 0, -1 };
                    _isQuizMode = true;
                    break;
                case 2:
                    // Stage 3: Real Match Starting Setup (3:3)
                    // AI Top=[0, 1, 2], Center=[3..7 empty], Player Bottom=[8, 9, 10]
                    pieceSetup = new int[] { 1, 1, 1, -1, -1, -1, -1, -1, 0, 0, 0 };
                    _isQuizMode = false;
                    break;
                case 3:
                    // Stage 4: 4:4 Match Setup (StateButton 3: Enemy, StateButton 7: Player)
                    // AI Top=[0, 1, 2, 3], Center=[4, 5, 6 empty], Player Bottom=[7, 8, 9, 10]
                    pieceSetup = new int[] { 1, 1, 1, 1, -1, -1, -1, 0, 0, 0, 0 };
                    _isQuizMode = false;
                    break;
                default:
                    pieceSetup = new int[] { 1, 1, 1, -1, -1, -1, -1, -1, 0, 0, 0 };
                    _isQuizMode = false;
                    break;
            }

            for (int i = 0; i < stateButtonComponents.Count; i++)
            {
                int s = (i < pieceSetup.Length) ? pieceSetup[i] : -1;
                stateButtonComponents[i].initialState = s;
                stateButtonComponents[i].state.Value = s;
                stateButtonComponents[i].isSelected.Value = false;
            }

            userInputBlocked = false;
            currentSelectedButtonIndex = -1;
            SetCurrentTurn(0); // P1 시작 및 턴 토글 갱신
        }

        private void SetupQuizUI(int stageIdx)
        {
            if (_quizContainerGo == null)
            {
                CreateQuizUIHierarchy();
            }

            if (_quizContainerGo != null)
            {
                _quizContainerGo.SetActive(_isQuizMode);
                if (_winToggle != null) _winToggle.isOn = false;
                if (_loseToggle != null) _loseToggle.isOn = false;
            }
        }

        private void CreateQuizUIHierarchy()
        {
            _quizContainerGo = new GameObject("QuizUI_Container", typeof(RectTransform));
            _quizContainerGo.transform.SetParent(transform, false);
            var rt = _quizContainerGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.6f, 0.8f);
            rt.anchorMax = new Vector2(0.95f, 0.95f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;

            var hl = _quizContainerGo.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 15;
            hl.childAlignment = TextAnchor.MiddleRight;
            hl.childControlWidth = false;
            hl.childControlHeight = false;

            // Win Toggle Button (이길 수 있다)
            _winToggle = CreateToggleElement(_quizContainerGo.transform, "WinToggle", "이길 수 있다");
            // Lose Toggle Button (이길 수 없다)
            _loseToggle = CreateToggleElement(_quizContainerGo.transform, "LoseToggle", "이길 수 없다");

            // Mutual exclusivity (상호 배타적 선택)
            _winToggle.onValueChanged.AddListener(isOn => { if (isOn && _loseToggle != null) _loseToggle.isOn = false; });
            _loseToggle.onValueChanged.AddListener(isOn => { if (isOn && _winToggle != null) _winToggle.isOn = false; });
        }

        private Toggle CreateToggleElement(Transform parent, string name, string label)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Toggle));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(130, 42);

            var img = go.GetComponent<Image>();
            img.color = new Color32(50, 60, 80, 230);

            var toggle = go.GetComponent<Toggle>();
            toggle.targetGraphic = img;

            GameObject textGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textGo.transform.SetParent(go.transform, false);
            var txtRt = textGo.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.sizeDelta = Vector2.zero;

            var txt = textGo.GetComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.text = label;
            txt.fontSize = 15;
            txt.fontStyle = FontStyle.Bold;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.raycastTarget = false;

            toggle.onValueChanged.AddListener(isOn =>
            {
                img.color = isOn ? new Color32(0, 200, 115, 255) : new Color32(50, 60, 80, 230);
            });

            return toggle;
        }

        private void EnsurePlayerToggles()
        {
            if (playerToggles == null || playerToggles.Count == 0 || playerToggles.Any(t => t == null))
            {
                playerToggles = new List<Toggle>();
                var pt0 = transform.Find("PlayerToggle0")?.GetComponent<Toggle>();
                var pt1 = transform.Find("PlayerToggle1")?.GetComponent<Toggle>();
                if (pt0 != null) playerToggles.Add(pt0);
                if (pt1 != null) playerToggles.Add(pt1);
            }
        }

        private void SetCurrentTurn(int playerIdx)
        {
            myPlayerIndex = playerIdx;
            EnsurePlayerToggles();
            if (playerToggles != null)
            {
                for (int i = 0; i < playerToggles.Count; i++)
                {
                    if (playerToggles[i] != null)
                    {
                        playerToggles[i].isOn = (i == myPlayerIndex);
                    }
                }
            }
        }

        private bool EvaluateQuizAnswer()
        {
            if (_winToggle == null || _loseToggle == null) return false;

            if (!_winToggle.isOn && !_loseToggle.isOn)
            {
                PopupService?.ShowToast("선공의 승리 가능 여부 토글(이길 수 있다/없다)을 먼저 선택해주세요.");
                return false;
            }

            // Expected Answer:
            // Stage 1 (Index 0): Lose (이길 수 없다)
            // Stage 2 (Index 1): Win (이길 수 있다)
            bool isCorrect = false;
            if (_currentStageIndex == 0)
            {
                isCorrect = _loseToggle.isOn;
            }
            else if (_currentStageIndex == 1)
            {
                isCorrect = _winToggle.isOn;
            }

            if (isCorrect)
            {
                SoundService?.PlaySFX("Correct");
                PopupService?.ShowToast("정답입니다! 완벽하게 분석했습니다.");
                SetStageCleared(true);
                MasterFramework.Services.PortfolioSaveService.Instance.SetStageCleared("Why02_Week11_Gonu", _currentStageIndex, true);
                pageClearAction?.Invoke(true);
                return true;
            }
            else
            {
                SoundService?.PlaySFX("Wrong");
                PopupService?.ShowToast("틀렸습니다. 말들의 이동 경로를 다시 분석해보세요.");
                return false;
            }
        }

        private void EnsureButtonAdjacencyGraph()
        {
            if (stateButtonComponents == null || stateButtonComponents.Count < 11) return;

            // Exact Gonu directional rules from original Why02 Week 11 (i_Learning_2_11.unity):
            // Nodes 0, 2, 8, 10 (Corners) -> Nodes 1, 9 (Well exits) -> Central Diamond (3, 4, 5, 6, 7).
            // Once entering the central diamond, pieces CANNOT move back out to 1 or 9!
            int[][] adjacency = new int[][]
            {
                new int[] { },                  // 0: Top-Left (Exit-only into 1)
                new int[] { 0, 2 },             // 1: Top-Center (Connects from 0, 2 only. Cannot enter from 3!)
                new int[] { },                  // 2: Top-Right (Exit-only into 1)
                new int[] { 1, 4, 5, 6 },       // 3: Upper-Center (Connects from 1, 4, 5, 6)
                new int[] { 3, 5, 7 },          // 4: Mid-Left (Connects from 3, 5, 7)
                new int[] { 3, 4, 6, 7 },       // 5: Center (Connects from 3, 4, 6, 7)
                new int[] { 3, 5, 7 },          // 6: Mid-Right (Connects from 3, 5, 7)
                new int[] { 4, 5, 6, 9 },       // 7: Lower-Center (Connects from 4, 5, 6, 9)
                new int[] { },                  // 8: Bottom-Left (Exit-only into 9)
                new int[] { 8, 10 },            // 9: Bottom-Center (Connects from 8, 10 only. Cannot enter from 7!)
                new int[] { }                   // 10: Bottom-Right (Exit-only into 9)
            };

            for (int i = 0; i < stateButtonComponents.Count && i < adjacency.Length; i++)
            {
                var btn = stateButtonComponents[i];
                if (btn == null) continue;

                if (btn.originateButtonComponents == null)
                    btn.originateButtonComponents = new List<GonuStateButtonComponent>();
                else
                    btn.originateButtonComponents.Clear();

                foreach (int neighborIdx in adjacency[i])
                {
                    if (neighborIdx >= 0 && neighborIdx < stateButtonComponents.Count)
                    {
                        var neighborBtn = stateButtonComponents[neighborIdx];
                        if (neighborBtn != null)
                        {
                            btn.originateButtonComponents.Add(neighborBtn);
                        }
                    }
                }
            }
        }

        private void SubscribeButtons()
        {
            _buttonDisposables.Clear();
            foreach (var btn in stateButtonComponents)
            {
                if (btn == null || btn.button == null) continue;

                btn.button
                    .OnClickAsObservable()
                    .Subscribe(_ =>
                    {
                        if (userInputBlocked || myPlayerIndex != 0) return;
                        HandlePlayerButtonClick(btn);
                    })
                    .AddTo(_buttonDisposables);
            }
        }

        private void HandlePlayerButtonClick(GonuStateButtonComponent btn)
        {
            // 1. If clicking own piece (Player 1 / Blue / state 0):
            if (btn.state.Value == myPlayerIndex)
            {
                if (currentSelectedButtonIndex == btn.index)
                {
                    // Deselect currently selected piece
                    btn.isSelected.Value = false;
                    currentSelectedButtonIndex = -1;
                }
                else
                {
                    // Deselect previously selected piece if any
                    if (currentSelectedButtonIndex >= 0 && currentSelectedButtonIndex < stateButtonComponents.Count)
                    {
                        stateButtonComponents[currentSelectedButtonIndex].isSelected.Value = false;
                    }

                    // Select this new piece
                    btn.isSelected.Value = true;
                    currentSelectedButtonIndex = btn.index;
                    SoundService?.PlaySFX("Click");
                }
            }
            // 2. If clicking an empty slot (state -1) when a piece is currently selected:
            else if (btn.state.Value == -1)
            {
                if (currentSelectedButtonIndex >= 0 && currentSelectedButtonIndex < stateButtonComponents.Count)
                {
                    var selectedBtn = stateButtonComponents[currentSelectedButtonIndex];

                    // Check if target empty slot can be entered from the selected piece
                    bool isAdjacent = btn.originateButtonComponents.Any(x => x.index == currentSelectedButtonIndex);

                    if (isAdjacent)
                    {
                        // Move piece to empty slot
                        btn.state.Value = myPlayerIndex;
                        selectedBtn.isSelected.Value = false;
                        selectedBtn.state.Value = -1;
                        currentSelectedButtonIndex = -1;

                        SoundService?.PlaySFX("Click");

                        // Evaluate game state and trigger AI turn in all stages
                        CheckAndEvaluateGame();
                    }
                    else
                    {
                        PopupService?.ShowToast("인접한 빈 칸으로만 이동할 수 있습니다.");
                    }
                }
                else
                {
                    PopupService?.ShowToast("먼저 이동하고 싶은 말을 선택해주세요.");
                }
            }
        }

        private void CheckAndEvaluateGame()
        {
            if (RuleValidator.CheckGameEnd(ruleNumber, stateButtonComponents, myPlayerIndex))
            {
                if (!_isQuizMode)
                {
                    OnPlayerWin();
                }
                else
                {
                    // Stage 1, 2 (퀴즈 모드)에서는 고누 승패와 무관하게 토글 선택 후 상단 정답확인을 눌러야 클리어됨
                    PopupService?.ShowToast("더 이상 이동할 수 없습니다. 우상단의 승리 가능 여부 토글을 선택하고 상단의 [정답 확인]을 누르세요.");
                }
            }
            else
            {
                EndTurn();
            }
        }

        private void EndTurn()
        {
            SetCurrentTurn(1 - myPlayerIndex);

            if (myPlayerIndex == 1 && singlePlayAssistAIExist)
            {
                userInputBlocked = true;
                aIActionCoroutine = StartCoroutine(AIService.ExecuteAITurnCoroutine(
                    stateButtonComponents,
                    () =>
                    {
                        // AI Wins (Player lost)
                        SoundService?.PlaySFX("Lose");
                        if (!_isQuizMode)
                        {
                            PopupService?.ShowToast("패배했습니다! 다시 도전해보세요.");
                            userInputBlocked = false;
                            ResetGame();
                        }
                        else
                        {
                            userInputBlocked = false;
                            PopupService?.ShowToast("AI가 승리 경로를 차단했습니다. 우상단의 토글을 선택하고 [정답 확인]을 누르세요.");
                        }
                    },
                    () =>
                    {
                        // Turn returned to player
                        SetCurrentTurn(0);
                        userInputBlocked = false;

                        if (RuleValidator.CheckGameEnd(ruleNumber, stateButtonComponents, 1))
                        {
                            if (!_isQuizMode)
                            {
                                OnPlayerWin();
                            }
                            else
                            {
                                PopupService?.ShowToast("더 이상 이동할 수 없습니다. 우상단의 승리 가능 여부 토글을 선택하고 상단의 [정답 확인]을 누르세요.");
                            }
                        }
                    }));
            }
        }

        #region ICheckableStage
        public bool SupportsAutoClear => !_isQuizMode;

        public bool CheckAnswer()
        {
            if (_isQuizMode)
            {
                return EvaluateQuizAnswer();
            }
            else
            {
                // Stage 3, 4 (실전 대전 모드)
                bool playerWon = RuleValidator.CheckGameEnd(ruleNumber, stateButtonComponents, 1);
                if (playerWon)
                {
                    OnPlayerWin();
                    return true;
                }
                else
                {
                    PopupService?.ShowToast("아직 대전이 진행 중입니다. 상대의 이동 경로를 모두 막으세요!");
                    return false;
                }
            }
        }

        public void SetAutoClearMode(bool isAutoClear)
        {
            // 퀴즈 모드는 수동 정답확인 버튼으로만 채점
        }
        #endregion

        private void OnPlayerWin()
        {
            SoundService?.PlaySFX("Win");
            if (victoryPopup != null) victoryPopup.SetActive(true);
            else if (winObject != null) winObject.SetActive(true);

            PopupService?.ShowToast("🎉 우물고누 대전에서 승리했습니다!");
            SetStageCleared(true);
            MasterFramework.Services.PortfolioSaveService.Instance.SetStageCleared("Why02_Week11_Gonu", _currentStageIndex, true);
            pageClearAction?.Invoke(true);
        }

        public void ResetGame()
        {
            if (aIActionCoroutine != null)
            {
                StopCoroutine(aIActionCoroutine);
                aIActionCoroutine = null;
            }

            SetupStageState(_currentStageIndex);
            if (victoryPopup != null) victoryPopup.SetActive(false);
            if (winObject != null) winObject.SetActive(false);
            if (loseObject != null) loseObject.SetActive(false);
        }

        public void ClearData()
        {
            ResetGame();
        }

        public string SerializeState()
        {
            return JsonUtility.ToJson(new GonuStateData
            {
                pieceStates = stateButtonComponents.Select(b => b.state.Value).ToArray(),
                currentPlayer = myPlayerIndex
            });
        }

        public void DeserializeAndRestoreState(string stateData)
        {
            if (string.IsNullOrEmpty(stateData)) return;
            var data = JsonUtility.FromJson<GonuStateData>(stateData);
            if (data != null && data.pieceStates != null)
            {
                for (int i = 0; i < stateButtonComponents.Count && i < data.pieceStates.Length; i++)
                {
                    stateButtonComponents[i].state.Value = data.pieceStates[i];
                }
                myPlayerIndex = data.currentPlayer;
            }
        }

        private void OnDestroy()
        {
            _buttonDisposables.Dispose();
        }

        [Serializable]
        private class GonuStateData
        {
            public int[] pieceStates;
            public int currentPlayer;
        }
    }
}
