// <copyright file="RankTitleActions.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.RankTitles;

using System.Runtime.CompilerServices;
using System.Text.Json;
using MUnique.OpenMU.GameLogic.CashShop;
using MUnique.OpenMU.GameLogic.Views.Inventory;

/// <summary>Purchases and equips permanent titles under the same persistence lock as skin purchases.</summary>
public sealed class RankTitleActions
{
    private readonly ConditionalWeakTable<Player, Session> _sessions = new();

    /// <summary>Processes a catalog request, purchase or title equipment change.</summary>
    /// <param name="player">The purchasing player.</param>
    /// <param name="requestId">The request identifier used to reject replays.</param>
    /// <param name="operation">Zero loads, one buys, two equips or clears a title.</param>
    /// <param name="rank">The configured title identifier, or zero to unequip.</param>
    /// <param name="quotedPrice">The WCoinC price the client was shown.</param>
    /// <returns>The completion of the request and its response.</returns>
    public ValueTask ExecuteAsync(Player player, uint requestId, byte operation, int rank, int quotedPrice)
        => player.RunPersistenceExclusiveAsync(async () =>
        {
            var status = await this.ProcessAsync(player, requestId, operation, rank, quotedPrice).ConfigureAwait(false);
            await player.InvokeViewPlugInAsync<IRankTitleView>(view => view.ShowAsync(requestId, status)).ConfigureAwait(false);
            await player.ForEachWorldObserverAsync<IRankTitleView>(view => view.AppearanceAsync(player), true).ConfigureAwait(false);
        });

    private static async ValueTask<string> ChangeAsync(Player player, RankTitleSettings settings, HashSet<int> owned, byte operation, int rank, int quotedPrice)
    {
        if (player.OpenedNpc is not null || player.TemporaryStorage?.Items.Any() == true
            || player.ShopStorage?.StoreOpen == true || player.IsCashShopOpen)
        {
            return "RankTitles_CloseWindows";
        }

        var offer = settings.Titles.SingleOrDefault(title => title.Rank == rank);
        if (operation == 1)
        {
            if (RankTitleRules.IsEarned(rank) || offer is null || offer.Price <= 0 || quotedPrice != offer.Price || owned.Count >= RankTitleFeaturePlugIn.MaximumOwnedTitles)
            {
                return "RankTitles_PriceChanged";
            }

            if (owned.Contains(rank))
            {
                return "RankTitles_Owned";
            }

            if (player.Account!.WCoinC < offer.Price)
            {
                return "RankTitles_Insufficient";
            }

            owned.Add(rank);
        }
        else if (operation != 2 || quotedPrice != 0 || (rank != 0 && (offer is null || !RankTitleRules.CanEquip(player, rank, owned))))
        {
            return "RankTitles_Invalid";
        }

        return await SaveAsync(player, owned, operation, rank, offer?.Price ?? 0).ConfigureAwait(false);
    }

    private static async ValueTask<string> SaveAsync(Player player, HashSet<int> owned, byte operation, int rank, int price)
    {
        var character = player.SelectedCharacter!;
        var account = player.Account!;
        var oldBalance = account.WCoinC;
        var oldOwned = character.OwnedRankTitlesJson;
        var oldActive = character.ActiveRankTitle;
        var oldHidden = character.HideRankTitle;
        var committed = false;
        try
        {
            if (operation == 1)
            {
                character.OwnedRankTitlesJson = JsonSerializer.Serialize(owned.Order());
                account.WCoinC -= price;
            }
            else
            {
                character.ActiveRankTitle = rank;
                character.HideRankTitle = rank == 0;
            }

            committed = await player.SaveProgressAsync().ConfigureAwait(false);
            return committed ? (operation == 1 ? "RankTitles_Purchased" : "RankTitles_Equipped") : "RankTitles_SaveFailed";
        }
        catch (Exception exception)
        {
            player.Logger.LogError(exception, "Saving a rank title failed for {Player}.", player);
            return "RankTitles_SaveFailed";
        }
        finally
        {
            if (!committed)
            {
                account.WCoinC = oldBalance;
                character.OwnedRankTitlesJson = oldOwned;
                character.ActiveRankTitle = oldActive;
                character.HideRankTitle = oldHidden;
            }
        }
    }

    private async ValueTask<string?> ProcessAsync(Player player, uint requestId, byte operation, int rank, int quotedPrice)
    {
        if (player.SelectedCharacter is not { } character || player.Account is null || player.IsTemplatePlayer
            || player.PlayerState.CurrentState != PlayerState.EnteredWorld)
        {
            return "RankTitles_Invalid";
        }

        var session = this._sessions.GetValue(player, _ => new());
        if (!ReferenceEquals(session.Character, character))
        {
            session.Character = character;
            session.LastRequest = 0;
        }

        if (requestId == 0 || requestId <= session.LastRequest)
        {
            return "RankTitles_Retry";
        }

        session.LastRequest = requestId;
        await player.ApplyPendingCashShopCoinGrantsAsync().ConfigureAwait(false);
        if (player.GameContext.FeaturePlugIns.GetPlugIn<RankTitleFeaturePlugIn>() is { } feature)
        {
            await feature.RefreshLeadersAsync(player.GameContext).ConfigureAwait(false);
        }

        if (RankTitleFeaturePlugIn.Settings(player.GameContext) is not { } settings)
        {
            return "RankTitles_Disabled";
        }

        if (!RankTitleRules.TryReadOwned(character, out var owned))
        {
            player.Logger.LogError("Invalid persisted title ownership for {Player}.", player);
            return "RankTitles_InvalidData";
        }

        return operation == 0 && rank == 0 && quotedPrice == 0 ? null
            : await ChangeAsync(player, settings, owned, operation, rank, quotedPrice).ConfigureAwait(false);
    }

    private sealed class Session
    {
        public Character? Character { get; set; }

        public uint LastRequest { get; set; }
    }
}
