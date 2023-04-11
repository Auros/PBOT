using PBOT.Models;
using PBOT.Services;
using SiraUtil.Zenject;
using System.Threading.Tasks;
using System.Threading;
using Zenject;
using System.Collections.Generic;
using System;

namespace PBOT.Managers;

internal class DeltaPlaybackManager : IDeltaPlaybackService, IAsyncInitializable, ITickable
{
    private readonly ScoreContract _scoreContract;
    private readonly IAudioTimeSource _audioTimeSource;
    private readonly IMultiplexedDeltaService _multiplexedDeltaService;

    private int _nextFrame;
    private IReadOnlyList<DeltaFrame>? _frames;

    public event Action<DeltaFrame>? OnFrameUpdated;

    public DeltaPlaybackManager(ScoreContract scoreContract, IAudioTimeSource audioTimeSource, IMultiplexedDeltaService multiplexedDeltaService)
    {
        _scoreContract = scoreContract;
        _audioTimeSource = audioTimeSource;
        _multiplexedDeltaService = multiplexedDeltaService;
    }


    public async Task InitializeAsync(CancellationToken token)
    {
        var frames = await _multiplexedDeltaService.GetFramesAsync(_scoreContract, token);

        if (frames.Count is 0)
            return;

        _frames = frames;
    }

    public void Tick()
    {
        // Don't update if we don't have any frames or we've processed all of them.
        if (_frames is null || _nextFrame >= _frames.Count)
            return;

        var now = _audioTimeSource.songTime;
        DeltaFrame frame = _frames[_nextFrame];
        
        // Check if the frame we're currently examining has reached its time.
        // We don't need to continue if it has not.
        if (frame.Time > now)
            return;

        // Find the youngest frame relative to now.
        while (_frames.Count > _nextFrame && now > frame.Time)
        {
            frame = _frames[_nextFrame];
            if (frame.Time > now)
                break;

            _nextFrame++;
        }

        // Send frame update
        OnFrameUpdated?.Invoke(frame);
    }
}