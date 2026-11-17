// <copyright file="MarketReturn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Market;

using System.Globalization;
using MUnique.OpenMU.Persistence;

/// <summary>Ends a listing unsold and mails its item back to the seller, in one transaction.</summary>
internal static class MarketReturn
{
    /// <summary>Cancels a listing of the player.</summary>
    /// <param name="player">The seller. The caller holds the market's write lock.</param>
    /// <param name="listingId">The listing.</param>
    /// <returns><see cref="MarketResult.Cancelled"/>, or why the listing wasn't cancelled.</returns>
    public static async ValueTask<MarketResult> CancelAsync(Player player, Guid listingId)
    {
        using var transaction = player.GameContext.PersistenceContextProvider.CreateNewPlayerContext(player.GameContext.Configuration);
        var listing = await transaction.GetByIdAsync<MarketListing>(listingId).ConfigureAwait(false);
        if (listing is null || listing.State != (int)MarketListingState.Active || listing.Item is null)
        {
            return MarketResult.NotAvailable;
        }

        if (listing.SellerName != player.Name || listing.SellerAccountId != player.Account?.GetId())
        {
            return MarketResult.NotYourListing;
        }

        var letter = await EndAsync(transaction, player.GameContext, player.Logger, listing, MarketListingState.Cancelled, player.Culture).ConfigureAwait(false);
        if (letter is null)
        {
            return MarketResult.TryAgain;
        }

        await MarketMail.ForwardAsync(player.GameContext, player.Logger, letter).ConfigureAwait(false);
        return MarketResult.Cancelled;
    }

    /// <summary>Ends the expired listings and mails their items back.</summary>
    /// <param name="context">The game context. The caller holds the market's write lock.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="settings">The settings, for how long ended listings stay in the history.</param>
    /// <returns>The count of ended or deleted listings.</returns>
    public static async ValueTask<int> ExpireAsync(IGameContext context, ILogger logger, MarketSettings settings)
    {
        var now = DateTime.UtcNow;
        IReadOnlyList<Guid> expired;
        int deleted;
        using (var lookup = context.PersistenceContextProvider.CreateNewPlayerContext(context.Configuration))
        {
            var listings = (await lookup.GetAsync<MarketListing>().ConfigureAwait(false)).ToList();
            expired = listings
                .Where(l => l.State == (int)MarketListingState.Active && l.ExpiresAt <= now)
                .Select(l => l.Id)
                .ToList();
            deleted = await DeleteOldAsync(lookup, listings, now.AddDays(-settings.HistoryDays)).ConfigureAwait(false);
        }

        var ended = 0;
        foreach (var id in expired)
        {
            using var transaction = context.PersistenceContextProvider.CreateNewPlayerContext(context.Configuration);
            if (await transaction.GetByIdAsync<MarketListing>(id).ConfigureAwait(false) is not { State: (int)MarketListingState.Active } listing
                || await EndAsync(transaction, context, logger, listing, MarketListingState.Expired, CultureInfo.InvariantCulture).ConfigureAwait(false) is not { } letter)
            {
                continue;
            }

            await MarketMail.ForwardAsync(context, logger, letter).ConfigureAwait(false);
            ended++;
        }

        return ended + deleted;
    }

    /// <summary>Deletes the ended listings which left the history; their items went out by mail before.</summary>
    private static async ValueTask<int> DeleteOldAsync(IPlayerContext context, IEnumerable<MarketListing> listings, DateTime historySince)
    {
        var old = listings
            .Where(l => l.State != (int)MarketListingState.Active && l.Item is null && l.ClosedAt < historySince)
            .ToList();
        foreach (var listing in old)
        {
            await context.DeleteAsync(listing).ConfigureAwait(false);
        }

        return old.Count > 0 && await context.SaveChangesAsync().ConfigureAwait(false) ? old.Count : 0;
    }

    private static async ValueTask<LetterBody?> EndAsync(IPlayerContext transaction, IGameContext context, ILogger logger, MarketListing listing, MarketListingState state, CultureInfo culture)
    {
        try
        {
            var item = listing.Item;
            var message = MarketMail.Text(culture, state == MarketListingState.Cancelled ? "Market_CancelledBody" : "Market_ExpiredBody", listing.ItemName);
            var letter = item is null
                ? null
                : await MarketMail.CreateAsync(transaction, listing.SellerName, MarketMail.Text(culture, "Market_ReturnedSubject"), message, item, 0, 0).ConfigureAwait(false);
            listing.Item = null;
            listing.State = (int)state;
            listing.ClosedAt = DateTime.UtcNow;
            listing.Revision = Guid.NewGuid();
            return await transaction.SaveChangesAsync().ConfigureAwait(false) ? letter : null;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not end the Marketplace listing {Listing} of {Seller}", listing.Id, listing.SellerName);
            return null;
        }
    }
}
