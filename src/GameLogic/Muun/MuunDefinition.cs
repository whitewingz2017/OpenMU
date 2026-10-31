// <copyright file="MuunDefinition.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Muun;

/// <summary>Administrator-configured rules for one Muun species.</summary>
public sealed class MuunDefinition
{
    /// <summary>Gets or sets the pet item group times 512 plus its number.</summary>
    public int ItemType { get; set; }
    /// <summary>Gets or sets its maximum zero-based item level.</summary>
    [Range(0, 15)] public byte MaximumLevel { get; set; } = 4;
    /// <summary>Gets or sets the evolved item id; -1 disables evolution.</summary>
    public int EvolutionItemType { get; set; } = -1;
    /// <summary>Gets or sets the required evolution stone item id.</summary>
    public int EvolutionStoneType { get; set; } = (14 * 512) + 250;
    /// <summary>Gets or sets the life-restoring consumable item id.</summary>
    public int RepairItemType { get; set; } = (14 * 512) + 16;
    /// <summary>Gets or sets the minimum character level for its bonus.</summary>
    public int MinimumCharacterLevel { get; set; }
    /// <summary>Gets or sets the maps enabling its bonus; empty means all maps.</summary>
    public List<short> Maps { get; set; } = [];
    /// <summary>Gets or sets the bonus multiplier in addition to the item definition's level-dependent bonuses.</summary>
    [Range(1, 10)] public float SpecialMultiplier { get; set; } = 1;
    /// <summary>Gets or sets the UTC start of its special bonus period.</summary>
    public DateTime? SpecialFromUtc { get; set; }
    /// <summary>Gets or sets the UTC end of its special bonus period.</summary>
    public DateTime? SpecialUntilUtc { get; set; }
}
