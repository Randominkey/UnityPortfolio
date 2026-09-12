using System.Collections.Generic;
using MasterFramework.Interaction;

namespace MasterFramework.Core
{
    [System.Serializable]
    public struct ValidationResult
    {
        public bool isCorrect;
        public string feedbackMessage;
        
        // References to pieces/cells that caused the error (for red outline / shake animations in UI)
        public List<string> offendingElementIds;

        public ValidationResult(bool isCorrect, string feedbackMessage = "", List<string> offendingElementIds = null)
        {
            this.isCorrect = isCorrect;
            this.feedbackMessage = feedbackMessage;
            this.offendingElementIds = offendingElementIds ?? new List<string>();
        }

        public static ValidationResult Correct => new ValidationResult(true);
        public static ValidationResult Incorrect(string message = "", List<string> ids = null) => new ValidationResult(false, message, ids);
    }

    /// <summary>
    /// Contract for validating user-placed pieces against puzzle logic constraints
    /// (e.g. Summation Checker, Grid Connectivity Checkers, Sudoku Checker).
    /// </summary>
    public interface IRuleValidator
    {
        string RuleName { get; }
        
        /// <summary>
        /// Validates the current layout of the interactive pieces on a grid.
        /// </summary>
        ValidationResult Validate(IInteractable[] pieces, IGridSystem gridSystem);
    }
}
