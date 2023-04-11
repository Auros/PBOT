using IPA.Loader;
using PBOT.Managers;
using PBOT.Models;
using Zenject;

namespace PBOT.Installers;

internal class PBOTGameplayInstaller : Installer
{
    public override void InstallBindings()
    {
        if (PluginManager.GetPlugin("Counters+") == null)
            Container.BindInterfacesTo<DeltaRankUIPanelVisualManager>().AsSingle();

        Container.BindInterfacesTo<DeltaRecordingManager>().AsSingle();
        Container.BindInterfacesTo<DeltaPlaybackManager>().AsSingle();

        Container.Bind<ScoreContract>().FromMethod(Context =>
        {
            var beatmap = Context.Container.Resolve<IDifficultyBeatmap>();
            var mode = beatmap.parentDifficultyBeatmapSet.beatmapCharacteristic.serializedName;
            var level = beatmap.level.levelID.Replace("custom_level_", string.Empty);
            var diff = beatmap.difficulty;
            return new ScoreContract(level, mode, diff);
        }).AsSingle();
    }
}