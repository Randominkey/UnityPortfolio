using Zenject;
using UnityEngine;

namespace MasterFramework.Playable.Common
{
    /// <summary>
    /// MiniGameBoardBase 기반 모든 미니게임의 공용 Zenject SubContainer Installer
    /// </summary>
    public class UniversalMinigameInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            // MiniGame 공용 바인딩 (필요 시 확장)
        }
    }
}
