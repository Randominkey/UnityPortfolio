using System.Collections.Generic;
using MasterFramework.Interaction;

namespace MasterFramework.Core
{
    /// <summary>
    /// 클리어 팝업/스탬프 피드백 신호 발생 타이밍 모드
    /// (타일의 시각적 피드백/실시간 보드 체크와는 별개로 최종 완료 피드백을 언제 띄울지 제어)
    /// </summary>
    public enum StageClearTriggerMode
    {
        ManualSubmitButton = 0, // '정답 확인' 버튼 클릭 시 최종 클리어 팝업/스탬프 발생 (기본값)
        AutoInstantOnSolved = 1 // 모든 조건 만족 시 즉시 클리어 팝업/스탬프 발생
    }

    public enum ValidationCapability
    {
        ManualOnly = 0,    // 수동 버튼 전용 게임
        AutoOnly = 1,      // 실시간 자동 전용 게임
        Flexible = 2       // 런타임/인스펙터 전환 자유 지원 (기본값)
    }

    /// <summary>
    /// Contract for stages that support switching between Instant Auto-Clear on solved and Manual Submit button.
    /// </summary>
    public interface IStageClearConfigurable
    {
        StageClearTriggerMode ClearTriggerMode { get; set; }
        ValidationCapability Capability { get; }
        void SetClearTriggerMode(StageClearTriggerMode mode);
    }

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
