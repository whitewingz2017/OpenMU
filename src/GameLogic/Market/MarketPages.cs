// <copyright file="MarketPages.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Market;

/// <summary>Builds the <see cref="MarketAnswer"/> of a player from the listings.</summary>
public static class MarketPages
{
    /// <summary>The listings of a page.</summary>
    public const int PageSize = 12;

    /// <summary>The most entries of the history.</summary>
    private const int HistorySize = 30;

    /// <summary>Creates the answer.</summary>
    /// <param name="player">The player.</param>
    /// <param name="settings">The settings.</param>
    /// <param name="listings">The active listings and the recent history.</param>
    /// <param name="request">The filter, or <c>null</c> for the first page of all.</param>
    /// <returns>The answer, without the request identifier and the result.</returns>
    public static MarketAnswer Create(Player player, MarketSettings settings, IReadOnlyList<MarketListingSummary> listings, MarketRequestData? request)
    {
        request ??= new MarketRequestData();
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var active = listings.Where(l => l.State == MarketListingState.Active && l.Expires > now).ToList();
        var matching = Sort(active.Where(l => Matches(l, request)), (MarketSort)request.Sort).ToList();
        var lastPage = Math.Max(0, (matching.Count - 1) / PageSize);
        var page = Math.Clamp(request.Page, 0, lastPage);
        return new MarketAnswer
        {
            Currencies = Currencies(player, settings),
            ListingHours = settings.ListingHours,
            MaximumListings = settings.MaximumListings,
            Page = page,
            Total = matching.Count,
            Listings = matching.Skip(page * PageSize).Take(PageSize).ToList(),
            Mine = active.Where(l => l.Seller == player.Name).OrderByDescending(l => l.Listed).ToList(),
            History = listings
                .Where(l => l.State != MarketListingState.Active && (l.Seller == player.Name || l.Buyer == player.Name))
                .OrderByDescending(l => l.Closed)
                .Take(HistorySize)
                .ToList(),
        };
    }

    private static IReadOnlyList<MarketCurrencyInfo> Currencies(Player player, MarketSettings settings)
        => MarketCurrency.Allowed(settings).Select(type => new MarketCurrencyInfo
        {
            Type = type,
            Name = MarketCurrency.Name(player.GameContext.Configuration, type),
            Tax = MarketCurrency.TaxPercent(settings, type),
            Balance = MarketCurrency.Balance(player, type),
        }).ToList();

    private static bool Matches(MarketListingSummary listing, MarketRequestData request)
        => (request.Category == (int)MarketCategory.All || (int)listing.Category == request.Category)
           && (request.Currency < 0 || listing.PriceType == request.Currency)
           && (string.IsNullOrWhiteSpace(request.Search) || listing.Name.Contains(request.Search.Trim(), StringComparison.OrdinalIgnoreCase));

    private static IEnumerable<MarketListingSummary> Sort(IEnumerable<MarketListingSummary> listings, MarketSort sort) => sort switch
    {
        MarketSort.PriceAscending => listings.OrderBy(l => l.PriceType).ThenBy(l => l.Price),
        MarketSort.PriceDescending => listings.OrderBy(l => l.PriceType).ThenByDescending(l => l.Price),
        _ => listings.OrderByDescending(l => l.Listed),
    };
}
