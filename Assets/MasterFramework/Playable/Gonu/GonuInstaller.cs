using Zenject;
using MasterFramework.Core;

namespace Portfolio.Playable.Gonu
{
    public class GonuInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.Bind<IRuleValidator>()
                     .To<GonuRuleValidator>()
                     .AsSingle();

            Container.Bind<GonuAIService>()
                     .AsSingle();
        }
    }
}
