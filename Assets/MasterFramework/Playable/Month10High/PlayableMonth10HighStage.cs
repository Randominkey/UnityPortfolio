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
    [System.Serializable]
    public class PresetFrameStartEndIndices
    {
        public Vector2Int startIdices;
        public Vector2Int endIdices;
    }

    public class PlayableMonth10HighStage : MonoBehaviour, IStageOrganizer, IStateRestorable
    {
        [Header("Configuration")]
        [SerializeField] private List<string> signMap;
        [SerializeField] private PresetFrameStartEndIndices[] presetFrameStartEndIndices;

        [Header("UI & References")]
        [SerializeField] private Transform cellParent;
        [SerializeField] private PlayableRectSquareFrame framePrefab;
        [SerializeField] private Button checkAnswerButton;

        [Header("Services Binding")]
        private ISoundService _soundService;
        private IPopupService _popupService;
        private IFeedbackStampService _stampService;

        public int StageIndex { get; set; }

        private readonly BoolReactiveProperty _isStageCleared = new BoolReactiveProperty(false);
        public IReadOnlyReactiveProperty<bool> IsStageCleared => _isStageCleared;

        private RectSquareGridSystem _gridSystem;
        private RectSquareRuleValidator _ruleValidator;

        private PlayableRectSquareCell[,] _cells;
        private List<PlayableRectSquareFrame> _presetFrames = new List<PlayableRectSquareFrame>();
        private List<PlayableRectSquareFrame> _createdFrames = new List<PlayableRectSquareFrame>();
        private PlayableRectSquareFrame _currentEditingFrame = null;

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

        public async UniTask InitializeStageAsync(int index)
        {
            StageIndex = index;
            _isStageCleared.Value = false;

            _gridSystem = gameObject.AddComponent<RectSquareGridSystem>();
            _ruleValidator = new RectSquareRuleValidator();

            int rows = signMap.Count;
            int cols = signMap[0].Length;
            _cells = new PlayableRectSquareCell[rows, cols];

            for (int r = 0; r < rows; r++)
            {
                int rowIndex = r;
                for (int c = 0; c < cols; c++)
                {
                    int colIndex = c;
                    int childIdx = rowIndex * cols + colIndex;
                    var cell = cellParent.GetChild(childIdx).GetComponent<PlayableRectSquareCell>();
                    _cells[rowIndex, colIndex] = cell;

                    int signVal = int.Parse(signMap[rowIndex][colIndex].ToString());
                    cell.Ready(rowIndex, colIndex, signVal);

                    cell.OnPointerDownCallback = OnCellPointerDown;
                    cell.OnPointerEnterCallback = OnCellPointerEnter;
                }
            }

            _gridSystem.InitializeGrid(_cells);

            CreatePresetFrames();

            checkAnswerButton.OnClickAsObservable()
                .Subscribe(_ => CheckAnswerAndSubmit())
                .AddTo(_disposables);

            await UniTask.CompletedTask;
        }

        public void SetStageData(UnityEngine.ScriptableObject data)
        {
            // Legacy stage script uses inspector mapping instead
        }

        public async UniTask EnterStageAsync()
        {
            Debug.Log($"[PlayableMonth10HighStage] Entered stage {StageIndex}");
            await UniTask.CompletedTask;
        }

        public async UniTask ExitStageAsync()
        {
            Debug.Log($"[PlayableMonth10HighStage] Exiting stage {StageIndex}");
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

            for (int i = 0; i < presetFrameStartEndIndices.Length; i++)
            {
                var start = presetFrameStartEndIndices[i].startIdices;
                var end = presetFrameStartEndIndices[i].endIdices;

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
        }

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
                Debug.Log($"[PlayableMonth10HighStage] Deserialized and Restored {_createdFrames.Count} frames successfully.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlayableMonth10HighStage] Failed to restore state: {ex.Message}");
            }
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
