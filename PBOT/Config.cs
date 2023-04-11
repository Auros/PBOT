using IPA.Config.Stores;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]
namespace PBOT;

internal class Config
{
    public virtual int Precision { get; set; } = 2;
    public virtual bool ShowDifference { get; set; } = true;
    public virtual string DefaultColor { get; set; } = "#ffffff80";
    public virtual string BeatingFrameColor { get; set; } = "#6bff69ff";
}