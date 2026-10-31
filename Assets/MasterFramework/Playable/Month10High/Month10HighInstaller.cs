using Zenject;
using MasterFramework.Core;
using MasterFramework.Services;

namespace MasterFramework.Playable.Month10High
{
    public class Month10HighInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.Bind<IRuleValidator>()
                     .To<RectSquareRuleValidator>()
                     .AsSingle();

            // Standalone scene fallback bindings
            Container.Bind<ISoundService>().To<MockSoundService>().AsSingle().IfNotBound();
            Container.Bind<IPopupService>().To<MockPopupService>().AsSingle().IfNotBound();
            Container.Bind<IFeedbackStampService>().To<MockFeedbackStampService>().AsSingle().IfNotBound();
        }
    }
}
