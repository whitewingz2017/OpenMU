// <copyright file="DuelWager.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.DuelBetting;

using System.Threading;
using MUnique.OpenMU.Persistence;

/// <summary>The accepted terms and escrow of the two fighters' equal WCoinC stakes.</summary>
/// <param name="amount">The agreed stake per fighter.</param>
/// <param name="settings">The betting settings used to offer the wager.</param>
internal sealed class DuelWager(int amount, DuelBettingSettings settings)
{
    /// <summary>Time for which an escrow remains active without renewal.</summary>
    internal static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(2);

    private static readonly TimeSpan FundingLockTimeout = TimeSpan.FromSeconds(1);

    /// <summary>Gets the unique identifier that the opponent must acknowledge.</summary>
    internal Guid OfferId { get; } = Guid.NewGuid();

    /// <summary>Gets the WCoinC stake per fighter.</summary>
    internal int Amount { get; } = amount;

    /// <summary>Gets the betting settings used to offer the wager.</summary>
    internal DuelBettingSettings Settings { get; } = settings;

    /// <summary>Gets the persisted escrow identity after both fighters paid.</summary>
    internal Guid EscrowId { get; private set; }

    /// <summary>Gets the active escrow's initial expiry.</summary>
    internal DateTime LeaseUntil { get; private set; }

    /// <summary>Gets the two funded stakes.</summary>
    internal IReadOnlyList<DuelBetTicket> Stakes { get; private set; } = [];

    /// <summary>Charges both fighters under the response's room lock.</summary>
    /// <param name="requester">The challenger.</param>
    /// <param name="opponent">The invited fighter.</param>
    /// <returns>The escrow funding result.</returns>
    internal async ValueTask<DuelBetRejection> FundAsync(Player requester, Player opponent)
    {
        if (requester.Account?.GetId() is not { } first || opponent.Account?.GetId() is not { } second
            || first == Guid.Empty || second == Guid.Empty || first == second)
        {
            return DuelBetRejection.InvalidIdentity;
        }

        DuelBetTicket[] stakes =
        [
            new(Guid.NewGuid(), first, DuelBetCurrency.WCoinC, DuelBetSide.Requester, this.Amount),
            new(Guid.NewGuid(), second, DuelBetCurrency.WCoinC, DuelBetSide.Opponent, this.Amount),
        ];
        var leaseUntil = DateTime.UtcNow.Add(LeaseDuration);

        // The response handler owns the opponent's lock. Bound the wait for the other account, as in store purchases.
        using var timeout = new CancellationTokenSource(FundingLockTimeout);
        var escrowId = await opponent.RunPersistenceExclusiveAsync(() => requester.RunPersistenceExclusiveAsync(
            () => DuelBetWallet.FundWagerAsync(requester, opponent, stakes, leaseUntil), timeout.Token)).ConfigureAwait(false);
        if (escrowId is null)
        {
            return DuelBetRejection.TryAgain;
        }

        if (escrowId == Guid.Empty)
        {
            return DuelBetRejection.NotEnoughWCoins;
        }

        this.Stakes = stakes;
        this.EscrowId = escrowId.Value;
        this.LeaseUntil = leaseUntil;
        return DuelBetRejection.None;
    }
}
