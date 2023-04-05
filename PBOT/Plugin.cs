using IPA;
using PBOT.Installers;
using SiraUtil.Zenject;
using IPALogger = IPA.Logging.Logger;

namespace PBOT
{
    [NoEnableDisable, Plugin(RuntimeOptions.DynamicInit)]
    public class Plugin
    {
        [Init]
        public Plugin(IPALogger logger, Zenjector zenjector)
        {
            zenjector.UseLogger(logger);
            zenjector.Install<PBOTCoreInstaller>(Location.App);
        }
    }
}