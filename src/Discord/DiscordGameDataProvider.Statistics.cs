// <copyright file="DiscordGameDataProvider.Statistics.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Discord;

using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.JewelBank;
using MUnique.OpenMU.GameLogic.MiniGames;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.GameLogic.PlugIns.GameEvents;
using MUnique.OpenMU.GameLogic.PlugIns.PeriodicTasks;
using MUnique.OpenMU.Interfaces;
using MUnique.OpenMU.Persistence;

/// <summary>Economy and live mini-game status snapshots.</summary>
public sealed partial class DiscordGameDataProvider
{
    /// <inheritdoc />
    public async ValueTask<DiscordEconomyStatus?> GetEconomyStatusAsync()
    {
        var codes = this._plugInManager.GetActivePlugInsOf<IMonsterItemDroppedPlugIn>()
            .OfType<JewelDropStatisticsPlugIn>().FirstOrDefault()?.Configuration?.ItemCodes
            ?? new JewelStatisticsConfiguration().ItemCodes;
        codes = codes.Distinct().Take(32).ToArray();
        using var context = this.CreatePlayerContext();
        var saved = await context.GetJewelStatisticsAsync(codes).ConfigureAwait(false);
        if (saved is null)
        {
            return null;
        }

        var owners = saved.Owners.ToDictionary(owner => (owner.OwnerId, owner.IsBank));
        foreach (var gameServer in this._serverProvider.Servers.OfType<IGameServerContextProvider>())
        {
            foreach (var player in await gameServer.Context.GetPlayersAsync().ConfigureAwait(false))
            {
                await player.RunPersistenceExclusiveAsync(() =>
                {
                    var now = DateTime.UtcNow;
                    if (player.SelectedCharacter?.Inventory is { } inventory)
                    {
                        owners[(inventory.GetId(), false)] = new JewelOwnerSnapshot(
                            inventory.GetId(),
                            false,
                            JewelStatistics.Count(inventory.Items, player.GameContext.Configuration, codes, now));
                    }

                    if (player.Account is { } account)
                    {
                        owners[(account.GetId(), true)] = new JewelOwnerSnapshot(account.GetId(), true, JewelBankLedger.Read(account.JewelBankBalances, codes));
                        if (account.Vault is { } vault)
                        {
                            owners[(vault.GetId(), false)] = new JewelOwnerSnapshot(
                                vault.GetId(),
                                false,
                                JewelStatistics.Count(vault.Items, player.GameContext.Configuration, codes, now));
                        }
                    }

                    return ValueTask.CompletedTask;
                }).ConfigureAwait(false);
            }
        }

        var totals = owners.Values.SelectMany(owner => owner.Amounts).GroupBy(amount => amount.ItemCode)
            .ToDictionary(group => group.Key, group => group.Sum(amount => amount.Amount));
        var produced = saved.Produced.ToDictionary(amount => amount.ItemCode, amount => amount.Amount);
        return new DiscordEconomyStatus(
            codes.Select(code => new JewelAmount(code, produced.GetValueOrDefault(code))).ToList(),
            codes.Select(code => new JewelAmount(code, totals.GetValueOrDefault(code))).ToList());
    }

    /// <inheritdoc />
    public ValueTask<IReadOnlyList<DiscordEventTimer>> GetEventTimersAsync()
    {
        var now = DateTime.UtcNow;
        var timers = new List<DiscordEventTimer>();
        foreach (var server in this._serverProvider.Servers.OfType<IGameServer>().Where(server => server.ServerState == ServerState.Started))
        {
            if (server is not IGameServerContextProvider source)
            {
                continue;
            }

            var context = source.Context;
            foreach (var start in context.PlugInManager.GetActivePlugInsOf<IPeriodicTaskPlugIn>().OfType<IPeriodicMiniGameStartPlugIn>())
            {
                var runs = context.MiniGames.GetRunningMiniGames(start.Key)
                    .Where(game => game.Definition.MapCreationPolicy == MiniGameMapCreationPolicy.Shared
                                   && game.State is MiniGameState.Open or MiniGameState.Closed or MiniGameState.Playing).ToList();
                var entrance = runs.Where(game => game.State == MiniGameState.Open).Select(game => (DateTime?)game.EnterEndsAtUtc).Max();
                if (entrance is { } closes)
                {
                    timers.Add(new DiscordEventTimer(start.Key.ToString(), context.Id, DiscordEventPhase.Entrance, closes));
                }
                else if (runs.Count > 0)
                {
                    timers.Add(new DiscordEventTimer(start.Key.ToString(), context.Id, DiscordEventPhase.Active, null));
                }
                else if (start.GetNextEntranceUtc(context, now) is { } opens)
                {
                    timers.Add(new DiscordEventTimer(start.Key.ToString(), context.Id, DiscordEventPhase.Opening, opens));
                }
            }
        }

        return ValueTask.FromResult<IReadOnlyList<DiscordEventTimer>>(timers.OrderBy(timer => timer.DeadlineUtc ?? now).ThenBy(timer => timer.ServerId).ToList());
    }
}
