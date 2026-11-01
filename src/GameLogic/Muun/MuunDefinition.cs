// <copyright file="MuunDefinition.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Muun;

/// <summary>Administrator-configured rules for one Muun species.</summary>
public sealed class MuunDefinition
{
    /// <summary>Gets or sets the fraction of owner damage used for assisted monster attacks; zero disables combat.</summary>
    [Range(0, 1)] public double AttackDamageFactor { get; set; } = 0.1;
    /// <summary>Gets or sets the minimum time between assisted attacks.</summary>
    [Range(500, 60000)] public int AttackIntervalMilliseconds { get; set; } = 3000;
    /// <summary>Gets or sets maximum attack distance in tiles.</summary>
    [Range(1, 10)] public int AttackRange { get; set; } = 4;
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
    /// <summary>Gets or sets the maximum character level for its bonus; zero means no limit.</summary>
    public int MaximumCharacterLevel { get; set; }
    /// <summary>Gets or sets the UTC week days enabling its bonus; empty means every day.</summary>
    public List<DayOfWeek> Days { get; set; } = [];
    /// <summary>Gets or sets the first UTC hour of its daily bonus window.</summary>
    [Range(0, 23)] public int FromHourUtc { get; set; }
    /// <summary>Gets or sets the UTC hour ending its daily bonus window; a value at or below <see cref="FromHourUtc"/> wraps past midnight.</summary>
    [Range(1, 24)] public int UntilHourUtc { get; set; } = 24;
    /// <summary>Gets or sets the maps enabling its bonus; empty means all maps.</summary>
    public List<short> Maps { get; set; } = [];
    /// <summary>Gets or sets the bonus multiplier in addition to the item definition's level-dependent bonuses.</summary>
    [Range(1, 10)] public float SpecialMultiplier { get; set; } = 1;
    /// <summary>Gets or sets the value added to each non-multiplicative bonus during the special period.</summary>
    [Range(0, 1000)] public float SpecialAddition { get; set; }
    /// <summary>Gets or sets the fraction removed from <see cref="AttackIntervalMilliseconds"/> during the special period.</summary>
    [Range(0, 0.9)] public double SpecialAttackIntervalReduction { get; set; }
    /// <summary>Gets or sets the UTC start of its special bonus period.</summary>
    public DateTime? SpecialFromUtc { get; set; }
    /// <summary>Gets or sets the UTC end of its special bonus period.</summary>
    public DateTime? SpecialUntilUtc { get; set; }
}
