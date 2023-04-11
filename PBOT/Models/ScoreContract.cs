namespace PBOT.Models;

internal record struct ScoreContract(string LevelId, string Mode, BeatmapDifficulty Difficulty)
{
    public override string ToString() => $"{LevelId}_{Mode}_{Difficulty.SerializedName()}";
}