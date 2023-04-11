using PBOT.Models;

namespace PBOT.Services;

internal interface IFrameVisualizerService
{
    void SetFrame(DeltaFrame frame);
}