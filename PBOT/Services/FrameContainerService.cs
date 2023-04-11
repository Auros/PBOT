using PBOT.Models;
using System.Collections.Generic;

namespace PBOT.Services;

internal class FrameContainerService : IFrameContainerService
{
    public List<DeltaFrame>? Frames { get; set; }
}
