// <copyright file="DuelActions.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions.Duel;

using System.Threading;
using Microsoft.Extensions.Logging;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic.DuelBetting;
using MUnique.OpenMU.GameLogic.GuildWar;
using MUnique.OpenMU.GameLogic.PlugIns.ChatCommands;
using MUnique.OpenMU.GameLogic.Views.Duel;

/// <summary>
/// Actions regarding duels.
/// </summary>
public class DuelActions
{
    /// <summary>
    /// The time after which an unanswered duel request ends, like a refused one.
    /// </summary>
    private static readonly TimeSpan UnansweredRequestTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Handles a duel request.
    /// </summary>
    /// <param name="player">The player which sends the request.</param>
    /// <param name="target">The target player which receives the request.</param>
    /// <param name="allowBetting">Whether the challenger opted into public betting.</param>
    /// <param name="wagerAmount">The proposed stake per fighter in WCoinC.</param>
    public async ValueTask HandleDuelRequestAsync(Player player, Player target, bool allowBetting = false, int wagerAmount = 0)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(target);

        if (player.DuelRoom is not null)
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.AlreadyInDuel)).ConfigureAwait(false);
            return;
        }

        if (target.DuelRoom is not null)
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.OtherPlayerAlreadyInDuel)).ConfigureAwait(false);
            return;
        }

        if (!await CheckIfDuelCanBeStartedAsync(player, target).ConfigureAwait(false))
        {
            return;
        }

        if (await player.GameContext.DuelRoomManager.GetFreeDuelRoomAsync(player, target, allowBetting: allowBetting, wagerAmount: wagerAmount).ConfigureAwait(false) is not { } duelRoom)
        {
            await player.InvokeViewPlugInAsync<IShowDuelRequestResultPlugIn>(p => p.ShowDuelRequestResultAsync(DuelStartResult.FailedByNoFreeRoom, target)).ConfigureAwait(false);
            return;
        }

        _ = ExpireUnansweredRequestAsync(duelRoom);
        var publicDuel = duelRoom.PublicCountdownSeconds > 0;
        var settings = new DuelBetInvitation(
            player.Name,
            player.SelectedCharacter?.CharacterClass?.Number,
            publicDuel,
            publicDuel ? duelRoom.PublicCountdownSeconds : DuelRoom.CombatCountdownSeconds,
            duelRoom.WinningScore,
            duelRoom.Wager?.OfferId,
            wagerAmount);
        await target.InvokeViewPlugInAsync<IDuelBettingView>(view => view.ShowInvitationAsync(settings)).ConfigureAwait(false);
        await target.InvokeViewPlugInAsync<IShowDuelRequestPlugIn>(p => p.ShowDuelRequestAsync(player)).ConfigureAwait(false);
    }

    /// <summary>
    /// Handles the duel response.
    /// </summary>
    /// <param name="player">The player which responds to the request.</param>
    /// <param name="requesterId">The challenger’s map ID sent with the invitation.</param>
    /// <param name="requesterName">The challenger’s character name sent with the invitation.</param>
    /// <param name="accepted">If set to <c>true</c> the duel request was accepted by the player.</param>
    /// <param name="offerId">The terms explicitly acknowledged by the opponent for a paid duel.</param>
    public async ValueTask HandleDuelResponseAsync(Player player, ushort requesterId, string requesterName, bool accepted, Guid? offerId = null)
    {
        if (player.DuelRoom is not { } duelRoom)
        {
            player.Logger.LogWarning($"Player {player.Name} sent duel response, but has no context.");
            return;
        }

        using var l = await duelRoom.Lock.LockAsync().ConfigureAwait(false);
        var target = duelRoom.Requester;

        // The invitation belongs to the reserved challenger, even if they moved out of sight.
        if (duelRoom != player.DuelRoom || duelRoom != target.DuelRoom || duelRoom.Opponent != player
            || duelRoom.State != DuelState.DuelRequested)
        {
            player.Logger.LogWarning(
                "Player {player} sent a duel response outside their pending invitation. Room state: {state}, invited opponent: {opponent}.",
                player.Name,
                duelRoom.State,
                duelRoom.Opponent.Name);
            return;
        }

        var nameMatches = string.Equals(requesterName, target.Name, StringComparison.Ordinal);
        if (requesterId != target.Id || !nameMatches)
        {
            player.Logger.LogWarning(
                "Player {player} sent a duel response with a mismatched challenger. Received ID: {requesterId}, expected ID: {expectedId}, name matches: {nameMatches}.",
                player.Name,
                requesterId,
                target.Id,
                nameMatches);
            return;
        }

        if (offerId is not null && offerId != duelRoom.Wager?.OfferId)
        {
            await player.ShowLocalizedBlueMessageAsync("DuelBetting_OfferExpired").ConfigureAwait(false);
            return;
        }

        if (!accepted)
        {
            await duelRoom.ResetAndDisposeAsync(DuelStartResult.Refused).ConfigureAwait(false);
            return;
        }

        if (duelRoom.Wager is not null && offerId is null)
        {
            await player.ShowLocalizedBlueMessageAsync("DuelBetting_ConfirmWager").ConfigureAwait(false);
            return;
        }

        if (!await CheckIfDuelCanBeStartedAsync(player, target).ConfigureAwait(false))
        {
            await duelRoom.ResetAndDisposeAsync(DuelStartResult.FailedByError).ConfigureAwait(false);
            return;
        }

        var duelConfig = player.GameContext.Configuration.DuelConfiguration;
        if (duelConfig is null)
        {
            player.Logger.LogError("Duel configuration is not set.");
            await duelRoom.ResetAndDisposeAsync(DuelStartResult.FailedByError).ConfigureAwait(false);
            return;
        }

        if (duelConfig.DuelAreas.FirstOrDefault(area => area.Index == duelRoom.Index) is not { } duelArea)
        {
            player.Logger.LogError("Duel area with index {index} was not found.", duelRoom.Index);
            await duelRoom.ResetAndDisposeAsync(DuelStartResult.FailedByError).ConfigureAwait(false);
            return;
        }

        if (duelArea.FirstPlayerGate is null || duelArea.SecondPlayerGate is null)
        {
            player.Logger.LogError("Duel area with index {index} has missing exit gates.", duelRoom.Index);
            await duelRoom.ResetAndDisposeAsync(DuelStartResult.FailedByError).ConfigureAwait(false);
            return;
        }

        if (!await FundWagerAsync(duelRoom).ConfigureAwait(false))
        {
            await duelRoom.ResetAndDisposeAsync(DuelStartResult.FailedByError).ConfigureAwait(false);
            return;
        }

        try
        {
            player.TryRemoveMoney(duelConfig.EntranceFee);
            target.TryRemoveMoney(duelConfig.EntranceFee);
            await duelRoom.Requester.WarpToAsync(duelArea.FirstPlayerGate).ConfigureAwait(false);
            await duelRoom.Opponent.WarpToAsync(duelArea.SecondPlayerGate).ConfigureAwait(false);
            duelRoom.State = DuelState.DuelAccepted;

            // The duel continues independently of the response packet's player persistence lock.
            using (ExecutionContext.SuppressFlow())
            {
                _ = Task.Run(duelRoom.RunDuelAsync);
            }
        }
        catch
        {
            await duelRoom.SettleBetsAsync(null, creditOnline: false).ConfigureAwait(false);
            await duelRoom.ResetAndDisposeAsync(DuelStartResult.FailedByError).ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>
    /// Handles the duel stop request of a player.
    /// </summary>
    /// <param name="player">The player which requests to stop the duel.</param>
    public async ValueTask HandleStopDuelRequestAsync(Player player)
    {
        var duelRoom = player.DuelRoom;
        if (duelRoom is null || !duelRoom.IsDuelist(player))
        {
            player.Logger.LogWarning($"Player {player.Name} sent request to stop the duel, but it has no duel active.");
            return;
        }

        if (duelRoom.State is DuelState.DuelFinished)
        {
            player.Logger.LogWarning($"Player {player.Name} sent request to stop the duel, but it is already finished.");
            return;
        }

        await duelRoom.CancelDuelAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Handles the duel channel join request asynchronous.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="requestedDuelIndex">Index of the requested duel.</param>
    /// <returns>The value task with the result.</returns>
    public async ValueTask HandleDuelChannelJoinRequestAsync(Player player, byte requestedDuelIndex)
    {
        var config = player.GameContext.Configuration.DuelConfiguration;

        if (config is null)
        {
            player.Logger.LogError("Duel configuration is not set.");
            return;
        }

        if (player.GameContext.DuelRoomManager.GetRoomByIndex(requestedDuelIndex) is not { } duelRoom)
        {
            player.Logger.LogWarning($"Player {player.Name} tried to join duel channel with index {requestedDuelIndex}, but it doesn't exist.");
            return;
        }

        if (duelRoom.Spectators.Count >= config.MaximumSpectatorsPerDuelRoom)
        {
            player.Logger.LogWarning($"Player {player.Name} tried to join duel channel with index {requestedDuelIndex}, but it is full.");
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.DuelChannelIsFull)).ConfigureAwait(false);
            return;
        }

        // move to duel map
        var spectatorsGate = config.DuelAreas.FirstOrDefault(area => area.Index == requestedDuelIndex)?.SpectatorsGate;
        if (spectatorsGate is null)
        {
            player.Logger.LogError("Duel area with index {index} was not found or has missing spectators gate.", requestedDuelIndex);
            return;
        }

        if (!await duelRoom.TryAddSpectatorAsync(player).ConfigureAwait(false))
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.DuelChannelIsFull)).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Handles the duel channel quit request asynchronous.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns>The value task with the result.</returns>
    public async ValueTask HandleDuelChannelQuitRequestAsync(Player player)
    {
        if (player.DuelRoom is not { } duelRoom)
        {
            return;
        }

        if (duelRoom.IsDuelist(player))
        {
            // todo: log this attempt?
            return;
        }

        if (player.GameContext.Configuration.DuelConfiguration is not { } config)
        {
            return;
        }

        if (config.DuelAreas.FirstOrDefault(area => area.Index == duelRoom.Index) is not { } area)
        {
            return;
        }

        var isInDuelMap = area.SpectatorsGate?.Map == player.CurrentMap?.Definition;
        if (!isInDuelMap)
        {
            return;
        }

        if (config.Exit is { Map: not null } exitGate)
        {
            await player.WarpToAsync(exitGate).ConfigureAwait(false);
        }
        else
        {
            await player.WarpToSafezoneAsync().ConfigureAwait(false);
        }

        await player.RemoveInvisibleEffectAsync().ConfigureAwait(false);
        await duelRoom.RemoveSpectatorAsync(player).ConfigureAwait(false);
    }

    /// <summary>
    /// Ends a duel request which is still unanswered after <see cref="UnansweredRequestTimeout"/>, e.g. because
    /// the other player's client never showed it. Both players would otherwise stay "already in a duel".
    /// </summary>
    private static async Task ExpireUnansweredRequestAsync(DuelRoom duelRoom)
    {
        try
        {
            await Task.Delay(UnansweredRequestTimeout).ConfigureAwait(false);
            using var l = await duelRoom.Lock.LockAsync().ConfigureAwait(false);
            if (duelRoom.State == DuelState.DuelRequested && duelRoom.Requester.DuelRoom == duelRoom)
            {
                await duelRoom.ResetAndDisposeAsync(DuelStartResult.Refused).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            duelRoom.Requester.Logger.LogError(ex, "Expiring the unanswered duel request of {player} failed.", duelRoom.Requester.Name);
        }
    }

    private static async ValueTask<bool> FundWagerAsync(DuelRoom duelRoom)
    {
        if (duelRoom.Wager is not { } wager)
        {
            return true;
        }

        var result = DuelBetRejection.TryAgain;
        try
        {
            result = await wager.FundAsync(duelRoom.Requester, duelRoom.Opponent).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            duelRoom.Requester.Logger.LogWarning(ex, "Unable to fund the wager for duel room {Room}.", duelRoom.Index);
        }

        if (result == DuelBetRejection.None)
        {
            return true;
        }

        var message = result == DuelBetRejection.NotEnoughWCoins ? "DuelBetting_WagerCannotFund" : $"DuelBetting_{result}";
        await duelRoom.Requester.ShowLocalizedBlueMessageAsync(message).ConfigureAwait(false);
        await duelRoom.Opponent.ShowLocalizedBlueMessageAsync(message).ConfigureAwait(false);
        return false;
    }

    private static async ValueTask<bool> CheckIfDuelCanBeStartedAsync(Player player, Player target)
    {
        if (player == target)
        {
            player.Logger.LogError("Player requested a duel with himself.");
            return false;
        }

        if (player.SelectedCharacter is not { } selectedCharacter)
        {
            player.Logger.LogError("Player requested a duel while not selecting a character.");
            return false;
        }

        if (target.SelectedCharacter is not { } targetCharacter)
        {
            // target logged out in the mean time.
            return false;
        }

        if (player.CurrentMap != target.CurrentMap)
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.DuelRequestMustBeOnSameMap)).ConfigureAwait(false);
            return false;
        }

        if (player.CurrentMiniGame is not null)
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.NoDuelDuringMiniGame)).ConfigureAwait(false);
            return false;
        }

        if (selectedCharacter.State >= HeroState.PlayerKiller2ndStage)
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.NoDuelWhileBeingPlayerKiller)).ConfigureAwait(false);
            return false;
        }

        if (targetCharacter.State >= HeroState.PlayerKiller2ndStage)
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.NoDuelWithPlayerKiller)).ConfigureAwait(false);
            return false;
        }

        if (player.GuildWarContext?.State is GuildWarState.Requested or GuildWarState.Started
            || target.GuildWarContext?.State is GuildWarState.Requested or GuildWarState.Started)
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.NoDuelDuringGuildWar)).ConfigureAwait(false);
            return false;
        }

        if (player.IsAnySelfDefenseActive()
            || target.IsAnySelfDefenseActive())
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.NoDuelDuringSelfDefense)).ConfigureAwait(false);
            return false;
        }

        if (player.OpenedNpc is not null
            || target.OpenedNpc is not null)
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.NoDuelWithOpenedNpcDialog)).ConfigureAwait(false);
            return false;
        }

        if (player.PlayerState.CurrentState != PlayerState.EnteredWorld
            || target.PlayerState.CurrentState != PlayerState.EnteredWorld)
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.NoDuelWithWrongState)).ConfigureAwait(false);
            return false;
        }

        var duelConfig = player.GameContext.Configuration.DuelConfiguration;
        if (player.Level < duelConfig?.MinimumCharacterLevel || target.Level < duelConfig?.MinimumCharacterLevel)
        {
            await player.InvokeViewPlugInAsync<IShowDuelRequestResultPlugIn>(p => p.ShowDuelRequestResultAsync(DuelStartResult.FailedByTooLowLevel, target)).ConfigureAwait(false);
            return false;
        }

        if (player.Money < duelConfig?.EntranceFee || target.Money < duelConfig?.EntranceFee)
        {
            await player.InvokeViewPlugInAsync<IShowDuelRequestResultPlugIn>(p => p.ShowDuelRequestResultAsync(DuelStartResult.FailedByNotEnoughMoney, target)).ConfigureAwait(false);
            return false;
        }

        return true;
    }
}
