// <copyright file="MarketListingCreation.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Market;

using MUnique.OpenMU.Persistence;

/// <summary>Moves an inventory item into a new listing, in one transaction.</summary>
internal static class MarketListingCreation
{
    /// <summary>Lists the item of the request's slot.</summary>
    /// <param name="player">The seller. The caller holds its persistence lock and the market's write lock.</param>
    /// <param name="settings">The settings.</param>
    /// <param name="listings">The current listings, for the limit.</param>
    /// <param name="request">The slot and the price.</param>
    /// <param name="serializeItem">Serializes an item as the client's inventory list carries it, hex.</param>
    /// <returns><see cref="MarketResult.Listed"/>, or why the item isn't listed.</returns>
    public static async ValueTask<MarketResult> ListAsync(Player player, MarketSettings settings, IReadOnlyList<MarketListingSummary> listings, MarketRequestData request, Func<Item, string> serializeItem)
    {
        if (request.Slot is < 0 or > byte.MaxValue
            || player.Inventory?.GetItem((byte)request.Slot) is not { } item
            || !MarketItems.CanSell(item))
        {
            return MarketResult.ItemNotAllowed;
        }

        if (!MarketCurrency.IsValidPrice(settings, player.GameContext.Configuration, request.PriceType, request.Price))
        {
            return MarketResult.PriceNotAllowed;
        }

        if (listings.Count(l => l.State == MarketListingState.Active && l.Seller == player.Name) >= settings.MaximumListings)
        {
            return MarketResult.ListingLimit;
        }

        return await SaveAsync(player, settings, request, item, serializeItem(item)).ConfigureAwait(false);
    }

    private static async ValueTask<MarketResult> SaveAsync(Player player, MarketSettings settings, MarketRequestData request, Item item, string itemData)
    {
        var slot = item.ItemSlot;
        using var transaction = player.GameContext.PersistenceContextProvider.CreateNewPlayerContext(player.GameContext.Configuration);
        transaction.Attach(player.Account!);
        transaction.Attach(player.SelectedCharacter!);
        var listing = transaction.CreateNew<MarketListing>();
        Fill(listing, player, settings, request, item, itemData);
        var removed = false;
        var committed = false;
        try
        {
            await player.Inventory!.RemoveItemAsync(item).ConfigureAwait(false);
            removed = true;
            listing.Item = item;
            committed = await transaction.SaveChangesAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            player.Logger.LogWarning(ex, "Could not list {Item} of {Player} on the Marketplace", item, player);
        }
        finally
        {
            if (!committed)
            {
                listing.Item = null;
                if (removed)
                {
                    await player.Inventory!.AddItemAsync(slot, item).ConfigureAwait(false);
                }
            }
        }

        if (!committed)
        {
            return MarketResult.TryAgain;
        }

        player.PersistenceContext.Detach(item);
        return MarketResult.Listed;
    }

    private static void Fill(MarketListing listing, Player player, MarketSettings settings, MarketRequestData request, Item item, string itemData)
    {
        var now = DateTime.UtcNow;
        listing.SellerAccountId = player.Account!.GetId();
        listing.SellerName = player.Name;
        listing.ItemData = itemData;
        listing.ItemName = MarketItems.Name(item);
        listing.PriceType = request.PriceType;
        listing.Price = request.Price;
        listing.ListedAt = now;
        listing.ExpiresAt = now.AddHours(settings.ListingHours);
        listing.State = (int)MarketListingState.Active;
        listing.Revision = Guid.NewGuid();
    }
}
