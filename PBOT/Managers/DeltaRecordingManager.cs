using IPA.Utilities;
using PBOT.Models;
using PBOT.Services;
using SiraUtil.Logging;
using SiraUtil.Submissions;
using System;
using System.Collections.Generic;
using System.Linq;
using Zenject;

namespace PBOT.Managers;

internal class DeltaRecordingManager : IInitializable, IDisposable
{
    private readonly SiraLog _siraLog;
    private readonly Submission _submission;
    private readonly IAudioTimeSource _audioTimeSource;
    private readonly IFrameContainerService _frameContainerService;
    private readonly GameplayCoreSceneSetupData _gameplayCoreSceneSetupData;
    private readonly RelativeScoreAndImmediateRankCounter _relativeScoreAndImmediateRankCounter;

    private List<DeltaFrame>? _recording;
    private readonly object? _scoreSaberReplay;

    public DeltaRecordingManager(SiraLog siraLog, Submission submission, IAudioTimeSource audioTimeSource, IFrameContainerService frameContainerService, GameplayCoreSceneSetupData gameplayCoreSceneSetupData, RelativeScoreAndImmediateRankCounter relativeScoreAndImmediateRankCounter, [InjectOptional] object? scoreSaberReplay)
    {
        _siraLog = siraLog;
        _submission = submission;
        _audioTimeSource = audioTimeSource;
        _frameContainerService = frameContainerService;
        _gameplayCoreSceneSetupData = gameplayCoreSceneSetupData;
        _relativeScoreAndImmediateRankCounter = relativeScoreAndImmediateRankCounter;
        _scoreSaberReplay = scoreSaberReplay;
    }

    public void Initialize()
    {
        var scoreSaberReplayActive = _scoreSaberReplay is not null;
        var inPracticeMode = _gameplayCoreSceneSetupData.practiceSettings != null;
        var beatLeaderReplayActive = _submission.Tickets().Any(t => t.GetProperty<string, Ticket>("Source") == "BeatLeaderReplayer");

        var shouldNotRecord = inPracticeMode || scoreSaberReplayActive || beatLeaderReplayActive;

        if (shouldNotRecord)
            return;

        _recording = new List<DeltaFrame>();
        _frameContainerService.Frames = _recording;
        _relativeScoreAndImmediateRankCounter.relativeScoreOrImmediateRankDidChangeEvent += RelativeScoreAndImmediateRankCounter_RelativeScoreOrImmediateRankDidChangeEvent;
    }

    public void Dispose()
    {
        if (_recording is null)
            return;

        _relativeScoreAndImmediateRankCounter.relativeScoreOrImmediateRankDidChangeEvent -= RelativeScoreAndImmediateRankCounter_RelativeScoreOrImmediateRankDidChangeEvent;
    }

    private void RelativeScoreAndImmediateRankCounter_RelativeScoreOrImmediateRankDidChangeEvent()
    {
        _recording?.Add(new DeltaFrame
        {
            Time = _audioTimeSource.songTime,
            Current = _relativeScoreAndImmediateRankCounter.relativeScore
        });
    }
}