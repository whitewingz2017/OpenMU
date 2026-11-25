// <copyright file="DuelBettingTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.DuelBetting;

/// <summary>Checks exact pool settlement and spectator acceptance without moving real funds.</summary>
[TestFixture]
public class DuelBettingTests
{
    private static readonly DateTime Now = new(2026, 11, 25, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>Defaults offer the requested currencies and the feature is disabled by default.</summary>
    [Test]
    public void ConfigurationIsOptIn()
    {
        var feature = new DuelBettingFeaturePlugIn();
        Assert.That(feature, Is.InstanceOf<IDisabledByDefault>());
        var settings = (DuelBettingSettings)feature.CreateDefaultConfig();
        Assert.That(settings.IsValid(), Is.True);
        Assert.That(settings.FeePercent, Is.Zero);
        Assert.That(settings.Limits.Keys, Is.EquivalentTo(Enum.GetValues<DuelBetCurrency>()));
        settings.Limits[DuelBetCurrency.Bless].Minimum = 251;
        Assert.That(settings.IsValid(), Is.False);
    }

    /// <summary>Pool payouts include the stake and remain proportional after the configured fee.</summary>
    /// <param name="fee">The fee percentage.</param>
    /// <param name="first">The first payout.</param>
    /// <param name="second">The second payout.</param>
    [TestCase(0, 20, 60)]
    [TestCase(10, 18, 54)]
    public void ProportionalPayout(int fee, int first, int second)
    {
        var tickets = new[] { Ticket(1, 10), Ticket(2, 30), Ticket(3, 40, DuelBetSide.Opponent) };
        var result = DuelBetSettlement.Calculate(tickets, DuelBetSide.Requester, fee);
        Assert.That(result.Select(payout => payout.Amount), Is.EqualTo(new[] { first, second, 0 }));
        Assert.That(result.All(payout => !payout.Refund), Is.True);
    }

    /// <summary>Every remainder unit is allocated once and tie-breaking is stable across input order.</summary>
    [Test]
    public void IntegerRemaindersAreConserved()
    {
        var tickets = new[] { Ticket(1, 1), Ticket(2, 1), Ticket(3, 1), Ticket(4, 2, DuelBetSide.Opponent) };
        var result = DuelBetSettlement.Calculate(tickets, DuelBetSide.Requester, 0);
        Assert.That(result.Select(payout => payout.Amount), Is.EqualTo(new[] { 2, 2, 1, 0 }));
        var reversed = DuelBetSettlement.Calculate(tickets.Reverse().ToArray(), DuelBetSide.Requester, 0);
        Assert.That(reversed.OrderBy(payout => payout.Ticket.Id).Select(payout => payout.Amount), Is.EqualTo(new[] { 2, 2, 1, 0 }));
        Assert.That(result.Sum(payout => payout.Amount), Is.EqualTo(5));
    }

    /// <summary>Currencies are independent, and uncontested pools are refunded without a fee.</summary>
    [Test]
    public void CurrencyPoolsNeverMix()
    {
        var tickets = new[]
        {
            Ticket(1, 10), Ticket(2, 20, DuelBetSide.Opponent),
            Ticket(3, 7, currency: DuelBetCurrency.Soul),
            Ticket(4, 100, currency: DuelBetCurrency.WCoinC),
            Ticket(5, 100, DuelBetSide.Opponent, DuelBetCurrency.WCoinC),
            Ticket(6, 9, DuelBetSide.Opponent, DuelBetCurrency.Life),
        };
        var payouts = DuelBetSettlement.Calculate(tickets, DuelBetSide.Requester, 0).ToDictionary(payout => payout.Ticket.Id);
        Assert.That(payouts[tickets[0].Id].Amount, Is.EqualTo(30));
        Assert.That(payouts[tickets[2].Id], Is.EqualTo(new DuelBetPayout(tickets[2], 7, true)));
        Assert.That(payouts[tickets[3].Id].Amount, Is.EqualTo(200));
        Assert.That(payouts[tickets[5].Id], Is.EqualTo(new DuelBetPayout(tickets[5], 9, true)));
    }

    /// <summary>Cancelled matches return every stake, even when a fee was configured.</summary>
    [Test]
    public void CancellationRefundsEverything()
    {
        var tickets = new[] { Ticket(1, 20), Ticket(2, 20, DuelBetSide.Opponent) };
        var result = DuelBetSettlement.Calculate(tickets, null, 25);
        Assert.That(result.All(payout => payout.Refund && payout.Amount == payout.Ticket.Amount), Is.True);
    }

    /// <summary>Large integer products cannot overflow or generate extra currency.</summary>
    [Test]
    public void LargePoolHasExactPayout()
    {
        var tickets = new[] { Ticket(1, int.MaxValue - 1), Ticket(2, 1, DuelBetSide.Opponent) };
        var result = DuelBetSettlement.Calculate(tickets, DuelBetSide.Requester, 0);
        Assert.That(result[0].Amount, Is.EqualTo(int.MaxValue));
        Assert.Throws<ArgumentException>(() => DuelBetSettlement.Calculate([Ticket(1, int.MaxValue), Ticket(2, 1)], DuelBetSide.Requester, 0));
        Assert.Throws<ArgumentException>(() => DuelBetSettlement.Calculate([tickets[0], tickets[0]], DuelBetSide.Requester, 0));
    }

    /// <summary>All four currencies reject bets by either fighter's account.</summary>
    /// <param name="currency">The tested currency.</param>
    [TestCase(DuelBetCurrency.Bless)]
    [TestCase(DuelBetCurrency.Soul)]
    [TestCase(DuelBetCurrency.Life)]
    [TestCase(DuelBetCurrency.WCoinC)]
    public void DuelistsCannotBet(DuelBetCurrency currency)
    {
        var requester = Guid.NewGuid();
        var book = new DuelBetBook(requester, Guid.NewGuid(), Now.AddSeconds(30), new());
        Assert.That(book.Add(new(Guid.NewGuid(), requester, currency, DuelBetSide.Requester, 1), Now), Is.EqualTo(DuelBetRejection.OwnMatch));
    }

    /// <summary>Acceptance respects deadlines, captured limits, duplicates and a single chosen side.</summary>
    [Test]
    public void AcceptanceRulesAreEnforced()
    {
        var settings = new DuelBettingSettings();
        settings.Limits.Remove(DuelBetCurrency.Soul);
        var book = new DuelBetBook(Guid.NewGuid(), Guid.NewGuid(), Now.AddSeconds(30), settings);
        var ticket = Ticket(1, 10);
        Assert.That(book.Add(ticket, Now), Is.EqualTo(DuelBetRejection.None));
        Assert.That(book.Add(ticket, Now), Is.EqualTo(DuelBetRejection.DuplicateTicket));
        Assert.That(book.Add(ticket with { Id = Guid.NewGuid(), Side = DuelBetSide.Opponent }, Now), Is.EqualTo(DuelBetRejection.OppositeSide));
        Assert.That(book.Add(Ticket(2, 1, currency: DuelBetCurrency.Soul), Now), Is.EqualTo(DuelBetRejection.CurrencyDisabled));
        Assert.That(book.Add(Ticket(3, 0), Now), Is.EqualTo(DuelBetRejection.InvalidAmount));
        Assert.That(book.Add(Ticket(4, 1), Now.AddSeconds(30)), Is.EqualTo(DuelBetRejection.Closed));
        book.Lock();
        Assert.That(book.Add(Ticket(5, 1), Now), Is.EqualTo(DuelBetRejection.Closed));
    }

    /// <summary>Account and currency caps reject the ticket without mutating the book.</summary>
    [Test]
    public void LimitsRejectWithoutMutation()
    {
        var settings = new DuelBettingSettings { MaximumTicketsPerAccount = 1, MaximumPool = 30 };
        foreach (var limit in settings.Limits.Values)
        {
            limit.Maximum = 30;
        }
        var book = new DuelBetBook(Guid.NewGuid(), Guid.NewGuid(), Now.AddSeconds(30), settings);
        var ticket = Ticket(1, 20);
        Assert.That(book.Add(ticket, Now), Is.EqualTo(DuelBetRejection.None));
        Assert.That(book.Add(ticket with { Id = Guid.NewGuid() }, Now), Is.EqualTo(DuelBetRejection.LimitReached));
        Assert.That(book.Add(Ticket(2, 20), Now), Is.EqualTo(DuelBetRejection.LimitReached));
        Assert.That(book.Tickets, Has.Count.EqualTo(1));
    }

    /// <summary>Live configuration changes cannot rewrite accepted-match rules or settled results.</summary>
    [Test]
    public void SettlementIsStable()
    {
        var settings = new DuelBettingSettings();
        var book = new DuelBetBook(Guid.NewGuid(), Guid.NewGuid(), Now.AddSeconds(30), settings);
        settings.FeePercent = 100;
        settings.Limits.Clear();
        Assert.That(book.Add(Ticket(1, 20), Now), Is.EqualTo(DuelBetRejection.None));
        Assert.That(book.Add(Ticket(2, 20, DuelBetSide.Opponent), Now), Is.EqualTo(DuelBetRejection.None));
        var first = book.Settle(DuelBetSide.Requester);
        Assert.That(first[0].Amount, Is.EqualTo(40));
        Assert.That(book.Settle(DuelBetSide.Requester), Is.SameAs(first));
        Assert.Throws<InvalidOperationException>(() => book.Settle(DuelBetSide.Opponent));
        Assert.That(book.Add(Ticket(3, 1), Now), Is.EqualTo(DuelBetRejection.Closed));
    }

    private static DuelBetTicket Ticket(int id, int amount, DuelBetSide side = DuelBetSide.Requester, DuelBetCurrency currency = DuelBetCurrency.Bless)
        => new(new Guid(id, 0, 0, new byte[8]), new Guid(id + 100, 0, 0, new byte[8]), currency, side, amount);
}
