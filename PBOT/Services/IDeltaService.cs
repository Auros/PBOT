using PBOT.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PBOT.Services;

internal interface IDeltaService
{
    Task<DeltaMetadata?> GetMetadataAsync(ScoreContract contract, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DeltaFrame>> GetFramesAsync(ScoreContract contract, CancellationToken cancellationToken = default);
    Task SaveAsync(ScoreContract score, DeltaMetadata metadata, List<DeltaFrame> frames, CancellationToken cancellationToken = default);
}