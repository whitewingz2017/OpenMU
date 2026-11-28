// <copyright file="DuelBetWallet.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.DuelBetting;

using System.Text.Json;
using Microsoft.Extensions.Logging;
using MUnique.OpenMU.Persistence;

/// <summary>Moves balances and escrow together in a player persistence transaction.</summary>
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
        var coins = account.WCoinC;
        Dictionary<string, int>? balances = null;
        if (ticket.Currency == DuelBetCurrency.WCoinC)
        {
            if (coins < ticket.Amount)
            {
                return Guid.Empty;
            }

            coins -= ticket.Amount;
        }
        else
        {
            balances = ReadBalances(account);
            var key = BankKey(ticket.Currency);
            if (balances.GetValueOrDefault(key) < ticket.Amount)
            {
                return Guid.Empty;
            }

            balances[key] -= ticket.Amount;
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
        return await SaveBalanceAsync(account, context, balances, coins).ConfigureAwait(false) ? escrow.GetId() : null;
    }

    /// <summary>Atomically debits both fighters and persists their stakes while both persistence locks are held.</summary>
    /// <param name="requester">The challenger.</param>
    /// <param name="opponent">The invited fighter.</param>
    /// <param name="tickets">The matching stakes in challenger and opponent order.</param>
    /// <param name="leaseUntil">The initial escrow expiry.</param>
    /// <returns>The escrow ID, an empty ID for insufficient funds, or null if the save failed.</returns>
    internal static async ValueTask<Guid?> FundWagerAsync(Player requester, Player opponent, IReadOnlyList<DuelBetTicket> tickets, DateTime leaseUntil)
    {
        if (requester.Account is not { } first || opponent.Account is not { } second
            || first.GetId() != tickets[0].AccountId || second.GetId() != tickets[1].AccountId
            || requester.PlayerState.CurrentState != PlayerState.EnteredWorld || opponent.PlayerState.CurrentState != PlayerState.EnteredWorld)
        {
            return null;
        }

        var amount = tickets[0].Amount;
        var firstBalance = first.WCoinC;
        var secondBalance = second.WCoinC;
        if (firstBalance < amount || secondBalance < amount)
        {
            return Guid.Empty;
        }

        using var context = CreateContext(requester.GameContext);
        context.Attach(first);
        context.Attach(second);
        var escrow = context.CreateNew<DuelBetEscrow>();
        escrow.Entries = JsonSerializer.Serialize(tickets);
        escrow.LeaseUntil = leaseUntil;
        escrow.Revision = Guid.NewGuid();
        var saved = false;
        try
        {
            first.WCoinC = firstBalance - amount;
            second.WCoinC = secondBalance - amount;
            saved = await context.SaveChangesAsync().ConfigureAwait(false);
            return saved ? escrow.GetId() : null;
        }
        finally
        {
            if (!saved)
            {
                first.WCoinC = firstBalance;
                second.WCoinC = secondBalance;
            }
        }
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
    /// <param name="onlyEscrow">An optional recently settled match to credit automatically.</param>
    /// <returns>The total credited, or -1 if the transaction failed.</returns>
    internal static async ValueTask<long> ClaimAsync(Player player, Guid? onlyEscrow = null)
    {
        var account = player.Account!;
        var accountId = account.GetId();
        var balances = ReadBalances(account);
        var coins = account.WCoinC;
        using var context = CreateContext(player.GameContext);
        var escrows = onlyEscrow is { } id
            ? await context.GetByIdAsync<DuelBetEscrow>(id).ConfigureAwait(false) is { Settled: true } entry ? [entry] : []
            : await context.GetAsync<DuelBetEscrow>().ConfigureAwait(false);
        var now = DateTime.UtcNow;
        long credited = 0;
        foreach (var escrow in escrows.Where(entry => entry.Settled || entry.LeaseUntil <= now))
        {
            var entries = ReadEntries(escrow);
            var amount = CreditPayouts(balances, entries, accountId, ref coins);
            if (amount > 0)
            {
                credited += amount;
                await UpdateEntriesAsync(context, escrow, entries).ConfigureAwait(false);
            }
        }

        return credited == 0 || await SaveBalanceAsync(account, context, balances, coins).ConfigureAwait(false) ? credited : -1;
    }

    /// <summary>Credits connected recipients, leaving outstanding credits in the settled escrow.</summary>
    /// <param name="context">The channel hosting the match.</param>
    /// <param name="id">The settled escrow.</param>
    /// <param name="payouts">The payouts and refunds to deliver.</param>
    internal static async ValueTask CreditOnlineAsync(IGameContext context, Guid id, IReadOnlyList<DuelBetPayout> payouts)
    {
        var accounts = payouts.Where(payout => payout.Amount > 0).Select(payout => payout.Ticket.AccountId).ToHashSet();
        foreach (var player in await context.GetPlayersAsync().ConfigureAwait(false))
        {
            if (player.Account?.GetId() is not { } account || !accounts.Contains(account))
            {
                continue;
            }

            try
            {
                var credited = await player.RunPersistenceExclusiveAsync(() => player.Account?.GetId() == account
                    ? ClaimAsync(player, id) : ValueTask.FromResult(0L)).ConfigureAwait(false);
                if (credited > 0)
                {
                    await player.ShowLocalizedBlueMessageAsync("DuelBetting_CoinsCredited", credited).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                player.Logger.LogWarning(ex, "Automatic credit for duel escrow {Escrow} failed; it remains claimable.", id);
            }
        }
    }

    private static long CreditPayouts(Dictionary<string, int> balances, List<DuelBetTicket> entries, Guid accountId, ref int coins)
    {
        long credited = 0;
        for (var i = entries.Count - 1; i >= 0; i--)
        {
            var entry = entries[i];
            if (entry.AccountId != accountId)
            {
                continue;
            }

            var key = entry.Currency == DuelBetCurrency.WCoinC ? null : BankKey(entry.Currency);
            var balance = key is null ? coins : balances.GetValueOrDefault(key);
            var credit = (int)Math.Min(entry.Amount, int.MaxValue - (long)balance);
            if (credit <= 0)
            {
                continue;
            }

            credited += credit;
            if (key is null)
            {
                coins = balance + credit;
            }
            else
            {
                balances[key] = balance + credit;
            }

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

    private static async ValueTask<bool> SaveBalanceAsync(Account account, IContext context, Dictionary<string, int>? balances, int coins)
    {
        var previous = account.JewelBankBalances;
        var previousCoins = account.WCoinC;
        var saved = false;
        try
        {
            context.Attach(account);
            if (balances is not null)
            {
                account.JewelBankBalances = JsonSerializer.Serialize(balances);
            }

            account.WCoinC = coins;
            saved = await context.SaveChangesAsync().ConfigureAwait(false);
            return saved;
        }
        finally
        {
            if (!saved)
            {
                account.JewelBankBalances = previous;
                account.WCoinC = previousCoins;
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
