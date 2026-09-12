using System;
using Cysharp.Threading.Tasks;

namespace MasterFramework.Core
{
    public interface ISoundService
    {
        void PlaySFX(string clipName);
        void PlayBGM(string clipName);
        void StopBGM();
        void SetVolume(string channel, float volume); // E.g., "BGM", "SFX"
    }

    public enum PopupMessageType
    {
        Confirm,
        Alert,
        Warning
    }

    public interface IPopupService
    {
        /// <summary>
        /// Shows a modal confirmation popup with custom action callbacks.
        /// </summary>
        UniTask<bool> ShowPopupAsync(PopupMessageType type, string title, string message, string confirmText = "OK", string cancelText = "Cancel");

        /// <summary>
        /// Displays a brief non-modal toast message at the bottom of the screen.
        /// </summary>
        void ShowToast(string message, float durationSeconds = 2.0f);
    }

    public enum StampType
    {
        Complete,
        Fail
    }

    public interface IFeedbackStampService
    {
        /// <summary>
        /// Triggers the visual and audial stamp feedback (Success/Complete or Fail/Incorrect).
        /// </summary>
        UniTask ShowStampAsync(StampType type);

        /// <summary>
        /// Hides the active stamp overlay.
        /// </summary>
        void HideStamp();
    }
}
