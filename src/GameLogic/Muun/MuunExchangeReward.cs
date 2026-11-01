namespace MUnique.OpenMU.GameLogic.Muun;
/// <summary>A weighted Muun exchange reward.</summary>
public sealed class MuunExchangeReward
{
    /// <summary>Gets or sets the registered species item type.</summary>
    public int ItemType { get; set; } = 13 * 512 + 450;
    /// <summary>Gets or sets its relative selection weight.</summary>
    [Range(1, 1000000)] public int Weight { get; set; } = 1;
}
