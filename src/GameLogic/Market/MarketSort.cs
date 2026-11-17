// <copyright file="MarketSort.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Market;

/// <summary>The orders of the Marketplace page.</summary>
public enum MarketSort
{
    /// <summary>The newest listings first.</summary>
    Recommended = 0,

    /// <summary>The lowest price first, per currency.</summary>
    PriceAscending = 1,

    /// <summary>The highest price first, per currency.</summary>
    PriceDescending = 2,
}
