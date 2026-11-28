// <copyright file="DuelBetCurrency.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.DuelBetting;

/// <summary>The currency identifiers of duel betting; never exchange currencies between pools.</summary>
/// <remarks>Identifier 3 is retired and must not be reused.</remarks>
public enum DuelBetCurrency
{
    /// <summary>Jewels of Bless.</summary>
    Bless = 0,

    /// <summary>Jewels of Soul.</summary>
    Soul = 1,

    /// <summary>Jewels of Life.</summary>
    Life = 2,

    /// <summary>WCoinC from the account's Cash Shop balance.</summary>
    WCoinC = 4,
}
