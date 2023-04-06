using PBOT.Models;
using PBOT.Services;
using SiraUtil.Logging;
using SiraUtil.Zenject;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PBOT.Managers;

internal class DeltaPlaygroundManager : IAsyncInitializable
{
    private readonly SiraLog _siraLog;
    private readonly IDeltaService _deltaService;
    private readonly IDifficultyBeatmap _difficultyBeatmap;

    public DeltaPlaygroundManager(SiraLog siraLog, IDeltaService deltaService, IDifficultyBeatmap difficultyBeatmap)
    {
        _siraLog = siraLog;
        _deltaService = deltaService;
        _difficultyBeatmap = difficultyBeatmap;
    }

    public async Task InitializeAsync(CancellationToken token)
    {
        var mode = _difficultyBeatmap.parentDifficultyBeatmapSet.beatmapCharacteristic.serializedName;
        var level = _difficultyBeatmap.level.levelID.Replace("custom_level_", string.Empty);
        var diff = _difficultyBeatmap.difficulty;
        var frames = await _deltaService.GetFramesAsync(new Models.ScoreContract(level, mode, diff), token);
        _siraLog.Info($"Frame Count: {frames.Count}");
        foreach (var frame in new DeltaFrame[] { frames[0], frames.Last() })
        {
            _siraLog.Info($"{frame.Time} - {frame.Current}");
        }
    }
}
