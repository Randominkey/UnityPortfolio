using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using UniRx;
using Cysharp.Threading.Tasks;
using MasterFramework.Core;
using MiniGame;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MasterFramework.Playable.Common
{
    /// <summary>
    /// MiniGameBoardBase 기반의 모든 미니게임(Gen 2 챌린지 퍼즐 등)을 100% 자동 연결하는 범용 스테이지 어댑터
    /// </summary>
    [RequireComponent(typeof(GameObjectContext))]
    [RequireComponent(typeof(UniversalMinigameInstaller))]
    public class UniversalMinigameStage : MonoBehaviour, IStageOrganizer, IStateRestorable, IUndoableStage
    {
        [Header("Polymorphic Board Component (MiniGameBoardBase)")]
        [SerializeField] private MiniGameBoardBase board;

        [Header("UI Hierarchy References (Auto-Mapped)")]
        [SerializeField] private Button checkAnswerButton;
        [SerializeField] private Button refreshButton;
        [SerializeField] private Button ruleButton;
        [SerializeField] private GameObject popupObject;
        [SerializeField] private Button popupCloseButton;
        [SerializeField] private Image levelHeaderImage;
        [SerializeField] private Sprite[] levelSprites;

        [Header("Injected Level Data (Optional ScriptableObject)")]
        [SerializeField] private ScriptableObject levelData;

        private ISoundService soundService;
        private IPopupService popupService;
        private IFeedbackStampService stampService;

        public int StageIndex { get; set; }
        private readonly BoolReactiveProperty _isStageCleared = new BoolReactiveProperty(false);
        public IReadOnlyReactiveProperty<bool> IsStageCleared => _isStageCleared;

        private readonly CompositeDisposable disposables = new CompositeDisposable();
        private readonly HistoryManager<string> _history = new HistoryManager<string>();
        private bool _isRestoringHistory = false;

        public bool CanUndo => _history.CanUndo;
        public bool CanRedo => _history.CanRedo;
        public IObservable<Unit> OnHistoryChanged => _history.OnHistoryChanged;

        public void Undo()
        {
            if (CanUndo)
            {
                _isRestoringHistory = true;
                string current = SerializeState();
                string prev = _history.Undo(current);
                DeserializeAndRestoreState(prev);
                _isRestoringHistory = false;
            }
        }

        public void Redo()
        {
            if (CanRedo)
            {
                _isRestoringHistory = true;
                string current = SerializeState();
                string next = _history.Redo(current);
                DeserializeAndRestoreState(next);
                _isRestoringHistory = false;
            }
        }

        public void ClearHistory() => _history.Clear();

        [Inject]
        public void Construct(
            ISoundService soundService,
            IPopupService popupService,
            IFeedbackStampService stampService)
        {
            this.soundService = soundService;
            this.popupService = popupService;
            this.stampService = stampService;
        }

        private void Awake()
        {
            AutoBindReferencesIfNull();
            EnsureValidFonts();
        }

        /// <summary>
        /// 런타임에 폰트 에셋이 누락(Missing)된 Text 컴포넌트에 안전하게 내장 폰트(LegacyRuntime.ttf)를 폴백 주입
        /// (디스크 상의 프리팹 에셋 GUID는 손상시키지 않고 인메모리 런타임에만 적용)
        /// </summary>
        private void EnsureValidFonts()
        {
            var fallbackFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (fallbackFont == null) return;

            var texts = GetComponentsInChildren<Text>(true);
            foreach (var txt in texts)
            {
                if (txt != null && txt.font == null)
                {
                    txt.font = fallbackFont;
                }
            }
        }

#if UNITY_EDITOR
        private void Reset()
        {
            AutoConfigureContextAndInstaller();
            AutoBindReferencesIfNull();
        }

        private void OnValidate()
        {
            AutoConfigureContextAndInstaller();
            AutoBindReferencesIfNull();
        }

        /// <summary>
        /// GameObjectContext의 Mono Installers에 UniversalMinigameInstaller 자동 등록
        /// </summary>
        private void AutoConfigureContextAndInstaller()
        {
            var ctx = GetComponent<GameObjectContext>();
            var installer = GetComponent<UniversalMinigameInstaller>();

            if (ctx != null && installer != null)
            {
                var serializedCtx = new SerializedObject(ctx);
                var installersProp = serializedCtx.FindProperty("_monoInstallers");
                if (installersProp != null)
                {
                    bool alreadyExists = false;
                    for (int i = 0; i < installersProp.arraySize; i++)
                    {
                        if (installersProp.GetArrayElementAtIndex(i).objectReferenceValue == installer)
                        {
                            alreadyExists = true;
                            break;
                        }
                    }

                    if (!alreadyExists)
                    {
                        installersProp.arraySize = 1;
                        installersProp.GetArrayElementAtIndex(0).objectReferenceValue = installer;
                        serializedCtx.ApplyModifiedProperties();
                    }
                }
            }
        }
#endif

        /// <summary>
        /// 하이어라키에서 Board, Button, Popup 등을 이름 기반으로 100% 자동 탐색 매핑
        /// </summary>
        public void AutoBindReferencesIfNull()
        {
            if (board == null)
            {
                board = GetComponentInChildren<MiniGameBoardBase>(true);
            }

            var allButtons = GetComponentsInChildren<Button>(true);
            foreach (var btn in allButtons)
            {
                string lowerName = btn.name.ToLower();
                if (checkAnswerButton == null && (lowerName.Contains("check") || lowerName.Contains("answer") || lowerName.Contains("correct")))
                {
                    checkAnswerButton = btn;
                }
                else if (refreshButton == null && (lowerName.Contains("refresh") || lowerName.Contains("retry") || lowerName.Contains("reset")))
                {
                    refreshButton = btn;
                }
                else if (ruleButton == null && (lowerName.Contains("rule") || lowerName.Contains("help") || lowerName.Contains("guide")))
                {
                    ruleButton = btn;
                }
            }

            if (popupObject == null)
            {
                var allTransforms = GetComponentsInChildren<Transform>(true);
                foreach (var t in allTransforms)
                {
                    if (t.name.ToLower().Contains("popup") && t != transform)
                    {
                        popupObject = t.gameObject;
                        break;
                    }
                }
            }

            if (popupObject != null && popupCloseButton == null)
            {
                var popupButtons = popupObject.GetComponentsInChildren<Button>(true);
                foreach (var btn in popupButtons)
                {
                    string lowerName = btn.name.ToLower();
                    if (lowerName.Contains("x") || lowerName.Contains("close") || lowerName.Contains("exit"))
                    {
                        popupCloseButton = btn;
                        break;
                    }
                }
            }

            if (levelHeaderImage == null)
            {
                var allImages = GetComponentsInChildren<Image>(true);
                foreach (var img in allImages)
                {
                    if (img.name.ToLower().Contains("level") || img.name.ToLower().Contains("header"))
                    {
                        levelHeaderImage = img;
                        break;
                    }
                }
            }
        }

        public void SetStageData(ScriptableObject data)
        {
            levelData = data;
        }

        public async UniTask InitializeStageAsync(int index)
        {
            StageIndex = index;
            _isStageCleared.Value = false;

            AutoBindReferencesIfNull();

            // 1. Level Header Sprite 업데이트
            if (levelHeaderImage != null && levelSprites != null && index >= 0 && index < levelSprites.Length)
            {
                levelHeaderImage.sprite = levelSprites[index];
            }

            // 2. 룰 팝업 기본 닫기
            if (popupObject != null)
            {
                popupObject.SetActive(false);
            }

            // 3. SO 레벨 데이터가 존재하면 보드 필드에 범용 리플렉션 자동 주입
            if (board != null && levelData != null)
            {
                InjectLevelDataToBoard(board, levelData);
            }

            // 4. 보드 초기화
            if (board != null)
            {
                board.Ready();
                EnsureValidFonts();
            }

            // 5. 이벤트 바인딩
            BindEvents();

            await UniTask.CompletedTask;
        }

        /// <summary>
        /// SO에 정의된 모든 필드 값을 MiniGameBoardBase 하위 구체 클래스의 동일 이름 필드에 자동 주입하는 범용 리플렉션 바인더
        /// </summary>
        private void InjectLevelDataToBoard(MiniGameBoardBase targetBoard, ScriptableObject data)
        {
            if (targetBoard == null || data == null) return;

            var boardType = targetBoard.GetType();
            var dataType = data.GetType();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

            var dataFields = dataType.GetFields(flags);
            foreach (var df in dataFields)
            {
                var targetField = boardType.GetField(df.Name, flags);
                if (targetField != null && targetField.FieldType.IsAssignableFrom(df.FieldType))
                {
                    object val = df.GetValue(data);
                    targetField.SetValue(targetBoard, val);
                }
            }
        }

        public async UniTask EnterStageAsync()
        {
            gameObject.SetActive(true);
            await UniTask.CompletedTask;
        }

        public async UniTask ExitStageAsync()
        {
            gameObject.SetActive(false);
            disposables.Clear();
            await UniTask.CompletedTask;
        }

        public void RefreshStage()
        {
            if (board != null)
            {
                board.Refresh();
            }
        }

        /// <summary>
        /// 게임 내 배치된 오리지널 그래픽 규칙 팝업 활성화
        /// </summary>
        public bool ShowRulePopup()
        {
            if (popupObject != null)
            {
                popupObject.SetActive(true);
                return true;
            }
            return false;
        }

        private void BindEvents()
        {
            disposables.Clear();

            // Check Answer Button (정답 확인)
            if (checkAnswerButton != null)
            {
                checkAnswerButton.OnClickAsObservable()
                    .Subscribe(_ => OnCheckAnswerClicked())
                    .AddTo(disposables);
            }

            // Refresh Button (새로고침)
            if (refreshButton != null)
            {
                refreshButton.OnClickAsObservable()
                    .Subscribe(_ => RefreshStage())
                    .AddTo(disposables);
            }

            // Rule Popup Buttons (규칙 팝업)
            if (ruleButton != null && popupObject != null)
            {
                ruleButton.OnClickAsObservable()
                    .Subscribe(_ => popupObject.SetActive(true))
                    .AddTo(disposables);
            }

            if (popupCloseButton != null && popupObject != null)
            {
                popupCloseButton.OnClickAsObservable()
                    .Subscribe(_ => popupObject.SetActive(false))
                    .AddTo(disposables);
            }
            
            if (board != null)
            {
                var boardType = board.GetType();
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
                var keypadField = boardType.GetField("neoKeypadComponent", flags);
                if (keypadField != null)
                {
                    var keypad = keypadField.GetValue(board) as CMS.Template.UI.Keypad.NeoKeypadComponent;
                    if (keypad != null)
                    {
                        var orig = keypad.OnButtonClicked;
                        keypad.OnButtonClicked = (type, name, str) =>
                        {
                            if (!_isRestoringHistory)
                            {
                                _history.RecordState(SerializeState());
                                AudioService.Instance.PlaySfx(SfxType.KeypadInput);
                            }
                            orig?.Invoke(type, name, str);
                        };
                    }
                }
            }
        }

        private void OnCheckAnswerClicked()
        {
            if (board == null) return;

            if (board.IsCorrect)
            {
                _isStageCleared.Value = true;
                soundService?.PlaySFX("sfx_stamp_win");
                popupService?.ShowToast("정답입니다! 완벽히 풀었습니다.", 1.5f);
                stampService?.ShowStampAsync(StampType.Complete).Forget();
                Debug.Log($"[UniversalMinigameStage] Stage {StageIndex} cleared!");
            }
            else
            {
                soundService?.PlaySFX("sfx_stamp_lose");
                AudioService.Instance.PlaySfx(SfxType.Error);
                popupService?.ShowToast("오답입니다. 배치 상태를 다시 확인하세요.", 1.5f);
                stampService?.ShowStampAsync(StampType.Fail).Forget();
                Debug.Log($"[UniversalMinigameStage] Stage {StageIndex} failed.");
            }
        }

        /// <summary>
        /// 포트폴리오 면접관/시연자를 위한 범용 Auto-Solve 기능.
        /// </summary>
        public void AutoSolve()
        {
            if (board == null) return;

            try
            {
                if (levelData != null)
                {
                    var solField = levelData.GetType().GetField("solutionGrid", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (solField != null)
                    {
                        var solGrid = solField.GetValue(levelData) as string[];
                        if (solGrid != null && solGrid.Length > 0)
                        {
                            ApplySolutionGrid(solGrid);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[UniversalMinigameStage] AutoSolve preset injection warning: {ex.Message}");
            }

            // Always ensure board is marked correct and triggers clear feedback
            var isCorrectProp = typeof(MiniGameBoardBase).GetProperty("IsCorrect", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (isCorrectProp != null && isCorrectProp.CanWrite)
            {
                isCorrectProp.SetValue(board, true);
            }
            else if (isCorrectProp != null)
            {
                var setter = isCorrectProp.GetSetMethod(true);
                setter?.Invoke(board, new object[] { true });
            }

            OnCheckAnswerClicked();
        }

        private void ApplySolutionGrid(string[] solGrid)
        {
            if (board == null || solGrid == null) return;

            var boardType = board.GetType();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            var innerCellsField = boardType.GetField("innerCells", flags) ?? boardType.GetField("cells", flags);

            if (innerCellsField != null)
            {
                var rawCells = innerCellsField.GetValue(board);
                if (rawCells is Array cellArray && cellArray.Rank == 2)
                {
                    int rows = cellArray.GetLength(0);
                    int cols = cellArray.GetLength(1);

                    for (int r = 0; r < rows && r < solGrid.Length; r++)
                    {
                        string solRow = solGrid[r];
                        for (int c = 0; c < cols && c < solRow.Length; c++)
                        {
                            object cellObj = cellArray.GetValue(r, c);
                            if (cellObj is Component cellComp)
                            {
                                char ch = solRow[c];
                                string cellText = (ch == 'x' || ch == ' ') ? "" : ch.ToString();

                                var nkltProp = cellComp.GetType().GetField("nKLT", flags);
                                if (nkltProp != null)
                                {
                                    var nklt = nkltProp.GetValue(cellComp) as CMS.Template.UI.Keypad.NeoKeypadLinkedToggleComponent;
                                    if (nklt != null && nklt.Label != null)
                                    {
                                        nklt.Label.text = cellText;
                                    }
                                }

                                var updateMethod = cellComp.GetType().GetMethod("UpdateInfoByText", flags);
                                if (updateMethod != null)
                                {
                                    updateMethod.Invoke(cellComp, null);
                                }
                            }
                        }
                    }
                }
            }

            // Call NeoKeypadButtonClicked or Refresh evaluation
            var method = boardType.GetMethod("NeoKeypadButtonClicked", flags);
            if (method != null)
            {
                var coroutine = method.Invoke(board, null) as System.Collections.IEnumerator;
                if (coroutine != null)
                {
                    StartCoroutine(coroutine);
                }
            }
        }

        public string SerializeState()
        {
            if (board == null) return string.Empty;
            var boardType = board.GetType();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            var innerCellsField = boardType.GetField("innerCells", flags) ?? boardType.GetField("cells", flags);
            if (innerCellsField == null) return string.Empty;

            var rawCells = innerCellsField.GetValue(board);
            if (rawCells is Array cellArray && cellArray.Rank == 2)
            {
                int rows = cellArray.GetLength(0);
                int cols = cellArray.GetLength(1);
                var rowStrings = new List<string>();
                for (int r = 0; r < rows; r++)
                {
                    var colStrings = new List<string>();
                    for (int c = 0; c < cols; c++)
                    {
                        object cellObj = cellArray.GetValue(r, c);
                        string val = " ";
                        if (cellObj is Component cellComp)
                        {
                            var nkltProp = cellComp.GetType().GetField("nKLT", flags);
                            if (nkltProp != null)
                            {
                                var nklt = nkltProp.GetValue(cellComp) as CMS.Template.UI.Keypad.NeoKeypadLinkedToggleComponent;
                                if (nklt != null && nklt.Label != null && !string.IsNullOrEmpty(nklt.Label.text))
                                {
                                    val = nklt.Label.text;
                                }
                            }
                        }
                        colStrings.Add(val);
                    }
                    rowStrings.Add(string.Join("", colStrings));
                }
                return string.Join("\n", rowStrings);
            }
            return string.Empty;
        }

        public void DeserializeAndRestoreState(string stateData)
        {
            if (string.IsNullOrEmpty(stateData)) return;
            string[] solGrid = stateData.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            ApplySolutionGrid(solGrid);
        }

        private void OnDestroy()
        {
            disposables.Dispose();
        }
    }
}
