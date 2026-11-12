// <copyright file="SkinShopItemSettings.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Cosmetics;

/// <summary>Sale availability and WCoinC price for one skin.</summary>
public sealed class SkinShopItemSettings
{
    /// <summary>Gets or sets a value indicating whether the skin can be sold.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Gets or sets the WCoinC price. Zero makes the skin preview only.</summary>
    [Range(0, int.MaxValue)]
    public int Price { get; set; } = 100;
}
