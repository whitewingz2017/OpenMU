// <copyright file="DuelRoomManager.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic;

using System.Threading;
using MUnique.OpenMU.GameLogic.DuelBetting;
using MUnique.OpenMU.GameLogic.Views.Duel;
using Nito.AsyncEx;

/// <summary>
/// A class that manages several instances of <see cref="DuelRoom"/>.
/// </summary>
public class DuelRoomManager
{
    private readonly DuelConfiguration _configuration;
    private readonly AsyncLock _lock = new AsyncLock();
    private readonly DuelRoom?[] _duelRooms;

    /// <summary>
    /// Initializes a new instance of the <see cref="DuelRoomManager"/> class.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    public DuelRoomManager(DuelConfiguration configuration)
    {
        this._configuration = configuration;
        this._duelRooms = new DuelRoom?[this.MaxRoomCount];
    }

    /// <summary>
    /// Gets the maximum <see cref="DuelRoom"/>s count.
    /// </summary>
    public int MaxRoomCount => this._configuration?.DuelAreas.Count ?? 0;

    /// <summary>
    /// Reserves a <see cref="DuelRoom"/> for two players and records the challenger's betting choice.
    /// </summary>
    /// <param name="player1">The first player.</param>
    /// <param name="player2">The second player.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <param name="allowBetting">Whether the challenger elected public betting.</param>
    /// <param name="wagerAmount">The WCoinC stake proposed for each fighter.</param>
    /// <returns>A <see cref="ValueTask"/> with a free <see cref="DuelRoom"/>.</returns>
    public async ValueTask<DuelRoom?> GetFreeDuelRoomAsync(Player player1, Player player2, CancellationToken cancellationToken = default, bool allowBetting = false, int wagerAmount = 0)
    {
        using var l = await this._lock.LockAsync(cancellationToken).ConfigureAwait(false);
        if (player1.DuelRoom is not null || player2.DuelRoom is not null)
        {
            return null;
        }

        for (int i = 0; i < this._duelRooms.Length; i++)
        {
            if (this._duelRooms[i] is null)
            {
                var area = this._configuration.DuelAreas.First(a => a.Index == i);

                var room = new DuelRoom(area, player1, player2) { State = DuelState.DuelRequested, AllowBetting = allowBetting };
                var settings = allowBetting ? DuelBettingFeaturePlugIn.GetSettings(player1.GameContext) : null;
                if (wagerAmount > 0)
                {
                    if (settings is null || wagerAmount < settings.WCoinLimit.Minimum || wagerAmount > settings.WCoinLimit.Maximum)
                    {
                        return null;
                    }

                    room.Wager = new(wagerAmount, settings);
                }

                room.PublicCountdownSeconds = settings?.BettingSeconds ?? 0;
                this._duelRooms[i] = room;
                player1.DuelRoom = room;
                player2.DuelRoom = room;
                return room;
            }
        }

        return null;
    }

    /// <summary>
    /// Discards a <see cref="DuelRoom"/>.
    /// </summary>
    /// <param name="duelRoom">The <see cref="DuelRoom"/>.</param>
    /// <returns>A <see cref="ValueTask"/>.</returns>
    public async ValueTask GiveBackDuelRoomAsync(DuelRoom duelRoom)
    {
        using var l = await this._lock.LockAsync().ConfigureAwait(false);
        if (this._duelRooms[duelRoom.Index] == duelRoom)
        {
            this._duelRooms[duelRoom.Index] = null;
        }
    }

    /// <summary>
    /// Gets a <see cref="DuelRoom"/> by its index number.
    /// </summary>
    /// <param name="requestedDuelIndex">The index of the <see cref="DuelRoom"/>.</param>
    /// <returns>A <see cref="DuelRoom"/>.</returns>
    public DuelRoom? GetRoomByIndex(byte requestedDuelIndex)
    {
        if (this.MaxRoomCount <= requestedDuelIndex)
        {
            return null;
        }

        return this._duelRooms[requestedDuelIndex];
    }

    /// <summary>
    /// Shows the <see cref="DuelRoom"/>s to a player.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns>A <see cref="ValueTask"/>.</returns>
    public async ValueTask ShowRoomsAsync(Player player)
    {
        await player.InvokeViewPlugInAsync<IDuelStatusUpdatePlugIn>(p => p.UpdateStatusAsync(this._duelRooms)).ConfigureAwait(false);
    }

    /// <summary>Finds the exact public duel so a stale request cannot bet on a reused arena.</summary>
    /// <param name="matchId">The unique match identity.</param>
    /// <returns>The match if it is still listed.</returns>
    internal DuelBetRoom? GetPublicMatch(Guid matchId)
        => this._duelRooms.Select(room => room?.Betting).FirstOrDefault(betting => betting?.MatchId == matchId);

    /// <summary>Returns the public duels of this game channel as seen by one account.</summary>
    /// <param name="viewer">The player viewing the board.</param>
    /// <returns>The public match snapshots.</returns>
    internal async ValueTask<DuelBetBoardEntry[]> GetPublicMatchesAsync(Player viewer)
    {
        var matches = new List<DuelBetBoardEntry>();
        foreach (var room in this._duelRooms)
        {
            if (room is { Betting: { } betting, State: DuelState.DuelAccepted or DuelState.DuelStarted or DuelState.DuelFinished })
            {
                matches.Add(await betting.SnapshotAsync(viewer).ConfigureAwait(false));
            }
        }

        return matches.ToArray();
    }
}
