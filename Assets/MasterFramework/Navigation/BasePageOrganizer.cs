using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UniRx;
using MasterFramework.Core;
using MasterFramework.Interaction;

namespace MasterFramework.Navigation
{
    public class BasePageOrganizer : MonoBehaviour, IPageOrganizer
    {
        [Header("Interaction Setup")]
        [SerializeField] protected DragDropSystem dragDropSystem;
        
        // Abstract components resolved at runtime or serialized
        [SerializeField] protected GameObject gridSystemObject;
        [SerializeField] protected List<GameObject> pieceObjects = new List<GameObject>();

        protected IGridSystem GridSystem;
        protected List<IInteractable> Pieces = new List<IInteractable>();

        [Header("Rule Validators")]
        [SerializeField] protected List<ScriptableObject> serializedRules = new List<ScriptableObject>();
        protected readonly List<IRuleValidator> RuleValidators = new List<IRuleValidator>();

        // Reactive Properties
        public int PageIndex { get; set; }
        public IReadOnlyReactiveProperty<StepProgressState> ProgressState => _progressState;
        private readonly ReactiveProperty<StepProgressState> _progressState = new ReactiveProperty<StepProgressState>(StepProgressState.Ready);

        public virtual async UniTask InitializePageAsync(int index)
        {
            PageIndex = index;
            _progressState.Value = StepProgressState.Ready;

            // Resolve interfaces from GameObjects
            if (gridSystemObject != null)
            {
                GridSystem = gridSystemObject.GetComponent<IGridSystem>();
            }

            Pieces.Clear();
            foreach (var pObj in pieceObjects)
            {
                if (pObj != null)
                {
                    var interactable = pObj.GetComponent<IInteractable>();
                    if (interactable != null)
                    {
                        Pieces.Add(interactable);
                    }
                }
            }

            // Register grid system into drag-drop system
            if (dragDropSystem != null && GridSystem != null)
            {
                dragDropSystem.RegisterGridSystem(GridSystem);
            }

            // Parse rules implementing IRuleValidator from ScriptableObjects
            RuleValidators.Clear();
            foreach (var ruleAsset in serializedRules)
            {
                if (ruleAsset is IRuleValidator validator)
                {
                    RuleValidators.Add(validator);
                }
            }

            await UniTask.CompletedTask;
        }

        public virtual void RefreshPage()
        {
            // Release grid occupancy and snap pieces back to initial placements
            if (GridSystem != null)
            {
                foreach (var piece in Pieces)
                {
                    GridSystem.ReleaseCells(piece);
                    piece.SnapTo(Vector3.zero); // Or reset to a cached local position
                }
            }

            _progressState.Value = StepProgressState.Ready;
            Debug.Log($"[Page {PageIndex}] Refreshed page state.");
        }

        public virtual bool CheckAnswer()
        {
            if (RuleValidators.Count == 0)
            {
                Debug.LogWarning($"[Page {PageIndex}] No rule validators configured. Auto-clearing answer.");
                _progressState.Value = StepProgressState.Complete;
                return true;
            }

            IInteractable[] piecesArr = Pieces.ToArray();
            foreach (var validator in RuleValidators)
            {
                var result = validator.Validate(piecesArr, GridSystem);
                if (!result.isCorrect)
                {
                    _progressState.Value = StepProgressState.Fail;
                    
                    // Trigger visual feedback (shake or outline errors) using result.offendingElementIds
                    TriggerErrorFeedback(result);
                    return false;
                }
            }

            _progressState.Value = StepProgressState.Complete;
            return true;
        }

        protected virtual void TriggerErrorFeedback(ValidationResult result)
        {
            Debug.LogWarning($"[Page {PageIndex}] Validation Failed: {result.feedbackMessage}");
            // E.g., apply red outline shaders to result.offendingElementIds
        }
    }
}
