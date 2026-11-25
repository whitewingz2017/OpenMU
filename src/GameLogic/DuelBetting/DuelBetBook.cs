// <copyright file="DuelBetBook.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.DuelBetting;

/// <summary>An in-memory confirmed-ticket book. The caller must serialize mutations and persist escrow separately.</summary>
public sealed class DuelBetBook
{
    private readonly Guid _requester;
    private readonly Guid _opponent;
    private readonly Dictionary<DuelBetCurrency, (int Minimum, int Maximum)> _limits;
    private readonly int _feePercent;
    private readonly int _maximumPool;
    private readonly int _maximumTickets;
    private readonly List<DuelBetTicket> _tickets = [];
    private readonly IReadOnlyList<DuelBetTicket> _readOnlyTickets;
    private IReadOnlyList<DuelBetPayout>? _settlement;
    private DuelBetSide? _winner;
    private bool _locked;

    /// <summary>Initializes a new instance of the <see cref="DuelBetBook"/> class with captured rules.</summary>
    /// <param name="requester">The requester's account.</param>
    /// <param name="opponent">The opponent's account.</param>
    /// <param name="closesAtUtc">The UTC betting deadline.</param>
    /// <param name="settings">The settings to capture.</param>
    public DuelBetBook(Guid requester, Guid opponent, DateTime closesAtUtc, DuelBettingSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (requester == Guid.Empty || opponent == Guid.Empty || requester == opponent
            || closesAtUtc.Kind != DateTimeKind.Utc || !settings.IsValid())
        {
            throw new ArgumentException("Invalid duel participants, deadline, or betting settings.");
        }

        this._requester = requester;
        this._opponent = opponent;
        this.ClosesAtUtc = closesAtUtc;
        this._limits = settings.Limits.ToDictionary(pair => pair.Key, pair => (pair.Value.Minimum, pair.Value.Maximum));
        this._feePercent = settings.FeePercent;
        this._maximumPool = settings.MaximumPool;
        this._maximumTickets = settings.MaximumTicketsPerAccount;
        this._readOnlyTickets = this._tickets.AsReadOnly();
    }

    /// <summary>Gets the UTC betting deadline.</summary>
    public DateTime ClosesAtUtc { get; }

    /// <summary>Gets confirmed tickets without permitting outside mutation.</summary>
    public IReadOnlyList<DuelBetTicket> Tickets => this._readOnlyTickets;

    /// <summary>Accepts a confirmed spectator ticket; this method never debits a wallet.</summary>
    /// <param name="ticket">The confirmed escrow ticket.</param>
    /// <param name="utcNow">The current UTC time.</param>
    /// <returns>The acceptance or rejection reason.</returns>
    public DuelBetRejection Add(DuelBetTicket ticket, DateTime utcNow)
    {
        ArgumentNullException.ThrowIfNull(ticket);
        if (utcNow.Kind != DateTimeKind.Utc || ticket.Id == Guid.Empty || ticket.AccountId == Guid.Empty || !Enum.IsDefined(ticket.Side))
        {
            return DuelBetRejection.InvalidIdentity;
        }

        var admission = this.CheckAdmission(ticket, utcNow);
        if (admission != DuelBetRejection.None)
        {
            return admission;
        }

        var limits = this.CheckAccountAndPool(ticket);
        if (limits != DuelBetRejection.None)
        {
            return limits;
        }

        this._tickets.Add(ticket);
        return DuelBetRejection.None;
    }

    /// <summary>Locks betting at combat start, even when the countdown has not elapsed.</summary>
    public void Lock() => this._locked = true;

    /// <summary>Calculates settlement once. Repeated settlement cannot select a different winner.</summary>
    /// <param name="winner">The authoritative winner, or null for a refund.</param>
    /// <returns>The cached settlement instructions.</returns>
    public IReadOnlyList<DuelBetPayout> Settle(DuelBetSide? winner)
    {
        if (this._settlement is not null && this._winner != winner)
        {
            throw new InvalidOperationException("A settled match cannot change its result.");
        }

        this.Lock();
        this._winner = winner;
        return this._settlement ??= DuelBetSettlement.Calculate(this._tickets, winner, this._feePercent);
    }

    private DuelBetRejection CheckAdmission(DuelBetTicket ticket, DateTime utcNow)
    {
        if (this._locked || utcNow >= this.ClosesAtUtc)
        {
            return DuelBetRejection.Closed;
        }

        if (ticket.AccountId == this._requester || ticket.AccountId == this._opponent)
        {
            return DuelBetRejection.OwnMatch;
        }

        if (!this._limits.TryGetValue(ticket.Currency, out var limit))
        {
            return DuelBetRejection.CurrencyDisabled;
        }

        if (ticket.Amount < limit.Minimum || ticket.Amount > limit.Maximum)
        {
            return DuelBetRejection.InvalidAmount;
        }

        return this._tickets.Any(existing => existing.Id == ticket.Id) ? DuelBetRejection.DuplicateTicket : DuelBetRejection.None;
    }

    private DuelBetRejection CheckAccountAndPool(DuelBetTicket ticket)
    {
        var owned = this._tickets.Where(existing => existing.AccountId == ticket.AccountId).ToArray();
        if (owned.Any(existing => existing.Side != ticket.Side))
        {
            return DuelBetRejection.OppositeSide;
        }

        if (owned.Length >= this._maximumTickets || this._tickets.Where(existing => existing.Currency == ticket.Currency)
            .Sum(existing => (long)existing.Amount) + ticket.Amount > this._maximumPool)
        {
            return DuelBetRejection.LimitReached;
        }

        return DuelBetRejection.None;
    }
}
