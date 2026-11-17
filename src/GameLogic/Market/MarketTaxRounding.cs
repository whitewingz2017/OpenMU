// <copyright file="MarketTaxRounding.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Market;

/// <summary>How a tax which isn't a whole amount is rounded, e.g. 10% of 5 jewels.</summary>
public enum MarketTaxRounding
{
    /// <summary>To the nearest whole amount, a half up: 0.5 jewels become 1.</summary>
    Nearest = 0,

    /// <summary>Always up: 0.1 jewels become 1.</summary>
    Up = 1,

    /// <summary>Always down: 0.9 jewels become 0.</summary>
    Down = 2,
}
