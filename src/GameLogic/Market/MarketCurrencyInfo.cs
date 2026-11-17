// <copyright file="MarketCurrencyInfo.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Market;

/// <summary>An allowed currency with its tax and the player's balance.</summary>
public sealed class MarketCurrencyInfo
{
    /// <summary>Gets the price type.</summary>
    public int Type { get; init; }

    /// <summary>Gets the display name.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Gets the tax in percent.</summary>
    public int Tax { get; init; }

    /// <summary>Gets the player's balance: zen, WCoinC, or the single jewels of the inventory.</summary>
    public long Balance { get; init; }
}
