using Zenject;
using MasterFramework.Core;
using MasterFramework.Services;

namespace MasterFramework.Playable.Month34High
{
    public class Month34HighInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.Bind<IRuleValidator>()
                     .To<NumberIslandRuleValidator>()
                     .AsSingle();

            // Standalone scene / Sandbox fallback bindings
            Container.Bind<ISoundService>().To<MockSoundService>().AsSingle().IfNotBound();
            Container.Bind<IPopupService>().To<MockPopupService>().AsSingle().IfNotBound();
            Container.Bind<IFeedbackStampService>().To<MockFeedbackStampService>().AsSingle().IfNotBound();
        }
    }
}
