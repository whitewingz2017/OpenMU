// <copyright file="DuelBettingActions.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.DuelBetting;

using System.Text.Json;
using MUnique.OpenMU.GameLogic.PlayerActions.Duel;
using MUnique.OpenMU.GameLogic.Views.Duel;
using MUnique.OpenMU.Persistence;

/// <summary>Handles challenge opt-in, the public board, stakes, and account payout claims.</summary>
public sealed class DuelBettingActions
{
    private static readonly DuelActions Duels = new();

    /// <summary>Executes the player's requested operation and sends a refreshed board.</summary>
    /// <param name="player">The player who sent the request.</param>
    /// <param name="requestId">The request identifier echoed in the answer.</param>
    /// <param name="operation">The requested operation.</param>
    /// <param name="data">The UTF-8 JSON for challenge and place operations.</param>
    /// <returns>The asynchronous completion.</returns>
    public async ValueTask ExecuteAsync(Player player, uint requestId, byte operation, ReadOnlyMemory<byte> data)
    {
        if (player.Account is not { } account || player.SelectedCharacter is null || player.IsTemplatePlayer
            || player.PlayerState.CurrentState != PlayerState.EnteredWorld)
        {
            return;
        }

        var accountId = account.GetId();
        var settings = DuelBettingFeaturePlugIn.GetSettings(player.GameContext);
        var enabled = settings is not null;
        var status = await RunAsync(player, accountId, (DuelBetOperation)operation, data, enabled).ConfigureAwait(false);
        if (operation == (byte)DuelBetOperation.Challenge && status.Length > 0)
        {
            await player.ShowBlueMessageAsync(status).ConfigureAwait(false);
        }

        var matches = await player.GameContext.DuelRoomManager.GetPublicMatchesAsync(player).ConfigureAwait(false);
        var firstTo = player.GameContext.Configuration.DuelConfiguration?.MaximumScore ?? 0;
        await player.InvokeViewPlugInAsync<IDuelBettingView>(
            view => view.ShowAsync(requestId, enabled, status, matches, settings?.BettingSeconds ?? 0, DuelRoom.CombatCountdownSeconds, firstTo)).ConfigureAwait(false);
    }

    private static async ValueTask<string> RunAsync(Player player, Guid accountId, DuelBetOperation operation, ReadOnlyMemory<byte> data, bool enabled)
    {
        try
        {
            switch (operation)
            {
                case DuelBetOperation.Load:
                    return string.Empty;
                case DuelBetOperation.Claim:
                    return await ClaimAsync(player).ConfigureAwait(false);
                case DuelBetOperation.Spectate:
                    return await SpectateAsync(player, JsonSerializer.Deserialize<DuelBetRequest>(data.Span)).ConfigureAwait(false);
                case DuelBetOperation.Place:
                    return await PlaceAsync(player, accountId, JsonSerializer.Deserialize<DuelBetRequest>(data.Span)).ConfigureAwait(false);
                case DuelBetOperation.Challenge:
                    return enabled
                        ? await ChallengeAsync(player, JsonSerializer.Deserialize<DuelBetRequest>(data.Span)).ConfigureAwait(false)
                        : player.GetLocalizedMessage("DuelBetting_Disabled");
                default:
                    return player.GetLocalizedMessage("DuelBetting_Invalid");
            }
        }
        catch (JsonException)
        {
            return player.GetLocalizedMessage("DuelBetting_Invalid");
        }
        catch (Exception ex)
        {
            player.Logger.LogWarning(ex, "Duel betting operation {Operation} failed for {Player}", operation, player.Name);
            return player.GetLocalizedMessage("DuelBetting_TryAgain");
        }
    }

    private static async ValueTask<string> ChallengeAsync(Player player, DuelBetRequest? request)
    {
        if (request?.TargetName is not { Length: > 0 } name
            || await player.GetObservingPlayerWithIdAsync(request.TargetId).ConfigureAwait(false) is not { } target
            || !string.Equals(name, target.Name, StringComparison.Ordinal))
        {
            return player.GetLocalizedMessage("DuelBetting_Invalid");
        }

        await Duels.HandleDuelRequestAsync(player, target, allowBetting: true).ConfigureAwait(false);
        return string.Empty;
    }

    private static async ValueTask<string> PlaceAsync(Player player, Guid accountId, DuelBetRequest? request)
    {
        if (request is not { Side: { } side, Currency: { } currency } || request.Ticket == Guid.Empty)
        {
            return player.GetLocalizedMessage("DuelBetting_Invalid");
        }

        var match = player.GameContext.DuelRoomManager.GetPublicMatch(request.Duel);
        if (match is null)
        {
            return player.GetLocalizedMessage("DuelBetting_Closed");
        }

        var ticket = new DuelBetTicket(request.Ticket, accountId, currency, side, request.Amount);
        var result = await player.RunPersistenceExclusiveAsync(() => match.PlaceAsync(player, ticket)).ConfigureAwait(false);
        return player.GetLocalizedMessage($"DuelBetting_{result}");
    }

    private static async ValueTask<string> ClaimAsync(Player player)
    {
        var claimed = await player.RunPersistenceExclusiveAsync(() => DuelBetWallet.ClaimAsync(player)).ConfigureAwait(false);
        return claimed switch
        {
            < 0 => player.GetLocalizedMessage("DuelBetting_TryAgain"),
            0 => player.GetLocalizedMessage("DuelBetting_NothingToClaim"),
            _ => player.GetLocalizedMessage("DuelBetting_Claimed", claimed),
        };
    }

    private static async ValueTask<string> SpectateAsync(Player player, DuelBetRequest? request)
    {
        if (request is not null && player.GameContext.DuelRoomManager.GetPublicMatch(request.Duel) is { } match
            && await match.SpectateAsync(player).ConfigureAwait(false))
        {
            return player.GetLocalizedMessage("DuelBetting_Spectating");
        }

        return player.GetLocalizedMessage("DuelBetting_CannotSpectate");
    }
}
