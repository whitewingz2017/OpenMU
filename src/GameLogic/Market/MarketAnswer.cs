// <copyright file="MarketAnswer.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Market;

/// <summary>The answer to a Marketplace request: the result, the currencies, the page and the player's listings.</summary>
public sealed class MarketAnswer
{
    /// <summary>Gets or sets the request identifier.</summary>
    public uint RequestId { get; set; }

    /// <summary>Gets or sets the result.</summary>
    public MarketResult Result { get; set; }

    /// <summary>Gets or sets the allowed currencies.</summary>
    public IReadOnlyList<MarketCurrencyInfo> Currencies { get; set; } = [];

    /// <summary>Gets or sets how long a listing stays on the market.</summary>
    public int ListingHours { get; set; }

    /// <summary>Gets or sets the most active listings of a character.</summary>
    public int MaximumListings { get; set; }

    /// <summary>Gets or sets the zero-based page.</summary>
    public int Page { get; set; }

    /// <summary>Gets or sets the count of all listings of the filter.</summary>
    public int Total { get; set; }

    /// <summary>Gets or sets the listings of the page.</summary>
    public IReadOnlyList<MarketListingSummary> Listings { get; set; } = [];

    /// <summary>Gets or sets the player's active listings.</summary>
    public IReadOnlyList<MarketListingSummary> Mine { get; set; } = [];

    /// <summary>Gets or sets the player's ended sales and purchases, newest first.</summary>
    public IReadOnlyList<MarketListingSummary> History { get; set; } = [];
}
