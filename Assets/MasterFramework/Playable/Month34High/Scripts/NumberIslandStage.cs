using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using MasterFramework.Core;

namespace MasterFramework.Playable.Month34High
{
    public class NumberIslandStage : MonoBehaviour, IStageOrganizer, IStateRestorable, IUndoableStage, IStageClearConfigurable, ICheckableStage
    {
        [Header("Configuration / Data")]
        [SerializeField] private NumberIslandLevelData levelData;

        [Header("Clear Submission Mode")]
        [Tooltip("보드 자체의 시각적 정답/오답 판정은 항상 실시간으로 유지되며, '최종 클리어 팝업/스탬프 신호'를 언제 띄울지 결정합니다.")]
        [SerializeField] private StageClearTriggerMode clearTriggerMode = StageClearTriggerMode.ManualSubmitButton;
        [SerializeField] private ValidationCapability validationCapability = ValidationCapability.Flexible;
        [SerializeField] private Toggle autoClearToggle;

        [Header("UI References")]
        [SerializeField] private GridLayoutGroup cellParent;
        [SerializeField] private NumberIslandCell cellPrefab;
        [SerializeField] private Button checkAnswerButton;
        [SerializeField] private Button refreshButton;
        [SerializeField] private Button undoButton;
        [SerializeField] private Button redoButton;

        [Header("Audio & Services")]
        private ISoundService _soundService;
        private IPopupService _popupService;
        private IFeedbackStampService _stampService;

        public int StageIndex { get; set; }

        public StageClearTriggerMode ClearTriggerMode
        {
            get => clearTriggerMode;
            set => SetClearTriggerMode(value);
        }

        public ValidationCapability Capability => validationCapability;

        #region ICheckableStage
        public bool CheckAnswer()
        {
            OnCheckAnswerButtonClicked();
            return _isStageCleared.Value;
        }

        public void SetAutoClearMode(bool isAutoClear)
        {
            SetClearTriggerMode(isAutoClear ? StageClearTriggerMode.AutoInstantOnSolved : StageClearTriggerMode.ManualSubmitButton);
            if (autoClearToggle != null && autoClearToggle.isOn != isAutoClear)
            {
                autoClearToggle.isOn = isAutoClear;
            }
        }
        #endregion

        private readonly BoolReactiveProperty _isStageCleared = new BoolReactiveProperty(false);
        public IReadOnlyReactiveProperty<bool> IsStageCleared => _isStageCleared;

        private NumberIslandRuleValidator _validator = new NumberIslandRuleValidator();
        private NumberIslandCell[,] _cells;
        private int _rowCount;
        private int _colCount;

        private readonly HistoryManager<string> _historyManager = new HistoryManager<string>();
        public bool CanUndo => _historyManager.CanUndo;
        public bool CanRedo => _historyManager.CanRedo;
        public IObservable<Unit> OnHistoryChanged => _historyManager.OnHistoryChanged;

        private CompositeDisposable _disposables = new CompositeDisposable();

        [Inject]
        public void Construct(
            [InjectOptional] ISoundService soundService = null,
            [InjectOptional] IPopupService popupService = null,
            [InjectOptional] IFeedbackStampService stampService = null)
        {
            _soundService = soundService;
            _popupService = popupService;
            _stampService = stampService;
        }

        public void SetStageData(ScriptableObject data)
        {
            if (data is NumberIslandLevelData islandData)
            {
                levelData = islandData;
            }
        }

        public void SetClearTriggerMode(StageClearTriggerMode mode)
        {
            clearTriggerMode = mode;
            if (autoClearToggle != null)
            {
                autoClearToggle.isOn = (mode == StageClearTriggerMode.AutoInstantOnSolved);
            }

            // 즉시 클리어 모드로 전환 시, 이미 보드가 완성 상태라면 바로 클리어 트리거
            if (clearTriggerMode == StageClearTriggerMode.AutoInstantOnSolved && _cells != null)
            {
                ValidationResult result = _validator.ValidateIslandGrid(_cells, _rowCount, _colCount);
                if (result.isCorrect && !_isStageCleared.Value)
                {
                    TriggerStageCleared();
                }
            }
        }

