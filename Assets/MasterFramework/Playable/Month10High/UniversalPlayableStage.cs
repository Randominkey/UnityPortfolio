using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using MasterFramework.Core;
using MasterFramework.Interaction;
using UnityEngine.InputSystem;
using Zenject;

namespace MasterFramework.Playable.Month10High
{
    public class UniversalPlayableStage : MonoBehaviour, IStageOrganizer, IStateRestorable, ICheckableStage
    {
        [Header("Template Prefabs")]
        [SerializeField] private PlayableRectSquareCell cellPrefab;
        [SerializeField] private PlayableRectSquareFrame framePrefab;
        private bool _autoClear = false;

        [Header("UI References")]
        [SerializeField] private Transform cellParent;
        [SerializeField] private Button checkAnswerButton;

        [Header("Services Binding")]
        private ISoundService _soundService;
        private IPopupService _popupService;
        private IFeedbackStampService _stampService;

        public int StageIndex { get; set; }
        
        private readonly BoolReactiveProperty _isStageCleared = new BoolReactiveProperty(false);
        public IReadOnlyReactiveProperty<bool> IsStageCleared => _isStageCleared;

        // 주입받을 레벨 데이터
        private PlayableLevelData _levelData;
        private RectSquareGridSystem _gridSystem;
        private IRuleValidator _ruleValidator;

        private PlayableRectSquareCell[,] _cells;
        private List<PlayableRectSquareFrame> _presetFrames = new List<PlayableRectSquareFrame>();
        private List<PlayableRectSquareFrame> _createdFrames = new List<PlayableRectSquareFrame>();
        private PlayableRectSquareFrame _currentEditingFrame = null;

        private CompositeDisposable _disposables = new CompositeDisposable();

        [Inject]
        public void Construct(
            [InjectOptional] ISoundService soundService = null, 
            [InjectOptional] IPopupService popupService = null, 
            [InjectOptional] IFeedbackStampService stampService = null,
            [InjectOptional] IRuleValidator ruleValidator = null)
        {
            _soundService = soundService;
            _popupService = popupService;
            _stampService = stampService;
            if (ruleValidator != null)
            {
                _ruleValidator = ruleValidator;
            }
        }

        private void Awake()
        {
            EnsurePrefabsLoaded();
        }

        public void EnsurePrefabsLoaded()
        {
            if (cellPrefab == null)
            {
#if UNITY_EDITOR
                cellPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<PlayableRectSquareCell>("Assets/MasterFramework/Playable/Month10High/AnyResources/PlayableRectSquareCell.prefab");
#endif
                if (cellPrefab == null)
                    cellPrefab = Resources.Load<PlayableRectSquareCell>("PlayableRectSquareCell");
            }

            if (framePrefab == null)
            {
#if UNITY_EDITOR
                framePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<PlayableRectSquareFrame>("Assets/MasterFramework/Playable/Month10High/AnyResources/PlayableRectSquareFrame.prefab");
#endif
                if (framePrefab == null)
                    framePrefab = Resources.Load<PlayableRectSquareFrame>("PlayableRectSquareFrame");
            }
        }

        public void SetStageData(ScriptableObject data)
        {
            _levelData = data as PlayableLevelData;
            if (_levelData == null && data != null)
            {
                Debug.LogError($"[UniversalPlayableStage] Cast failed. Expected PlayableLevelData but got {data.GetType().Name}");
            }
        }

        public async UniTask InitializeStageAsync(int index)
        {
            StageIndex = index;
            _isStageCleared.Value = false;

            if (_levelData == null)
            {
                Debug.LogError("[UniversalPlayableStage] LevelData is not set! Call SetStageData() before InitializeStageAsync().");
                return;
            }

            // 1. 기존 생성물 제거 (재진입 대응)
            CleanOldLayout();

            // 2. 그리드 레이아웃 및 룰 검증기 설정
            _gridSystem = gameObject.GetComponent<RectSquareGridSystem>();
            if (_gridSystem == null)
                _gridSystem = gameObject.AddComponent<RectSquareGridSystem>();

            // 룰 팩토리 분기 (주입된 룰이 없으면 fallback)
            if (_ruleValidator == null)
            {
                _ruleValidator = CreateRuleValidator(_levelData.validationRule);
            }

            // 3. 그리드 셀 동적 배치
            int rows = _levelData.rows;
            int cols = _levelData.cols;
            _cells = new PlayableRectSquareCell[rows, cols];

            // Yield a frame to ensure the RectTransform has fully stretched and positioned
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);

            ConfigureGridLayout(rows, cols);

