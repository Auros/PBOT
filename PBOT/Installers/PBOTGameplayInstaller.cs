using IPA.Loader;
using PBOT.Managers;
using PBOT.Models;
using Zenject;
using static BeatmapLevelSaveDataVersion4.BeatmapLevelSaveData;

namespace PBOT.Installers;

internal class PBOTGameplayInstaller : Installer
{
    public override void InstallBindings()
    {
        Container.BindInterfacesTo<DeltaRecordingManager>().AsSingle();
        Container.BindInterfacesTo<DeltaPlaybackManager>().AsSingle();

        Container.Bind<ScoreContract>().FromMethod(Context =>
        {
            var beatmap = Context.Container.Resolve<BeatmapKey>();
            var mode = beatmap.characteristic.SerializedName();
            var level = beatmap.levelId.Replace("custom_level_", string.Empty);
            var diff = beatmap.difficulty;

            return new ScoreContract(level, mode, diff);
        }).AsSingle();
    }
}
