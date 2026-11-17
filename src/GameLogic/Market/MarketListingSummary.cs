// <copyright file="MarketListingSummary.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Market;

using System.Text.Json.Serialization;

/// <summary>A listing as the client lists it: without the item object, with the item's client bytes.</summary>
public sealed class MarketListingSummary
{
    /// <summary>Gets the listing id.</summary>
    public Guid Id { get; init; }

    /// <summary>Gets the seller's account id; not sent.</summary>
    [JsonIgnore]
    public Guid SellerAccountId { get; init; }

    /// <summary>Gets the seller's character name.</summary>
    public string Seller { get; init; } = string.Empty;

    /// <summary>Gets the buyer's character name, once sold.</summary>
    public string Buyer { get; init; } = string.Empty;

    /// <summary>Gets the item's bytes as the client's inventory list carries them, hex.</summary>
    public string Item { get; init; } = string.Empty;

    /// <summary>Gets the item name, for the search.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Gets the category.</summary>
    public MarketCategory Category { get; init; }

    /// <summary>Gets the price type, see <see cref="MarketCurrency"/>.</summary>
    public int PriceType { get; init; }

    /// <summary>Gets the price.</summary>
    public int Price { get; init; }

    /// <summary>Gets the seller's proceeds after the tax, once sold.</summary>
    public int Proceeds { get; init; }

    /// <summary>Gets the state.</summary>
    public MarketListingState State { get; init; }

    /// <summary>Gets when the item was listed, as Unix seconds.</summary>
    public long Listed { get; init; }

    /// <summary>Gets when the listing expires, as Unix seconds.</summary>
    public long Expires { get; init; }

    /// <summary>Gets when the listing ended, as Unix seconds; 0 while active.</summary>
    public long Closed { get; init; }

    /// <summary>Creates the summary of a stored listing.</summary>
    /// <param name="listing">The listing.</param>
    /// <param name="settings">The settings, for the category.</param>
    /// <param name="muun">The Muun configuration, for the category; <c>null</c> without Muuns.</param>
    /// <returns>The summary.</returns>
    public static MarketListingSummary From(MarketListing listing, MarketSettings settings, Muun.MuunConfiguration? muun) => new()
    {
        Id = listing.Id,
        SellerAccountId = listing.SellerAccountId,
        Seller = listing.SellerName,
        Buyer = listing.BuyerName,
        Item = listing.ItemData,
        Name = listing.ItemName,
        Category = MarketItems.CategoryOf(listing.Item, settings, muun),
        PriceType = listing.PriceType,
        Price = listing.Price,
        Proceeds = listing.Proceeds,
        State = (MarketListingState)listing.State,
        Listed = UnixSeconds(listing.ListedAt),
        Expires = UnixSeconds(listing.ExpiresAt),
        Closed = listing.ClosedAt is { } closed ? UnixSeconds(closed) : 0,
    };

    private static long UnixSeconds(DateTime time)
        => new DateTimeOffset(DateTime.SpecifyKind(time, DateTimeKind.Utc)).ToUnixTimeSeconds();
}
