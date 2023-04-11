using PBOT.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PBOT.Services;

internal interface IMultiplexedDeltaService
{
    Task<IReadOnlyList<DeltaFrame>> GetFramesAsync(ScoreContract contract, CancellationToken cancellationToken = default);
}