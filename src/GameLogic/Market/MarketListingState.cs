// <copyright file="MarketListingState.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Market;

/// <summary>The states of a <see cref="MarketListing"/>, stored as its <see cref="MarketListing.State"/>.</summary>
public enum MarketListingState
{
    /// <summary>On the market.</summary>
    Active = 0,

    /// <summary>Sold; the item went to the buyer's mail, the proceeds to the seller's.</summary>
    Sold = 1,

    /// <summary>Cancelled by the seller; the item went back by mail.</summary>
    Cancelled = 2,

    /// <summary>Expired unsold; the item went back by mail.</summary>
    Expired = 3,
}
