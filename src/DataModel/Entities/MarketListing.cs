// <copyright file="MarketListing.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.DataModel.Entities;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// An item a character offers on the Marketplace, and after it ended, the record of its sale, cancel or expiry.
/// </summary>
/// <remarks>
/// It's not part of the <see cref="Account"/> aggregate: other accounts buy the item while the seller may be
/// offline or on another game server, so it refers to its seller by identifier and name. While the listing is
/// active it holds the item; afterwards the item is in the mail of the buyer or the seller.
/// </remarks>
[AggregateRoot]
public class MarketListing
{
    /// <summary>Gets or sets the identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the identifier of the seller's account.</summary>
    public Guid SellerAccountId { get; set; }

    /// <summary>Gets or sets the name of the selling character, who gets the mail.</summary>
    [MaxLength(10)]
    public string SellerName { get; set; } = string.Empty;

    /// <summary>Gets or sets the offered item, while the listing is active.</summary>
    [MemberOfAggregate]
    public virtual Item? Item { get; set; }

    /// <summary>Gets or sets the item as the client shows it (its inventory data, hexadecimal), kept for the history.</summary>
    public string ItemData { get; set; } = string.Empty;

    /// <summary>Gets or sets the item's name, kept for the history and the search.</summary>
    public string ItemName { get; set; } = string.Empty;

    /// <summary>Gets or sets the currency: zero for zen, a cash shop coin code, or an item type (group &lt;&lt; 12 | number).</summary>
    public int PriceType { get; set; }

    /// <summary>Gets or sets the price in the currency.</summary>
    public int Price { get; set; }

    /// <summary>Gets or sets when the item was listed.</summary>
    public DateTime ListedAt { get; set; }

    /// <summary>Gets or sets when the listing ends and the item goes back to the seller.</summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>Gets or sets the state: 0 active, 1 sold, 2 cancelled, 3 expired.</summary>
    public int State { get; set; }

    /// <summary>Gets or sets the name of the buying character.</summary>
    [MaxLength(10)]
    public string BuyerName { get; set; } = string.Empty;

    /// <summary>Gets or sets when the listing ended.</summary>
    public DateTime? ClosedAt { get; set; }

    /// <summary>Gets or sets what the seller got after the tax.</summary>
    public int Proceeds { get; set; }

    /// <summary>Gets or sets the optimistic token which lets only one buy, cancel or expiry end the listing.</summary>
    [ConcurrencyCheck]
    public Guid Revision { get; set; }
}
