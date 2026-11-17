// <copyright file="MarketOperation.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Market;

/// <summary>The operations of a Marketplace request.</summary>
public enum MarketOperation : byte
{
    /// <summary>Loads a page of listings, the player's own listings and the history.</summary>
    Load = 0,

    /// <summary>Lists an inventory item for sale.</summary>
    List = 1,

    /// <summary>Buys a listing.</summary>
    Buy = 2,

    /// <summary>Cancels one of the player's listings.</summary>
    Cancel = 3,
}