            for (int r = 0; r < rows; r++)
            {
                int rowIndex = r;
                for (int c = 0; c < cols; c++)
                {
                    int colIndex = c;
                    var cell = Instantiate(cellPrefab, cellParent);
                    cell.name = $"Cell_{rowIndex}_{colIndex}";
                    _cells[rowIndex, colIndex] = cell;

                    int signVal = 0;
                    if (rowIndex < _levelData.signMap.Count && colIndex < _levelData.signMap[rowIndex].Length)
                    {
                        signVal = int.Parse(_levelData.signMap[rowIndex][colIndex].ToString());
                    }
                    cell.Ready(rowIndex, colIndex, signVal);

                    cell.OnPointerDownCallback = OnCellPointerDown;
                    cell.OnPointerEnterCallback = OnCellPointerEnter;
                }
            }

            _gridSystem.InitializeGrid(_cells);

            // Force layout update on cellParent to resolve cell positions instantly before spawning preset frames
            if (cellParent != null)
            {
                var cellParentRect = cellParent.GetComponent<RectTransform>();
                LayoutRebuilder.ForceRebuildLayoutImmediate(cellParentRect);
            }

            // 4. 고정 프레임(힌트) 생성
            CreatePresetFrames();

            // 5. 정답 체크 버튼 구독
            if (checkAnswerButton != null)
            {
                checkAnswerButton.OnClickAsObservable()
                    .Subscribe(_ => CheckAnswerAndSubmit())
                    .AddTo(_disposables);
            }

            await UniTask.CompletedTask;
        }

        private void CleanOldLayout()
        {
            _disposables.Clear();
            
            foreach (var frame in _createdFrames)
            {
                if (frame != null) Destroy(frame.gameObject);
            }
            _createdFrames.Clear();

            foreach (var pf in _presetFrames)
            {
                if (pf != null) Destroy(pf.gameObject);
            }
            _presetFrames.Clear();

            if (cellParent != null)
            {
                for (int i = cellParent.childCount - 1; i >= 0; i--)
                {
                    Destroy(cellParent.GetChild(i).gameObject);
                }
            }
            
            _currentEditingFrame = null;
        }

        private IRuleValidator CreateRuleValidator(RuleType ruleType)
        {
            switch (ruleType)
            {
                case RuleType.RectSquareDivision:
                    return new RectSquareRuleValidator();
                default:
                    Debug.LogWarning($"[UniversalPlayableStage] RuleType {ruleType} is not fully integrated yet, falling back to RectSquareDivision.");
                    return new RectSquareRuleValidator();
            }
        }

        private void ConfigureGridLayout(int rows, int cols)
        {
            if (cellParent == null) return;

            var grid = cellParent.GetComponent<GridLayoutGroup>();
            if (grid == null)
                grid = cellParent.gameObject.AddComponent<GridLayoutGroup>();

            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = cols;
            grid.spacing = new Vector2(_levelData.cellSpacing, _levelData.cellSpacing);

            // Force rebuild of UI layouts to get up-to-date Rect dimensions
            var parentRect = cellParent.GetComponent<RectTransform>();
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);

            float totalWidth = parentRect.rect.width;
            float totalHeight = parentRect.rect.height;

            float spacingWidth = _levelData.cellSpacing * (cols - 1);
            float spacingHeight = _levelData.cellSpacing * (rows - 1);

            float cellWidth = (totalWidth - spacingWidth) / cols;
            float cellHeight = (totalHeight - spacingHeight) / rows;

            // 격자가 찌그러지지 않도록 정형 비율(정사각형) 유지
            float finalCellSize = Mathf.Min(cellWidth, cellHeight);
            grid.cellSize = new Vector2(finalCellSize, finalCellSize);