        public async UniTask InitializeStageAsync(int index)
        {
            StageIndex = index;
            _isStageCleared.Value = false;
            _disposables.Clear();

            if (levelData == null)
            {
                Debug.LogError($"[NumberIslandStage] levelData is not assigned for stage {index}!");
                return;
            }

            _rowCount = levelData.RowCount;
            _colCount = levelData.ColumnCount;

            // 1. Grid setup
            cellParent.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            cellParent.constraintCount = _colCount;

            AdjustGridCellSize();

            for (int i = cellParent.transform.childCount - 1; i >= 0; i--)
            {
                Destroy(cellParent.transform.GetChild(i).gameObject);
            }

            // 2. Instantiate cells
            _cells = new NumberIslandCell[_rowCount, _colCount];

            for (int r = 0; r < _rowCount; r++)
            {
                for (int c = 0; c < _colCount; c++)
                {
                    NumberIslandCell cell = Instantiate(cellPrefab, cellParent.transform);
                    cell.name = $"Cell_{r}_{c}";
                    cell.Ready(r, c);

                    cell.OnCellClicked = OnCellClicked;
                    _cells[r, c] = cell;
                }
            }

            // 3. Apply hints
            if (levelData.Hints != null)
            {
                foreach (var hint in levelData.Hints)
                {
                    if (hint.x >= 0 && hint.x < _rowCount && hint.y >= 0 && hint.y < _colCount)
                    {
                        _cells[hint.x, hint.y].SetAsNumberLand(hint.z);
                    }
                }
            }

            // 초기 보드 판 실시간 유효성 상태 평가
            _validator.ValidateIslandGrid(_cells, _rowCount, _colCount);

            // 4. Bind UI buttons
            if (checkAnswerButton != null)
            {
                checkAnswerButton.OnClickAsObservable()
                    .Subscribe(_ => OnCheckAnswerButtonClicked())
                    .AddTo(_disposables);
            }

            if (refreshButton != null)
            {
                refreshButton.OnClickAsObservable()
                    .Subscribe(_ => RefreshStage())
                    .AddTo(_disposables);
            }

            if (undoButton != null)
            {
                undoButton.OnClickAsObservable()
                    .Subscribe(_ => Undo())
                    .AddTo(_disposables);
            }

            if (redoButton != null)
            {
                redoButton.OnClickAsObservable()
                    .Subscribe(_ => Redo())
                    .AddTo(_disposables);
            }

            // Bind Auto Clear Toggle UI
            if (autoClearToggle != null)
            {
                if (validationCapability == ValidationCapability.Flexible)
                {
                    autoClearToggle.gameObject.SetActive(true);
                    autoClearToggle.isOn = (clearTriggerMode == StageClearTriggerMode.AutoInstantOnSolved);
                    autoClearToggle.OnValueChangedAsObservable()
                        .Subscribe(isOn =>
                        {
                            SetClearTriggerMode(isOn ? StageClearTriggerMode.AutoInstantOnSolved : StageClearTriggerMode.ManualSubmitButton);
                        })
                        .AddTo(_disposables);
                }
                else
                {
                    autoClearToggle.gameObject.SetActive(false);
                }
            }

            _historyManager.OnHistoryChanged
                .Subscribe(_ =>
                {
                    if (undoButton != null) undoButton.interactable = CanUndo;
                    if (redoButton != null) redoButton.interactable = CanRedo;
                })
                .AddTo(_disposables);

            _historyManager.Clear();

            await UniTask.CompletedTask;
        }

        private void AdjustGridCellSize()
        {
            RectTransform parentRect = cellParent.GetComponent<RectTransform>();
            if (parentRect == null) return;

            float availableWidth = parentRect.rect.width - cellParent.padding.left - cellParent.padding.right - (_colCount - 1) * cellParent.spacing.x;
            float availableHeight = parentRect.rect.height - cellParent.padding.top - cellParent.padding.bottom - (_rowCount - 1) * cellParent.spacing.y;

            if (availableWidth > 0 && availableHeight > 0 && _rowCount > 0 && _colCount > 0)
            {
                float cellW = availableWidth / _colCount;
                float cellH = availableHeight / _rowCount;
                float minSize = Mathf.Min(cellW, cellH);
                cellParent.cellSize = new Vector2(minSize, minSize);
            }
        }

