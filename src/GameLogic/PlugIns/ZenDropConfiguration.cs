namespace MUnique.OpenMU.GameLogic.PlugIns;
/// <summary>Monster Zen amount settings. A multiplier of one preserves current amounts.</summary>
public sealed class ZenDropConfiguration
{
    [Range(0, 10000)] public double AmountMultiplier { get; set; } = 1;
    [Range(0, int.MaxValue)] public int MinimumAmount { get; set; }
    [Range(0, int.MaxValue)] public int MaximumAmount { get; set; } = int.MaxValue;
}
