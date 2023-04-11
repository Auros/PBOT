using PBOT.Managers;
using PBOT.Services;
using Zenject;

namespace PBOT.Installers
{
    internal class PBOTCoreInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.Bind<FileSystemDeltaService>().AsSingle();
            Container.Bind<BeatLeaderScoreGraphDeltaService>().AsSingle();
            Container.Bind<IMultiplexedDeltaService>().To<CachableTimeBasedMultiplexedDeltaService>().AsSingle();
            Container.Bind<IFrameContainerService>().To<FrameContainerService>().AsSingle();
            Container.BindInterfacesTo<DeltaFrameSavingManager>().AsSingle();
        }
    }
}