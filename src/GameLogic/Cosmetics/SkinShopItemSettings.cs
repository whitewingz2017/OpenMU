// <copyright file="SkinShopItemSettings.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Cosmetics;

/// <summary>Sale availability, prices and an optional visual effect for one skin.</summary>
public sealed class SkinShopItemSettings
{
    /// <summary>Gets or sets a value indicating whether the skin can be sold.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether every copy displays the excellent effect without changing its options.</summary>
    public bool ExcellentEffect { get; set; }

    /// <summary>Gets or sets the aura on every copy of this full set.</summary>
    [EnumDataType(typeof(SkinAuraColor))]
    public SkinAuraColor AuraColor { get; set; } = SkinAuraColor.Default;

    /// <summary>Gets or sets the WCoinC price. Zero makes the skin preview only.</summary>
    [Range(0, int.MaxValue)]
    public int Price { get; set; } = 100;

    /// <summary>Gets or sets the Goblin Points price. Zero means the skin can only be bought with WCoinC.</summary>
    [Range(0, int.MaxValue)]
    public int GoblinPrice { get; set; }
}
