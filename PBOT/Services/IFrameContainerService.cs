using PBOT.Models;
using System.Collections.Generic;

namespace PBOT.Services;

internal interface IFrameContainerService
{
    public List<DeltaFrame>? Frames { get; set; }
}