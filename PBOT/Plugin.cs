using IPA;
using IPA.Config.Stores;
using IPA.Loader;
using PBOT.Installers;
using PBOT.Managers;
using SiraUtil.Attributes;
using SiraUtil.Zenject;
using Conf = IPA.Config.Config;
using IPALogger = IPA.Logging.Logger;

namespace PBOT
{
    [Slog, NoEnableDisable, Plugin(RuntimeOptions.DynamicInit)]
    public class Plugin
    {
        [Init]
        public Plugin(Conf conf, IPALogger logger, Zenjector zenjector)
        {
            var config = conf.Generated<Config>();
            zenjector.UseHttpService();
            zenjector.UseLogger(logger);
            zenjector.Install<PBOTCoreInstaller>(Location.App);
            zenjector.Install<PBOTGameplayInstaller>(Location.Player);
            zenjector.Install(Location.App, Container => Container.BindInstance(config).AsSingle());
            zenjector.Install(Location.StandardPlayer | Location.CampaignPlayer, Container =>
            {
                // Install the visual panel if counters+ is not installed... only in singleplayer and campaign
                if (PluginManager.GetPlugin("Counters+") != null)
                    return;
                
                Container.BindInterfacesTo<DeltaRankUIPanelVisualManager>().AsSingle();
            });
        }
    }
}