namespace MUnique.OpenMU.GameLogic.Muun;
/// <summary>A configured egg or material exchange.</summary>
public sealed class MuunExchangeRecipe
{
    /// <summary>Gets or sets the confirmation label.</summary>
    public string Name { get; set; } = "Muun Egg";
    /// <summary>Gets or sets the bag material item type.</summary>
    public int MaterialItemType { get; set; } = 14 * 512 + 251;
    /// <summary>Gets or sets the required material count.</summary>
    [Range(1, 255)] public int RequiredCount { get; set; } = 1;
    /// <summary>Gets or sets weighted rewards, placed in Muun storage.</summary>
    public List<MuunExchangeReward> Rewards { get; set; } = [new(), new() { ItemType = 13 * 512 + 452 }, new() { ItemType = 13 * 512 + 454 }];
}
