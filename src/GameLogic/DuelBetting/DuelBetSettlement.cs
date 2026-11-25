// <copyright file="DuelBetSettlement.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.DuelBetting;

/// <summary>Computes deterministic integer settlements without exchanging currencies or creating units.</summary>
public static class DuelBetSettlement
{
    private const int Percent = 100;

    /// <summary>Settles each currency independently, refunding cancellations and one-sided pools.</summary>
    /// <param name="tickets">Confirmed tickets.</param>
    /// <param name="winner">The authoritative winner, or null when cancelled without a result.</param>
    /// <param name="feePercent">The fee captured when betting opened.</param>
    /// <returns>One payout instruction per ticket.</returns>
    public static IReadOnlyList<DuelBetPayout> Calculate(IReadOnlyList<DuelBetTicket> tickets, DuelBetSide? winner, int feePercent)
    {
        ArgumentNullException.ThrowIfNull(tickets);
        if (feePercent is < 0 or > Percent || (winner is { } side && !Enum.IsDefined(side)))
        {
            throw new ArgumentOutOfRangeException(nameof(feePercent));
        }

        if (tickets.Any(ticket => ticket is null || ticket.Id == Guid.Empty || ticket.AccountId == Guid.Empty
            || ticket.Amount <= 0 || !Enum.IsDefined(ticket.Currency) || !Enum.IsDefined(ticket.Side))
            || tickets.Select(ticket => ticket.Id).Distinct().Count() != tickets.Count)
        {
            throw new ArgumentException("Invalid or duplicated confirmed tickets.", nameof(tickets));
        }

        return Array.AsReadOnly(tickets.GroupBy(ticket => ticket.Currency).SelectMany(pool => SettlePool(pool.ToArray(), winner, feePercent)).ToArray());
    }

    private static IReadOnlyList<DuelBetPayout> SettlePool(DuelBetTicket[] pool, DuelBetSide? winner, int feePercent)
    {
        var total = pool.Sum(ticket => (long)ticket.Amount);
        if (total > int.MaxValue)
        {
            throw new ArgumentException("The currency pool exceeds the supported payout amount.", nameof(pool));
        }

        var winners = pool.Where(ticket => ticket.Side == winner).OrderBy(ticket => ticket.Id).ToArray();
        if (winner is null || winners.Length == 0 || winners.Length == pool.Length)
        {
            return pool.Select(ticket => new DuelBetPayout(ticket, ticket.Amount, true)).ToArray();
        }

        var distributable = total - (total * feePercent / Percent);
        var winningStake = winners.Sum(ticket => (long)ticket.Amount);
        var amounts = winners.ToDictionary(ticket => ticket.Id, ticket => distributable * ticket.Amount / winningStake);
        var remainder = distributable - amounts.Values.Sum();

        // Largest fractional shares receive the remaining whole units; ticket ID breaks exact ties.
        foreach (var ticket in winners.OrderByDescending(ticket => (distributable * ticket.Amount) % winningStake)
            .ThenBy(ticket => ticket.Id).Take((int)remainder))
        {
            amounts[ticket.Id]++;
        }

        return pool.Select(ticket => new DuelBetPayout(ticket, amounts.TryGetValue(ticket.Id, out var amount) ? (int)amount : 0, false)).ToArray();
    }
}
