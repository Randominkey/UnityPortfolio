using Cysharp.Threading.Tasks;
using UnityEngine;
using MasterFramework.Core;

namespace MasterFramework.Services
{
    public class MockFeedbackStampService : IFeedbackStampService
    {
        public async UniTask ShowStampAsync(StampType type)
        {
            Debug.Log($"[MockFeedbackStamp] >>> STAMP SHOWN: {type.ToString().ToUpper()} <<<");
            
            // Simulate stamp visual overlay duration before fade-out/completion
            await UniTask.Delay(1000);
            
            Debug.Log($"[MockFeedbackStamp] Stamp visual transition completed.");
        }

        public void HideStamp()
        {
            Debug.Log("[MockFeedbackStamp] Stamp overlay hidden.");
        }
    }
}
