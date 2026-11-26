// <copyright file="DuelBetRoom.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.DuelBetting;

using MUnique.OpenMU.Persistence;
using Nito.AsyncEx;

/// <summary>The public bet book and the persisted stakes for one accepted challenge.</summary>
internal sealed class DuelBetRoom(DuelRoom room, DuelBetBook book)
{
    // An abandoned match becomes refundable after its last lease. Live matches renew the lease before it expires.
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan RenewalMargin = TimeSpan.FromMinutes(1);

    // Placement takes the player's persistence lock before this lock. Settlement and snapshots take only this lock.
    private readonly AsyncLock _lock = new();
    private Guid _escrowId;
    private DateTime _leaseUntil;

    /// <summary>Gets the stable identity of this match for the board and stake requests.</summary>
    internal Guid MatchId { get; } = Guid.NewGuid();

    /// <summary>Persists the stake before adding it to the public pool.</summary>
    /// <param name="player">The bettor, who holds the player persistence lock.</param>
    /// <param name="ticket">The proposed stake.</param>
    /// <returns>The admission or payment result.</returns>
    internal async ValueTask<DuelBetRejection> PlaceAsync(Player player, DuelBetTicket ticket)
    {
        using var l = await this._lock.LockAsync().ConfigureAwait(false);
        if (player.Account?.GetId() != ticket.AccountId || player.PlayerState.CurrentState != PlayerState.EnteredWorld)
        {
            return DuelBetRejection.InvalidIdentity;
        }

        if (book.Tickets.FirstOrDefault(existing => existing.Id == ticket.Id) is { } existing)
        {
            return existing == ticket ? DuelBetRejection.None : DuelBetRejection.DuplicateTicket;
        }

        var now = DateTime.UtcNow;
        if (!room.CanTakeBets)
        {
            return DuelBetRejection.Closed;
        }

        var result = book.Validate(ticket, now);
        if (result != DuelBetRejection.None)
        {
            return result;
        }

        var leaseUntil = now.Add(LeaseDuration);
        var escrowId = await DuelBetWallet.DebitAsync(player, this._escrowId, ticket, leaseUntil).ConfigureAwait(false);
        if (escrowId is null)
        {
            return DuelBetRejection.TryAgain;
        }

        if (escrowId == Guid.Empty)
        {
            return DuelBetRejection.NotEnoughJewels;
        }

        this._escrowId = escrowId.Value;
        this._leaseUntil = leaseUntil;
        return book.Add(ticket, now);
    }

    /// <summary>Reads the state available to the player opening the board.</summary>
    /// <param name="viewer">The player viewing the board.</param>
    /// <returns>A public snapshot with the viewer's eligibility.</returns>
    internal async ValueTask<DuelBetBoardEntry> SnapshotAsync(Player viewer)
    {
        using var l = await this._lock.LockAsync().ConfigureAwait(false);
        var accountId = viewer.Account?.GetId() ?? Guid.Empty;
        var now = DateTime.UtcNow;
        var open = room.CanTakeBets && book.IsOpen(now);
        var secondsLeft = open ? (int)Math.Ceiling((book.ClosesAtUtc - now).TotalSeconds) : 0;
        var eligible = open && accountId != Guid.Empty && !book.IsDuelist(accountId);
        var pools = book.Tickets.GroupBy(ticket => ticket.Currency).Select(pool => new DuelBetPool(
            pool.Key,
            pool.Where(ticket => ticket.Side == DuelBetSide.Requester).Sum(ticket => ticket.Amount),
            pool.Where(ticket => ticket.Side == DuelBetSide.Opponent).Sum(ticket => ticket.Amount))).ToArray();
        var firstClass = room.Requester.SelectedCharacter?.CharacterClass?.Number;
        var secondClass = room.Opponent.SelectedCharacter?.CharacterClass?.Number;
        var started = room.State == DuelState.DuelStarted;
        var completed = room.State == DuelState.DuelFinished;
        var hasBet = book.Tickets.Any(ticket => ticket.AccountId == accountId);
        return new(
            this.MatchId,
            room.Requester.Name,
            room.Opponent.Name,
            room.ScoreRequester,
            room.ScoreOpponent,
            open,
            eligible,
            secondsLeft,
            pools,
            firstClass,
            secondClass,
            started,
            completed,
            hasBet,
            this.CanSpectate(viewer),
            room.WinningScore);
    }

    /// <summary>Joins the spectator area of the selected match.</summary>
    /// <param name="viewer">The player joining the match.</param>
    /// <returns>Whether the player joined.</returns>
    internal ValueTask<bool> SpectateAsync(Player viewer)
        => this.CanSpectate(viewer) ? room.TryAddSpectatorAsync(viewer) : ValueTask.FromResult(false);

    /// <summary>Stops accepting new stakes before combat starts.</summary>
    internal async ValueTask CloseAsync()
    {
        using var l = await this._lock.LockAsync().ConfigureAwait(false);
        book.Lock();
    }

    /// <summary>Extends the escrow lease while the game server is running the match.</summary>
    internal async ValueTask RenewAsync()
    {
        using var l = await this._lock.LockAsync().ConfigureAwait(false);
        var now = DateTime.UtcNow;
        if (this._escrowId == Guid.Empty || now >= this._leaseUntil || now.Add(RenewalMargin) < this._leaseUntil)
        {
            return;
        }

        var leaseUntil = now.Add(LeaseDuration);
        try
        {
            if (await DuelBetWallet.UpdateEscrowAsync(room.Requester.GameContext, this._escrowId, leaseUntil, null).ConfigureAwait(false))
            {
                this._leaseUntil = leaseUntil;
            }
        }
        catch (Exception ex)
        {
            room.Requester.Logger.LogWarning(ex, "Could not renew the escrow lease for duel {Match}", this.MatchId);
        }
    }

    /// <summary>Records claimable payouts or refunds for every staked account.</summary>
    /// <param name="winner">The winning fighter, or null to refund all stakes.</param>
    internal async ValueTask SettleAsync(DuelBetSide? winner)
    {
        using var l = await this._lock.LockAsync().ConfigureAwait(false);
        var payouts = book.Settle(winner);
        if (this._escrowId == Guid.Empty)
        {
            return;
        }

        if (!await DuelBetWallet.UpdateEscrowAsync(room.Requester.GameContext, this._escrowId, this._leaseUntil, payouts).ConfigureAwait(false))
        {
            room.Requester.Logger.LogWarning("Duel {Match} could not settle its escrow and its stakes are refundable after lease expiry.", this.MatchId);
            return;
        }

        this._escrowId = Guid.Empty;
    }

    private bool CanSpectate(Player viewer) => room.State == DuelState.DuelStarted && room.IsOpen
        && room.Area.SpectatorsGate is not null && viewer.DuelRoom is null && viewer.CurrentMiniGame is null
        && viewer.IsAlive && viewer.OpenedNpc is null && viewer.PlayerState.CurrentState == PlayerState.EnteredWorld;
}
