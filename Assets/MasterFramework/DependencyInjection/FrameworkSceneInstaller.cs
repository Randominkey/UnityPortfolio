using UnityEngine;
using Zenject;
using MasterFramework.Core;
using MasterFramework.API;
using MasterFramework.Services;

namespace MasterFramework.DependencyInjection
{
    public class FrameworkSceneInstaller : MonoInstaller
    {
        [Header("Configuration")]
        [SerializeField] private bool useMockApi = true;
        
        [Header("Mock Session Context")]
        [SerializeField] private string semId = "873";
        [SerializeField] private string topCorsId = "1581";
        [SerializeField] private string levelCode = "WHY10";
        [SerializeField] private string lessonId = "118742";
        [SerializeField] private string userId = "1898256";
        [SerializeField] private string accountType = "s"; // s = student, t = teacher
        [SerializeField] private int comId = 1515;

        public override void InstallBindings()
        {
            // 1. Bind Session Context (Decoupled configuration)
            var sessionContext = new LessonSessionContext
            {
                semId = semId,
                topCorsId = topCorsId,
                levelCode = levelCode,
                lessonId = lessonId,
                userId = userId,
                accountType = accountType,
                comId = comId
            };
            Container.Bind<LessonSessionContext>().FromInstance(sessionContext).AsSingle();

            // 2. Bind API Service (DIP: Mock vs Real switcher)
            if (useMockApi)
            {
                Container.Bind<IAPIService>().To<MockAPIService>().AsSingle();
                Debug.Log("[FrameworkSceneInstaller] Bound IAPIService to MockAPIService.");
            }
            else
            {
                // Note: In real setup, you would pass actual URL and auth keys (e.g. from launch deep links)
                Container.Bind<IAPIService>().To<RealAPIService>()
                    .AsSingle()
                    .WithArguments("https://cmsapistest.creverse.com/api/v1", "MOCK_API_KEY", "MOCK_AUTH_TOKEN", "launcher");
                Debug.Log("[FrameworkSceneInstaller] Bound IAPIService to RealAPIService.");
            }

            // 3. Bind Core Audio & Popup Infrastructure Services
            Container.Bind<ISoundService>().To<MockSoundService>().AsSingle();
            Container.Bind<IPopupService>().To<MockPopupService>().AsSingle();
            Container.Bind<IFeedbackStampService>().To<MockFeedbackStampService>().AsSingle();
            
            Debug.Log("[FrameworkSceneInstaller] Bindings for Sound, Popup, and FeedbackStamp completed.");
        }
    }
}
