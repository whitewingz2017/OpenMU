// <copyright file="PlayTimeTrackingPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.Offline;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Adds the time each account spends in the game to its <see cref="Account.PlayTimeSeconds"/>.
/// </summary>
/// <remarks>
/// Counted once a minute for the players in the game; offline leveling, bot and template accounts don't count.
/// The account is saved with the player's progress, so the up to one minute since the last count
/// is lost when the player leaves the game.
/// </remarks>
[PlugIn]
[Display(Name = "Play time tracking", Description = "Adds the time each account spends in the game to its play time, shown in the admin panel's account list.")]
[Guid("6F0F1B5E-2C3A-4B8E-9D1F-7A4C5E2B9D30")]
public class PlayTimeTrackingPlugIn : IPeriodicTaskPlugIn
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(1);

    private readonly ConditionalWeakTable<Player, StrongBox<DateTime>> _countedUntil = new();

    private DateTime _nextRunUtc = DateTime.UtcNow;

    /// <inheritdoc />
    public async ValueTask ExecuteTaskAsync(GameContext gameContext)
    {
        var now = DateTime.UtcNow;
        if (now < this._nextRunUtc)
        {
            return;
        }

        this._nextRunUtc = now + CheckInterval;
        foreach (var player in await gameContext.GetPlayersAsync().ConfigureAwait(false))
        {
            if (!IsCounted(player))
            {
                this._countedUntil.Remove(player);
                continue;
            }

            var countedUntil = this._countedUntil.GetValue(player, _ => new StrongBox<DateTime>(now));
            var seconds = (long)(now - countedUntil.Value).TotalSeconds;
            if (seconds <= 0)
            {
                continue;
            }

            // The remainder below a second carries over to the next count.
            countedUntil.Value = countedUntil.Value.AddSeconds(seconds);
            await AddAsync(player, seconds).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public void ForceStart()
    {
        this._nextRunUtc = DateTime.UtcNow;
    }

    private static bool IsCounted(Player player)
    {
        return player.PlayerState.CurrentState == PlayerState.EnteredWorld
               && player is not OfflinePlayer
               && player.Account is { IsBot: false, IsTemplate: false };
    }

    private static async ValueTask AddAsync(Player player, long seconds)
    {
        if (player.Account is not { } account)
        {
            return;
        }

        try
        {
            await player.RunPersistenceExclusiveAsync(() =>
            {
                account.PlayTimeSeconds += seconds;
                return ValueTask.CompletedTask;
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            player.Logger.LogError(ex, "Error when adding the play time of player {player}.", player);
        }
    }
}
