// <copyright file="DuelBetWallet.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.DuelBetting;

using System.Text.Json;
using MUnique.OpenMU.Persistence;

/// <summary>Moves Jewel Bank balances and escrow together in a player persistence transaction.</summary>
internal static class DuelBetWallet
{
    /// <summary>Debits a bank balance and adds the stake to escrow in one save.</summary>
    /// <param name="player">The bettor, who holds the player persistence lock.</param>
    /// <param name="escrowId">The existing escrow, or an empty identifier for the first ticket.</param>
    /// <param name="ticket">The admitted stake.</param>
    /// <param name="leaseUntil">The updated expiry of the escrow lease.</param>
    /// <returns>The saved escrow ID, an empty ID for insufficient funds, or null if the transaction failed.</returns>
    internal static async ValueTask<Guid?> DebitAsync(Player player, Guid escrowId, DuelBetTicket ticket, DateTime leaseUntil)
    {
        var account = player.Account!;
        var balances = ReadBalances(account);
        var key = BankKey(ticket.Currency);
        if (balances.GetValueOrDefault(key) < ticket.Amount)
        {
            return Guid.Empty;
        }

        using var context = CreateContext(player.GameContext);
        var escrow = escrowId == Guid.Empty
            ? context.CreateNew<DuelBetEscrow>()
            : await context.GetByIdAsync<DuelBetEscrow>(escrowId).ConfigureAwait(false);
        if (escrow is null || escrow.Settled || (escrowId != Guid.Empty && escrow.LeaseUntil <= DateTime.UtcNow))
        {
            return null;
        }

        var entries = ReadEntries(escrow);
        entries.Add(ticket);
        escrow.Entries = JsonSerializer.Serialize(entries);
        escrow.LeaseUntil = leaseUntil;
        escrow.Revision = Guid.NewGuid();
        balances[key] -= ticket.Amount;
        return await SaveBalanceAsync(player, context, balances).ConfigureAwait(false) ? escrow.GetId() : null;
    }

    /// <summary>Renews a live lease or replaces escrowed stakes with resolved payouts.</summary>
    /// <param name="gameContext">The context hosting the duel.</param>
    /// <param name="id">The escrow ID.</param>
    /// <param name="leaseUntil">The new expiry if renewing.</param>
    /// <param name="payouts">The final payouts, or null to renew the lease.</param>
    /// <returns>Whether the update was saved.</returns>
    internal static async ValueTask<bool> UpdateEscrowAsync(IGameContext gameContext, Guid id, DateTime leaseUntil, IReadOnlyList<DuelBetPayout>? payouts)
    {
        using var context = CreateContext(gameContext);
        var escrow = await context.GetByIdAsync<DuelBetEscrow>(id).ConfigureAwait(false);
        if (escrow is not { Settled: false } || escrow.LeaseUntil <= DateTime.UtcNow)
        {
            return false;
        }

        if (payouts is not null)
        {
            var entries = payouts.Where(payout => payout.Amount > 0)
                .Select(payout => payout.Ticket with { Amount = payout.Amount }).ToList();
            await UpdateEntriesAsync(context, escrow, entries).ConfigureAwait(false);
        }
        else
        {
            escrow.LeaseUntil = leaseUntil;
            escrow.Revision = Guid.NewGuid();
        }

        return await context.SaveChangesAsync().ConfigureAwait(false);
    }

    /// <summary>Claims settled payouts and refunds abandoned stakes for the player's account.</summary>
    /// <param name="player">The bettor, who holds the player persistence lock.</param>
    /// <returns>The total credited, or -1 if the transaction failed.</returns>
    internal static async ValueTask<long> ClaimAsync(Player player)
    {
        var account = player.Account!;
        var accountId = account.GetId();
        var balances = ReadBalances(account);
        using var context = CreateContext(player.GameContext);
        var escrows = await context.GetAsync<DuelBetEscrow>().ConfigureAwait(false);
        var now = DateTime.UtcNow;
        long credited = 0;
        foreach (var escrow in escrows.Where(entry => entry.Settled || entry.LeaseUntil <= now))
        {
            var entries = ReadEntries(escrow);
            var amount = CreditPayouts(balances, entries, accountId);
            if (amount > 0)
            {
                credited += amount;
                await UpdateEntriesAsync(context, escrow, entries).ConfigureAwait(false);
            }
        }

        return credited == 0 || await SaveBalanceAsync(player, context, balances).ConfigureAwait(false) ? credited : -1;
    }

    private static long CreditPayouts(Dictionary<string, int> balances, List<DuelBetTicket> entries, Guid accountId)
    {
        long credited = 0;
        for (var i = entries.Count - 1; i >= 0; i--)
        {
            var entry = entries[i];
            if (entry.AccountId != accountId)
            {
                continue;
            }

            var key = BankKey(entry.Currency);
            var balance = balances.GetValueOrDefault(key);
            var credit = (int)Math.Min(entry.Amount, int.MaxValue - (long)balance);
            if (credit <= 0)
            {
                continue;
            }

            credited += credit;
            balances[key] = balance + credit;
            if (credit == entry.Amount)
            {
                entries.RemoveAt(i);
            }
            else
            {
                entries[i] = entry with { Amount = entry.Amount - credit };
            }
        }

        return credited;
    }

    private static async ValueTask UpdateEntriesAsync(IContext context, DuelBetEscrow escrow, List<DuelBetTicket> entries)
    {
        if (entries.Count == 0)
        {
            if (!await context.DeleteAsync(escrow).ConfigureAwait(false))
            {
                throw new InvalidOperationException("Could not remove an empty duel escrow.");
            }

            return;
        }

        escrow.Entries = JsonSerializer.Serialize(entries);
        escrow.Settled = true;
        escrow.Revision = Guid.NewGuid();
    }

    private static async ValueTask<bool> SaveBalanceAsync(Player player, IContext context, Dictionary<string, int> balances)
    {
        var account = player.Account!;
        var previous = account.JewelBankBalances;
        var saved = false;
        try
        {
            context.Attach(account);
            account.JewelBankBalances = JsonSerializer.Serialize(balances);
            saved = await context.SaveChangesAsync().ConfigureAwait(false);
            return saved;
        }
        finally
        {
            if (!saved)
            {
                account.JewelBankBalances = previous;
            }
        }
    }

    private static Dictionary<string, int> ReadBalances(Account account)
        => JsonSerializer.Deserialize<Dictionary<string, int>>(string.IsNullOrWhiteSpace(account.JewelBankBalances) ? "{}" : account.JewelBankBalances)
            ?? throw new InvalidOperationException("Invalid Jewel Bank balances.");

    private static List<DuelBetTicket> ReadEntries(DuelBetEscrow escrow)
        => JsonSerializer.Deserialize<List<DuelBetTicket>>(escrow.Entries)
            ?? throw new InvalidOperationException("Invalid duel escrow.");

    private static string BankKey(DuelBetCurrency currency) => currency switch
    {
        DuelBetCurrency.Bless => "7181:0",
        DuelBetCurrency.Soul => "7182:0",
        DuelBetCurrency.Life => "7184:0",
        _ => throw new ArgumentOutOfRangeException(nameof(currency)),
    };

    private static IPlayerContext CreateContext(IGameContext context)
        => context.PersistenceContextProvider.CreateNewPlayerContext(context.Configuration);
}
