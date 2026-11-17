// <copyright file="MarketPurchase.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Market;

using MUnique.OpenMU.Persistence;

/// <summary>
/// Buys a listing in one transaction: takes the price, mails the item to the buyer and the price
/// after the tax to the seller, and ends the listing.
/// </summary>
internal static class MarketPurchase
{
    /// <summary>Buys the listing.</summary>
    /// <param name="buyer">The buyer. The caller holds its persistence lock and the market's write lock.</param>
    /// <param name="settings">The settings, for the tax.</param>
    /// <param name="listingId">The listing.</param>
    /// <returns><see cref="MarketResult.Bought"/>, or why it wasn't bought.</returns>
    public static async ValueTask<MarketResult> BuyAsync(Player buyer, MarketSettings settings, Guid listingId)
    {
        using var transaction = buyer.GameContext.PersistenceContextProvider.CreateNewPlayerContext(buyer.GameContext.Configuration);
        var listing = await transaction.GetByIdAsync<MarketListing>(listingId).ConfigureAwait(false);
        if (listing is not { State: (int)MarketListingState.Active, Item: { } item } || listing.ExpiresAt <= DateTime.UtcNow)
        {
            return MarketResult.NotAvailable;
        }

        if (listing.SellerAccountId == buyer.Account!.GetId())
        {
            return MarketResult.OwnListing;
        }

        if (MarketPayment.Create(buyer, listing.PriceType, listing.Price) is not { } payment)
        {
            return MarketResult.NotEnoughCurrency;
        }

        transaction.Attach(buyer.Account);
        transaction.Attach(buyer.SelectedCharacter!);
        var letters = await SaveAsync(transaction, buyer, settings, listing, item, payment).ConfigureAwait(false);
        if (letters is not { } saved)
        {
            return MarketResult.TryAgain;
        }

        payment.DetachPaidJewels();
        await MarketMail.ForwardAsync(buyer.GameContext, buyer.Logger, saved.Buyer).ConfigureAwait(false);
        await MarketMail.ForwardAsync(buyer.GameContext, buyer.Logger, saved.Seller).ConfigureAwait(false);
        return MarketResult.Bought;
    }

    private static async ValueTask<(LetterBody Buyer, LetterBody Seller)?> SaveAsync(IPlayerContext transaction, Player buyer, MarketSettings settings, MarketListing listing, Item item, MarketPayment payment)
    {
        var committed = false;
        try
        {
            var tax = MarketCurrency.Tax(settings, listing.PriceType, listing.Price);
            var proceeds = listing.Price - tax;
            var buyerLetter = await CreateBuyerLetterAsync(transaction, buyer, listing, item).ConfigureAwait(false);
            var sellerLetter = await CreateSellerLetterAsync(transaction, buyer, settings, listing, tax, proceeds).ConfigureAwait(false);
            await payment.TakeAsync(transaction).ConfigureAwait(false);
            listing.Item = null;
            listing.State = (int)MarketListingState.Sold;
            listing.BuyerName = buyer.Name;
            listing.ClosedAt = DateTime.UtcNow;
            listing.Proceeds = proceeds;
            listing.Revision = Guid.NewGuid();
            committed = await transaction.SaveChangesAsync().ConfigureAwait(false);
            return committed ? (buyerLetter, sellerLetter) : null;
        }
        catch (Exception ex)
        {
            buyer.Logger.LogWarning(ex, "Could not buy the Marketplace listing {Listing} for {Player}", listing.Id, buyer);
            return null;
        }
        finally
        {
            if (!committed)
            {
                await payment.RefundAsync().ConfigureAwait(false);
            }
        }
    }

    private static ValueTask<LetterBody> CreateBuyerLetterAsync(IPlayerContext transaction, Player buyer, MarketListing listing, Item item)
    {
        var currency = MarketCurrency.Name(buyer.GameContext.Configuration, listing.PriceType);
        var message = MarketMail.Text(buyer.Culture, "Market_BoughtBody", listing.ItemName, listing.SellerName, listing.Price, currency);
        return MarketMail.CreateAsync(transaction, buyer.Name, MarketMail.Text(buyer.Culture, "Market_BoughtSubject"), message, item, 0, 0);
    }

    private static ValueTask<LetterBody> CreateSellerLetterAsync(IPlayerContext transaction, Player buyer, MarketSettings settings, MarketListing listing, int tax, int proceeds)
    {
        var culture = System.Globalization.CultureInfo.InvariantCulture;
        var currency = MarketCurrency.Name(buyer.GameContext.Configuration, listing.PriceType);
        var percent = MarketCurrency.TaxPercent(settings, listing.PriceType);
        var message = MarketMail.Text(culture, "Market_SoldBody", buyer.Name, listing.ItemName, listing.Price, currency, percent, tax, proceeds);
        return MarketMail.CreateAsync(transaction, listing.SellerName, MarketMail.Text(culture, "Market_SoldSubject"), message, null, listing.PriceType, proceeds);
    }
}
