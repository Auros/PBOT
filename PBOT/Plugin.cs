using IPA;
using PBOT.Installers;
using PBOT.Managers;
using SiraUtil.Attributes;
using SiraUtil.Zenject;
using IPALogger = IPA.Logging.Logger;

namespace PBOT
{
    [Slog, NoEnableDisable, Plugin(RuntimeOptions.DynamicInit)]
    public class Plugin
    {
        [Init]
        public Plugin(IPALogger logger, Zenjector zenjector)
        {
            zenjector.UseHttpService();
            zenjector.UseLogger(logger);
            zenjector.Install<PBOTCoreInstaller>(Location.App);
            zenjector.Install(Location.Singleplayer, Container => Container.BindInterfacesAndSelfTo<DeltaPlaygroundManager>().AsSingle());
        }
    }
}