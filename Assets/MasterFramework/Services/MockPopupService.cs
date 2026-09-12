using Cysharp.Threading.Tasks;
using UnityEngine;
using MasterFramework.Core;

namespace MasterFramework.Services
{
    public class MockPopupService : IPopupService
    {
        public async UniTask<bool> ShowPopupAsync(PopupMessageType type, string title, string message, string confirmText = "OK", string cancelText = "Cancel")
        {
            Debug.Log($"[MockPopup] Displaying modal popup of type '{type}':\nTitle: {title}\nMessage: {message}\nButtons: [{confirmText}] [{cancelText}]");
            
            // Simulate a brief delay to mimic user thinking and clicking
            await UniTask.Delay(500);
            
            Debug.Log("[MockPopup] Auto-clicked confirmation button 'OK'.");
            return true;
        }

        public void ShowToast(string message, float durationSeconds = 2f)
        {
            Debug.Log($"[MockPopup] TOAST ({durationSeconds}s): {message}");
        }
    }
}
