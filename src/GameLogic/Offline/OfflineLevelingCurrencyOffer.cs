// <copyright file="OfflineLevelingCurrencyOffer.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Offline;

/// <summary>An allowed currency of the offline leveling window: its prices and the player's balances.</summary>
public sealed class OfflineLevelingCurrencyOffer
{
    /// <summary>Gets the currency.</summary>
    public OfflineLevelingCurrency Currency { get; init; }

    /// <summary>Gets the price of an hour of leveling; for level-based zen, of the first hour.</summary>
    public long PerHour { get; init; }

    /// <summary>Gets the player's balance of the currency.</summary>
    public long Balance { get; init; }

    /// <summary>Gets the currency the protection is paid in.</summary>
    public OfflineLevelingCurrency ProtectionCurrency { get; init; }

    /// <summary>Gets the price of an hour of protection.</summary>
    public long ProtectionPerHour { get; init; }

    /// <summary>Gets the player's balance of the protection's currency.</summary>
    public long ProtectionBalance { get; init; }
}
