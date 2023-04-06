using PBOT.Services;
using Zenject;

namespace PBOT.Installers
{
    internal class PBOTCoreInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.Bind<IDeltaService>().To<BeatLeaderDeltaService>().AsSingle();
        }
    }
}