            // 레이아웃 중앙 정렬 설정
            grid.childAlignment = TextAnchor.MiddleCenter;
        }

        public async UniTask EnterStageAsync()
        {
            Debug.Log($"[UniversalPlayableStage] Entered stage {StageIndex}");
            await UniTask.CompletedTask;
        }

        public async UniTask ExitStageAsync()
        {
            Debug.Log($"[UniversalPlayableStage] Exiting stage {StageIndex}");
            await UniTask.CompletedTask;
        }

        public void RefreshStage()
        {
            foreach (var frame in _createdFrames)
            {
                if (frame != null) Destroy(frame.gameObject);
            }
            _createdFrames.Clear();

            _gridSystem.ClearAllContained();
            CreatePresetFrames();
            _stampService?.HideStamp();
            _isStageCleared.Value = false;
        }

        private void CreatePresetFrames()
        {
            foreach (var pf in _presetFrames)
            {
                if (pf != null) Destroy(pf.gameObject);
            }
            _presetFrames.Clear();

            if (_levelData == null || _levelData.presetFrames == null || framePrefab == null) return;

            for (int i = 0; i < _levelData.presetFrames.Length; i++)
            {
                var start = _levelData.presetFrames[i].startIdices;
                var end = _levelData.presetFrames[i].endIdices;

                if (start.x >= _cells.GetLength(0) || start.y >= _cells.GetLength(1) ||
                    end.x >= _cells.GetLength(0) || end.y >= _cells.GetLength(1))
                    continue;

                PlayableRectSquareFrame pf = Instantiate(framePrefab, transform);
                pf.name = "Presetted Frame";
                pf.Ready(_cells[start.x, start.y]);
                pf.IsDeletable = false;

                UpdateCellAndFrameState(pf, _cells[end.x, end.y]);
                pf.FixAndSet();

                _presetFrames.Add(pf);
            }

            ReevaluateAllCellOccupation();
        }

        private void OnCellPointerDown(PlayableRectSquareCell cell)
        {
            if (cell.IsContained) return;

            if (framePrefab == null)
            {
                EnsurePrefabsLoaded();
            }

            if (framePrefab == null)
            {
                Debug.LogError("[UniversalPlayableStage] framePrefab is null and could not be loaded!");
                return;
            }

            PlayableRectSquareFrame newFrame = Instantiate(framePrefab, transform);
            newFrame.Ready(cell);
            newFrame.NeedUpdate = true;

            UpdateCellAndFrameState(newFrame, cell);
            _currentEditingFrame = newFrame;
        }

        private void OnCellPointerEnter(PlayableRectSquareCell cell)
        {
            if (cell.IsContained) return;
            if (_currentEditingFrame != null)
            {
                UpdateCellAndFrameState(_currentEditingFrame, cell);
            }
        }

        private void UpdateCellAndFrameState(PlayableRectSquareFrame frame, PlayableRectSquareCell endCell)
        {
            bool isOverlap = _gridSystem.IsOccupiedOrInvalid(frame, endCell.RectTransform.position);

            if (!isOverlap)
            {
                frame.EndCell = endCell;
                frame.NeedUpdate = true;

                var allPieces = new List<IInteractable>(_createdFrames);
                allPieces.Add(frame);
                foreach (var pf in _presetFrames) allPieces.Add(pf);

                var result = _ruleValidator.Validate(allPieces.ToArray(), _gridSystem);

                if (result.offendingElementIds.Contains(frame.UniqueId))
                {
                    frame.State.Value = ObjectState.Error;
                }
                else
                {
                    frame.State.Value = ObjectState.Correct;
                }
            }
            else
            {
                frame.State.Value = ObjectState.Error;
            }
        }

        private void Update()
        {
            if (Pointer.current != null && Pointer.current.press.wasReleasedThisFrame)
            {
                if (_currentEditingFrame != null)
                {
                    if (_currentEditingFrame.State.Value == ObjectState.Correct)
                    {
                        var lockedFrame = _currentEditingFrame;
                        lockedFrame.FixAndSet();

                        lockedFrame.OnDestroyCallback = (f) =>
                        {
                            _createdFrames.Remove(f);
                            ReevaluateAllCellOccupation();
                        };

                        _createdFrames.Add(lockedFrame);
                        ReevaluateAllCellOccupation();
                    }
                    else
                    {
                        Destroy(_currentEditingFrame.gameObject);
                    }
                    _currentEditingFrame = null;
                }
            }
        }

        private void ReevaluateAllCellOccupation()
        {
            for (int r = 0; r < _cells.GetLength(0); r++)
            {
                for (int c = 0; c < _cells.GetLength(1); c++)
                {
                    _cells[r, c].IsContained = false;
                    _cells[r, c].ResetState();
                    _cells[r, c].SetPresetBackgroundColor(false);
                }
            }

            foreach (var pf in _presetFrames) MarkFrameOccupied(pf);
            foreach (var cf in _createdFrames) MarkFrameOccupied(cf);

            if (_autoClear && !_isStageCleared.Value && _gridSystem != null && _ruleValidator != null)
            {
                bool allOccupied = _gridSystem.CheckAllContained();
                var allPieces = new List<IInteractable>(_createdFrames);
                foreach (var pf in _presetFrames) allPieces.Add(pf);
                var res = _ruleValidator.Validate(allPieces.ToArray(), _gridSystem);
                if (allOccupied && res.isCorrect)
                {
                    CheckAnswerAndSubmit();
                }
            }
        }

        #region ICheckableStage
        public bool CheckAnswer()
        {
            CheckAnswerAndSubmit();
            return _isStageCleared.Value;
        }

        public void SetAutoClearMode(bool isAutoClear)
        {
            _autoClear = isAutoClear;
            if (_autoClear && !_isStageCleared.Value && _cells != null)
            {
                ReevaluateAllCellOccupation();
            }
        }
        #endregion

        private void MarkFrameOccupied(PlayableRectSquareFrame frame)
        {
            if (frame.StartCell == null || frame.EndCell == null) return;

            int rMin = Mathf.Min(frame.StartCell.RowIndex, frame.EndCell.RowIndex);
            int rMax = Mathf.Max(frame.StartCell.RowIndex, frame.EndCell.RowIndex);
            int cMin = Mathf.Min(frame.StartCell.ColumnIndex, frame.EndCell.ColumnIndex);
            int cMax = Mathf.Max(frame.StartCell.ColumnIndex, frame.EndCell.ColumnIndex);

            for (int r = rMin; r <= rMax; r++)
            {
                for (int c = cMin; c <= cMax; c++)
                {
                    _cells[r, c].IsContained = true;
                    _cells[r, c].State.Value = ObjectState.Correct;
                    if (!frame.IsDeletable)
                    {
                        _cells[r, c].SetPresetBackgroundColor(true);
                    }
                }
            }
        }

        private void CheckAnswerAndSubmit()
        {
            bool allOccupied = _gridSystem.CheckAllContained();

            var allPieces = new List<IInteractable>(_createdFrames);
            foreach (var pf in _presetFrames) allPieces.Add(pf);

            var result = _ruleValidator.Validate(allPieces.ToArray(), _gridSystem);

            if (allOccupied && result.isCorrect)
            {
                _isStageCleared.Value = true;
                _soundService?.PlaySFX("Complete");
                _popupService?.ShowToast("완벽하게 분할했습니다!");
                _stampService?.ShowStampAsync(StampType.Complete).Forget();
            }
            else
            {
                _isStageCleared.Value = false;
                _soundService?.PlaySFX("Fail");
                _popupService?.ShowToast("분할 규칙을 만족하지 못했거나 빈 공간이 있습니다.");
                _stampService?.ShowStampAsync(StampType.Fail).Forget();
            }
        }

        public string SerializeState()
        {
            var dataList = new List<FrameSaveData>();
            foreach (var f in _createdFrames)
            {
                dataList.Add(new FrameSaveData
                {
                    startRow = f.StartCell.RowIndex,
                    startCol = f.StartCell.ColumnIndex,
                    endRow = f.EndCell.RowIndex,
                    endCol = f.EndCell.ColumnIndex
                });
            }

            var root = new MementoStateRoot { frames = dataList };
            return JsonUtility.ToJson(root);
        }

        public void DeserializeAndRestoreState(string stateData)
        {
            if (string.IsNullOrEmpty(stateData)) return;

            try
            {
                var root = JsonUtility.FromJson<MementoStateRoot>(stateData);

                foreach (var f in _createdFrames)
                {
                    if (f != null) Destroy(f.gameObject);
                }
                _createdFrames.Clear();

                foreach (var save in root.frames)
                {
                    PlayableRectSquareFrame f = Instantiate(framePrefab, transform);
                    f.Ready(_cells[save.startRow, save.startCol]);
                    f.EndCell = _cells[save.endRow, save.endCol];
                    f.NeedUpdate = true;
                    f.FixAndSet();
                    f.State.Value = ObjectState.Correct;

                    f.OnDestroyCallback = (target) =>
                    {
                        _createdFrames.Remove(target);
                        ReevaluateAllCellOccupation();
                    };

                    _createdFrames.Add(f);
                }

                ReevaluateAllCellOccupation();
                Debug.Log($"[UniversalPlayableStage] Deserialized and Restored {_createdFrames.Count} frames successfully.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UniversalPlayableStage] Failed to restore state: {ex.Message}");
            }
        }

        /// <summary>
        /// 포트폴리오 면접관/시연자를 위한 Auto-Solve 기능.
        /// </summary>
        public void AutoSolve()
        {
            _isStageCleared.Value = true;
            _soundService?.PlaySFX("Complete");
            _popupService?.ShowToast("정답입니다! (Auto-Solved)");
            _stampService?.ShowStampAsync(StampType.Complete).Forget();
            Debug.Log($"[UniversalPlayableStage] Stage {StageIndex} Auto-Solved!");
        }

        [Serializable]
        private class FrameSaveData
        {
            public int startRow;
            public int startCol;
            public int endRow;
            public int endCol;
        }

        [Serializable]
        private class MementoStateRoot
        {
            public List<FrameSaveData> frames;
        }

        private void OnDestroy()
        {
            _disposables.Dispose();
        }
    }
}
