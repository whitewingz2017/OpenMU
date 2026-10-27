// <copyright file="StoreCurrencySettings.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions.PlayerStore;

/// <summary>Allowed currencies for extended personal stores.</summary>
public class StoreCurrencySettings
{
    /// <summary>Gets or sets inventory jewel identifiers (group * 512 + number). Only plain level-zero singles are accepted.</summary>
    public int[] Jewels { get; set; } = [7181, 7182, 6159, 7184, 7190, 7199, 7210];

    /// <summary>Gets or sets a value indicating whether cash-shop coin prices are allowed.</summary>
    public bool AllowCashShopCoins { get; set; } = true;
}
