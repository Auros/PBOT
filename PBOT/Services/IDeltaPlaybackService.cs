using PBOT.Models;
using System;

namespace PBOT.Services;

internal interface IDeltaPlaybackService
{
    event Action<DeltaFrame>? OnFrameUpdated;
}