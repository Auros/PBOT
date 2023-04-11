using PBOT.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PBOT.Services;

internal class CachableTimeBasedMultiplexedDeltaService : IMultiplexedDeltaService
{
    private readonly FileSystemDeltaService _fileSystemDeltaService;
    private readonly BeatLeaderScoreGraphDeltaService _beatLeaderScoreGraphDeltaService;

    public CachableTimeBasedMultiplexedDeltaService(FileSystemDeltaService fileSystemDeltaService, BeatLeaderScoreGraphDeltaService beatLeaderScoreGraphDeltaService)
    {
        _fileSystemDeltaService = fileSystemDeltaService;
        _beatLeaderScoreGraphDeltaService = beatLeaderScoreGraphDeltaService;
    }

    public async Task<IReadOnlyList<DeltaFrame>> GetFramesAsync(ScoreContract contract, CancellationToken cancellationToken = default)
    {
        var localMetadata = await _fileSystemDeltaService.GetMetadataAsync(contract, cancellationToken);
        var beatLeaderMetadata = await _beatLeaderScoreGraphDeltaService.GetMetadataAsync(contract, cancellationToken);
        
        if (beatLeaderMetadata is null && localMetadata is null)
            return Array.Empty<DeltaFrame>();
        
        if (beatLeaderMetadata is null && localMetadata is not null)
            return await _fileSystemDeltaService.GetFramesAsync(contract, cancellationToken);

        // Fetch the frames from BeatLeader if we don't have anything stored locally or the BeatLeader data is newer than the local data.
        if (beatLeaderMetadata is not null && localMetadata is null || (beatLeaderMetadata is not null && localMetadata is not null && beatLeaderMetadata.Timestamp > localMetadata.Timestamp))
        {
            var beatLeaderFrames = await _beatLeaderScoreGraphDeltaService.GetFramesAsync(contract, cancellationToken);
            if (beatLeaderFrames.Count > 0)
                await _fileSystemDeltaService.SaveAsync(contract, beatLeaderMetadata, beatLeaderFrames.ToList(), cancellationToken);
            return beatLeaderFrames;
        }

        // This should not happen.
        if (beatLeaderMetadata is null || localMetadata is null)
            return Array.Empty<DeltaFrame>();

        return await _fileSystemDeltaService.GetFramesAsync(contract, cancellationToken);
    }
}