        private void OnCellClicked(NumberIslandCell cell)
        {
            _soundService?.PlaySFX("Click");

            // [핵심] 보드 판의 타일 상태(Correct/Error 등)는 실시간으로 항상 갱신됩니다!
            ValidationResult result = _validator.ValidateIslandGrid(_cells, _rowCount, _colCount);

            // 클리어 신호(팝업/스탬프)를 띄우는 타이밍만 모드에 따라 분기
            if (clearTriggerMode == StageClearTriggerMode.AutoInstantOnSolved)
            {
                if (result.isCorrect && !_isStageCleared.Value)
                {
                    TriggerStageCleared();
                }
                else if (!result.isCorrect && _isStageCleared.Value)
                {
                    // 정답 상태에서 다시 오답으로 변경된 경우
                    _isStageCleared.Value = false;
                    _stampService?.HideStamp();
                }
            }
        }

        public void OnCheckAnswerButtonClicked()
        {
            if (_cells == null) return;

            ValidationResult result = _validator.ValidateIslandGrid(_cells, _rowCount, _colCount);

            if (result.isCorrect)
            {
                TriggerStageCleared();
            }
            else
            {
                _isStageCleared.Value = false;
                _soundService?.PlaySFX("Error");
                _stampService?.ShowStampAsync(StampType.Fail).Forget();
            }
        }

        private void TriggerStageCleared()
        {
            _isStageCleared.Value = true;
            _soundService?.PlaySFX("Correct");
            _stampService?.ShowStampAsync(StampType.Complete).Forget();
            Debug.Log($"[NumberIslandStage] Stage {StageIndex} Cleared!");
        }

        public async UniTask EnterStageAsync()
        {
            Debug.Log($"[NumberIslandStage] Entered stage {StageIndex}");

            // 스테이지 진입/재진입 시 현재 보드 배치 상태를 즉시 평가하여 셀 정오 표기(색상) 갱신
            if (_cells != null)
            {
                _validator.ValidateIslandGrid(_cells, _rowCount, _colCount);
            }

            await UniTask.CompletedTask;
        }

        public async UniTask ExitStageAsync()
        {
            Debug.Log($"[NumberIslandStage] Exited stage {StageIndex}");
            await UniTask.CompletedTask;
        }

        public void RefreshStage()
        {
            if (_cells == null) return;

            _soundService?.PlaySFX("Reset");
            _historyManager.RecordState(SerializeState());

            for (int r = 0; r < _rowCount; r++)
            {
                for (int c = 0; c < _colCount; c++)
                {
                    _cells[r, c].Refresh();
                }
            }

            _stampService?.HideStamp();
            _isStageCleared.Value = false;

            // 리프레시 후에도 실시간 보드 타일 상태 즉시 갱신
            _validator.ValidateIslandGrid(_cells, _rowCount, _colCount);
        }

        public void Undo()
        {
            if (!CanUndo) return;
            string currentState = SerializeState();
            string previousState = _historyManager.Undo(currentState);
            DeserializeAndRestoreState(previousState);

            _stampService?.HideStamp();
            _isStageCleared.Value = false;
            _validator.ValidateIslandGrid(_cells, _rowCount, _colCount);
        }

        public void Redo()
        {
            if (!CanRedo) return;
            string currentState = SerializeState();
            string nextState = _historyManager.Redo(currentState);
            DeserializeAndRestoreState(nextState);

            _stampService?.HideStamp();
            _isStageCleared.Value = false;
            _validator.ValidateIslandGrid(_cells, _rowCount, _colCount);
        }

        public void ClearHistory()
        {
            _historyManager.Clear();
        }

        public string SerializeState()
        {
            if (_cells == null) return string.Empty;
            System.Text.StringBuilder sb = new System.Text.StringBuilder(_rowCount * _colCount);
            for (int r = 0; r < _rowCount; r++)
            {
                for (int c = 0; c < _colCount; c++)
                {
                    sb.Append((int)_cells[r, c].cellType.Value);
                }
            }
            return sb.ToString();
        }

        public void DeserializeAndRestoreState(string stateData)
        {
            if (_cells == null || string.IsNullOrEmpty(stateData)) return;

            int idx = 0;
            for (int r = 0; r < _rowCount; r++)
            {
                for (int c = 0; c < _colCount; c++)
                {
                    if (idx < stateData.Length)
                    {
                        int typeVal = stateData[idx] - '0';
                        _cells[r, c].SetType((CellType)typeVal);
                        idx++;
                    }
                }
            }

            // 캐시된 상태 복원 후 현재 보드 배치에 맞춰 정오 표기 갱신
            _validator.ValidateIslandGrid(_cells, _rowCount, _colCount);
        }

        private void OnDestroy()
        {
            _disposables.Dispose();
        }
    }
}
