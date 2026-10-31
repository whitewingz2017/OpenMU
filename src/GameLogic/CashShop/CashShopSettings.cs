// <copyright file="CashShopSettings.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.CashShop;

/// <summary>
/// The settings of the cash shop, which are configured at the <see cref="CashShopFeaturePlugIn"/>.
/// </summary>
/// <remarks>
/// The catalog isn't part of it, but of the <see cref="GameConfiguration.CashShopConfiguration"/>,
/// because it refers to item definitions.
/// </remarks>
public class CashShopSettings
{
    /// <summary>
    /// Gets or sets the alternate entrance tickets sold by the cash shop.
    /// </summary>
    public IList<CashShopEventPass> EventPasses { get; set; } = new List<CashShopEventPass>
    {
        new() { Event = MiniGameType.DevilSquare, ItemNumber = 46 },
        new() { Event = MiniGameType.BloodCastle, ItemNumber = 47 },
        new() { Event = MiniGameType.IllusionTemple, ItemNumber = 61 },
        new() { Event = MiniGameType.ImperialGuardian, ItemNumber = 127, Levels = new List<int> { 7 } },
    };

    /// <summary>
    /// Gets or sets a value indicating whether players can send packages as gift.
    /// </summary>
    public bool IsGiftingEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether players can send gifts to the characters of their own account.
    /// </summary>
    public bool CanGiftToOwnAccount { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of items in the cash shop storage of an account, including the gifts.
    /// 0 means unlimited.
    /// </summary>
    public int MaximumStorageItems { get; set; }

    /// <summary>
    /// Gets or sets explicit replacement pairs by magic-effect number. Listed effects use these
    /// pairs instead of their shared subtype; other effects keep the configured subtype rule.
    /// </summary>
    public Dictionary<short, short[]> EffectReplacements { get; set; } = new()
    {
        [29] = [101, 102], // Ascension replaces the master seals.
        [30] = [101, 102], // Wealth coexists with Ascension and Healing.
        [101] = [29, 30],
        [102] = [29, 30],
        [113] = [114], // Maximum AG and SD auras replace one another.
        [114] = [113],
    };
}
