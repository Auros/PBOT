using PBOT.Models;
using PBOT.Services;
using SiraUtil.Logging;
using SiraUtil.Services;
using System;
using System.Threading.Tasks;
using Zenject;

namespace PBOT.Managers;

internal class DeltaFrameSavingManager : IInitializable, IDisposable
{
    private readonly ILevelFinisher _levelFinisher;
    private readonly IFrameContainerService _frameContainerService;
    private readonly FileSystemDeltaService _fileSystemDeltaService;

    public DeltaFrameSavingManager(ILevelFinisher levelFinisher, IFrameContainerService frameContainerService, FileSystemDeltaService fileSystemDeltaService)
    {
        _levelFinisher = levelFinisher;
        _frameContainerService = frameContainerService;
        _fileSystemDeltaService = fileSystemDeltaService;
    }

    public void Initialize()
    {
        _levelFinisher.StandardLevelDidFinish += LevelFinisher_StandardLevelDidFinish;
        _levelFinisher.MissionLevelDidFinish += LevelFinisher_MissionLevelDidFinish;
    }

    private void LevelFinisher_StandardLevelDidFinish(StandardLevelScenesTransitionSetupDataSO sceneSetup, LevelCompletionResults lcr)
    {
        Save(sceneSetup.difficultyBeatmap, lcr);
    }

    private void LevelFinisher_MissionLevelDidFinish(MissionLevelScenesTransitionSetupDataSO sceneSetup, MissionCompletionResults mlcr)
    {
        Save(sceneSetup.difficultyBeatmap, mlcr.levelCompletionResults);
    }

    private void Save(IDifficultyBeatmap beatmap, LevelCompletionResults results)
    {
        var frames = _frameContainerService.Frames;
        _frameContainerService.Frames = null; // Reset the frame reference: we don't need it anymore.

        // Only save the frame data if we have frames and the level has cleared
        if (frames is null || results.levelEndStateType is not LevelCompletionResults.LevelEndStateType.Cleared)
            return;

        // Pull necessary info to generate the contract and metadata
        var mode = beatmap.parentDifficultyBeatmapSet.beatmapCharacteristic.serializedName;
        var level = beatmap.level.levelID.Replace("custom_level_", string.Empty);
        var score = results.multipliedScore;
        var diff = beatmap.difficulty;

        Task.Run(async () =>
        {
            ScoreContract contract = new(level, mode, diff);

            // Don't save if we already have metadata for this score and it hasn't been beaten.
            var metadata = await _fileSystemDeltaService.GetMetadataAsync(contract);
            if (metadata is not null && metadata.TotalScore >= score)
                return;

            // Create the new metadata for this score
            metadata = new DeltaMetadata
            {
                Source = "Local",
                Timestamp = DateTimeOffset.UtcNow.AddMinutes(2f), // Add some arbituary time so the score on BeatLeader doesn't immediately override this version
                TotalScore = score,
                Version = new Hive.Versioning.Version(1, 0, 0)
            };

            // Save the delta frames (metadata and binary)
            await _fileSystemDeltaService.SaveAsync(contract, metadata, frames, default);
        });
    }

    public void Dispose()
    {
        _levelFinisher.MissionLevelDidFinish -= LevelFinisher_MissionLevelDidFinish;
        _levelFinisher.StandardLevelDidFinish -= LevelFinisher_StandardLevelDidFinish;
    }
}
