// <copyright file="DuelBettingTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using System.Text.Json;
using System.Threading;
using Moq;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.DuelBetting;
using MUnique.OpenMU.GameLogic.Views;
using MUnique.OpenMU.GameServer.MessageHandler.Duel;
using MUnique.OpenMU.GameServer.RemoteView.Duel;
using MUnique.OpenMU.Interfaces;
using MUnique.OpenMU.Network.Packets.ClientToServer;
using MUnique.OpenMU.Persistence;
using BasicModel = MUnique.OpenMU.Persistence.BasicModel;
using DuelInvitationPacket = MUnique.OpenMU.Network.Packets.ServerToClient.DuelStartRequest;

/// <summary>Checks exact pool settlement, remote betting and escrow claims.</summary>
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
        Assert.That(settings.BettingSeconds, Is.EqualTo(30));
        Assert.That(settings.Limits.Keys, Is.EquivalentTo(new[] { DuelBetCurrency.Bless, DuelBetCurrency.Soul, DuelBetCurrency.Life }));
        Assert.That(settings.WCoinLimit.Maximum, Is.EqualTo(250));
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
            Ticket(4, 100, currency: DuelBetCurrency.Life),
            Ticket(5, 100, DuelBetSide.Opponent, DuelBetCurrency.Life),
        };
        var payouts = DuelBetSettlement.Calculate(tickets, DuelBetSide.Requester, 0).ToDictionary(payout => payout.Ticket.Id);
        Assert.That(payouts[tickets[0].Id].Amount, Is.EqualTo(30));
        Assert.That(payouts[tickets[2].Id], Is.EqualTo(new DuelBetPayout(tickets[2], 7, true)));
        Assert.That(payouts[tickets[3].Id].Amount, Is.EqualTo(200));
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

    /// <summary>All three currencies reject bets by either fighter's account.</summary>
    /// <param name="currency">The tested currency.</param>
    [TestCase(DuelBetCurrency.Bless)]
    [TestCase(DuelBetCurrency.Soul)]
    [TestCase(DuelBetCurrency.Life)]
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

    /// <summary>The retired fourth currency cannot be configured, accepted or settled.</summary>
    [Test]
    public void RetiredCurrencyIsRejected()
    {
        const DuelBetCurrency retiredCurrency = (DuelBetCurrency)3;
        var settings = new DuelBettingSettings();
        var book = new DuelBetBook(Guid.NewGuid(), Guid.NewGuid(), Now.AddSeconds(30), settings);
        var ticket = Ticket(1, 1, currency: retiredCurrency);
        Assert.That(book.Add(ticket, Now), Is.EqualTo(DuelBetRejection.CurrencyDisabled));
        Assert.Throws<ArgumentException>(() => DuelBetSettlement.Calculate([ticket], null, 0));
        settings.Limits[retiredCurrency] = new();
        Assert.That(settings.IsValid(), Is.False);
    }

    /// <summary>Only an opted-in accepted duel appears on the public board and sends a channel notice.</summary>
    /// <param name="allowBetting">The challenger's choice.</param>
    [TestCase(true)]
    [TestCase(false)]
    public async Task AnnouncementFollowsChallengerChoiceAsync(bool allowBetting)
    {
        var requester = await CreateBettorAsync().ConfigureAwait(false);
        var context = (GameContext)requester.GameContext;
        var opponent = await CreateBettorAsync(context).ConfigureAwait(false);
        var bettor = await CreateBettorAsync(context).ConfigureAwait(false);
        requester.SelectedCharacter!.Name = "Alice";
        opponent.SelectedCharacter!.Name = "Bob";
        requester.Id = 1;
        opponent.Id = 2;
        var area = new DuelArea
        {
            FirstPlayerGate = new ExitGate { Map = requester.CurrentMap?.Definition },
            SecondPlayerGate = new ExitGate { Map = opponent.CurrentMap?.Definition },
        };
        var config = new Mock<DuelConfiguration>();
        config.SetupGet(value => value.DuelAreas).Returns([area]);
        context.DuelRoomManager = new DuelRoomManager(config.Object);
        context.FeaturePlugIns.AddPlugIn(new DuelBettingFeaturePlugIn(), true);
        await context.AddPlayerAsync(bettor).ConfigureAwait(false);

        var notice = new TaskCompletionSource<string>();
        var messages = Mock.Get(bettor.ViewPlugIns.GetPlugIn<IShowMessagePlugIn>()!);
        messages.Setup(view => view.ShowMessageAsync(It.IsAny<string>(), MessageType.BlueNormal))
            .Callback((string message, MessageType _) => notice.TrySetResult(message))
            .Returns(ValueTask.CompletedTask);
        var room = await context.DuelRoomManager.GetFreeDuelRoomAsync(requester, opponent, allowBetting: allowBetting).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Could not allocate the test duel.");
        Assert.That(await context.DuelRoomManager.GetFreeDuelRoomAsync(bettor, opponent, allowBetting: !allowBetting).ConfigureAwait(false), Is.Null);
        var run = room.RunDuelAsync();
        try
        {
            var board = await context.DuelRoomManager.GetPublicMatchesAsync(bettor).ConfigureAwait(false);
            if (allowBetting)
            {
                var announcement = await notice.Task.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
                Assert.That(announcement, Does.Contain("Alice vs. Bob starts in 30s"));
                Assert.That(board.Single().CanBet, Is.True);
                Assert.That(requester.IsAttackBlockedBySafezone(opponent), Is.True);
            }
            else
            {
                Assert.That(board, Is.Empty);
                Assert.That(notice.Task.IsCompleted, Is.False);
            }
        }
        finally
        {
            await room.CancelDuelAsync().ConfigureAwait(false);
            await run.ConfigureAwait(false);
        }
    }

    /// <summary>Both clients can answer the reserved challenger while outside their view.</summary>
    /// <param name="windowsClient">Whether the reply serializes the challenger ID in big-endian order.</param>
    /// <param name="accepted">Whether the opponent accepts the invitation.</param>
    [TestCase(true, true)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(false, false)]
    public async Task DuelReplyMatchesInvitationAsync(bool windowsClient, bool accepted)
    {
        var requester = await CreateBettorAsync().ConfigureAwait(false);
        var context = (GameContext)requester.GameContext;
        var opponent = await CreateBettorAsync(context).ConfigureAwait(false);
        requester.SelectedCharacter!.Name = "Alice";
        opponent.SelectedCharacter!.Name = "Bob";
        requester.Id = 0x0234;
        opponent.Id = 0x0235;
        var map = requester.CurrentMap!;
        var area = new DuelArea
        {
            FirstPlayerGate = new ExitGate { Map = map.Definition },
            SecondPlayerGate = new ExitGate { Map = map.Definition },
        };
        var config = new Mock<DuelConfiguration>();
        config.SetupGet(value => value.DuelAreas).Returns([area]);
        context.Configuration.DuelConfiguration = config.Object;
        context.DuelRoomManager = new DuelRoomManager(config.Object);
        context.FeaturePlugIns.AddPlugIn(new DuelBettingFeaturePlugIn { Configuration = new DuelBettingSettings { BettingSeconds = 5 } }, true);
        var room = await context.DuelRoomManager.GetFreeDuelRoomAsync(requester, opponent, allowBetting: true).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Could not allocate the test duel.");
        await opponent.RemoveObserverAsync(requester).ConfigureAwait(false);
        Assert.That(await opponent.GetObservingPlayerWithIdAsync(requester.Id).ConfigureAwait(false), Is.Null);
        var packet = CreateDuelReply(requester, windowsClient, accepted);
        var handler = new DuelStartResponseHandlerPlugIn();
        await handler.HandlePacketAsync(requester, packet).ConfigureAwait(false);
        var wrongName = CreateDuelReply(requester, windowsClient, true);
        _ = new DuelStartResponse(wrongName) { PlayerName = "Different" };
        await handler.HandlePacketAsync(opponent, wrongName).ConfigureAwait(false);
        var wrongId = CreateDuelReply(requester, windowsClient, true);
        _ = new DuelStartResponse(wrongId) { PlayerId = opponent.Id };
        await handler.HandlePacketAsync(opponent, wrongId).ConfigureAwait(false);
        Assert.That(room.State, Is.EqualTo(DuelState.DuelRequested));

        await handler.HandlePacketAsync(opponent, packet).ConfigureAwait(false);
        if (!accepted)
        {
            await handler.HandlePacketAsync(opponent, CreateDuelReply(requester, windowsClient, true)).ConfigureAwait(false);
            Assert.That(room.State, Is.EqualTo(DuelState.DuelRefused));
            Assert.That(requester.DuelRoom, Is.Null);
            Assert.That(opponent.DuelRoom, Is.Null);
            return;
        }

        try
        {
            await handler.HandlePacketAsync(opponent, packet).ConfigureAwait(false);
            Assert.That(room.State, Is.EqualTo(DuelState.DuelAccepted));
            Assert.That(requester.CurrentMap, Is.Null);
            Assert.That(opponent.CurrentMap, Is.Null);
            await requester.ClientReadyAfterMapChangeAsync().ConfigureAwait(false);
            Assert.That(room.Betting, Is.Null);
            await opponent.ClientReadyAfterMapChangeAsync().ConfigureAwait(false);
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            while (room.Betting is null)
            {
                await Task.Delay(20, timeout.Token).ConfigureAwait(false);
            }

            Assert.That(room.CanTakeBets, Is.True);
            while (room.State == DuelState.DuelAccepted)
            {
                await Task.Delay(20, timeout.Token).ConfigureAwait(false);
            }

            Assert.That(room.State, Is.EqualTo(DuelState.DuelStarted));
        }
        finally
        {
            await room.CancelDuelAsync().ConfigureAwait(false);
        }
    }

    /// <summary>A remote player can stake through the window, retry, and claim exactly once.</summary>
    [Test]
    public async Task RemoteStakeAndPayoutAreConservedAsync()
    {
        var requester = await CreateBettorAsync().ConfigureAwait(false);
        var opponent = await CreateBettorAsync(requester.GameContext).ConfigureAwait(false);
        var player = await CreateBettorAsync(requester.GameContext).ConfigureAwait(false);
        var accountId = player.Account!.GetId();
        var other = await CreateBettorAsync(player.GameContext).ConfigureAwait(false);
        var duelistId = requester.Account!.GetId();
        var duel = new DuelRoom(new(), requester, opponent) { State = DuelState.DuelAccepted };
        var book = new DuelBetBook(duelistId, opponent.Account!.GetId(), DateTime.UtcNow.AddSeconds(30), new());
        var match = new DuelBetRoom(duel, book);
        var ticket = new DuelBetTicket(Guid.NewGuid(), accountId, DuelBetCurrency.Bless, DuelBetSide.Requester, 10);
        var opposingTicket = new DuelBetTicket(Guid.NewGuid(), other.Account!.GetId(), DuelBetCurrency.Bless, DuelBetSide.Opponent, 20);
        Assert.That(player.DuelRoom, Is.Null);

        Assert.That((await match.SnapshotAsync(requester).ConfigureAwait(false)).CanBet, Is.False);
        Assert.That((await match.SnapshotAsync(player).ConfigureAwait(false)).CanBet, Is.True);
        Assert.That(await match.PlaceAsync(player, ticket with { Amount = 40 }).ConfigureAwait(false), Is.EqualTo(DuelBetRejection.NotEnoughJewels));
        Assert.That(await match.PlaceAsync(player, ticket).ConfigureAwait(false), Is.EqualTo(DuelBetRejection.None));
        Assert.That(await match.PlaceAsync(player, ticket).ConfigureAwait(false), Is.EqualTo(DuelBetRejection.None));
        Assert.That(await match.PlaceAsync(other, opposingTicket).ConfigureAwait(false), Is.EqualTo(DuelBetRejection.None));
        var snapshot = await match.SnapshotAsync(player).ConfigureAwait(false);
        Assert.That(snapshot.HasBet, Is.True);
        Assert.That(snapshot.Pools.Single(), Is.EqualTo(new DuelBetPool(DuelBetCurrency.Bless, 10, 20)));
        Assert.That(BlessBalance(player), Is.EqualTo(20));
        Assert.That(await DuelBetWallet.ClaimAsync(player).ConfigureAwait(false), Is.Zero);

        await match.CloseAsync().ConfigureAwait(false);
        Assert.That(await match.PlaceAsync(player, ticket with { Id = Guid.NewGuid() }).ConfigureAwait(false), Is.EqualTo(DuelBetRejection.Closed));
        await match.SettleAsync(DuelBetSide.Requester).ConfigureAwait(false);
        Assert.That(await DuelBetWallet.ClaimAsync(player).ConfigureAwait(false), Is.EqualTo(30));
        Assert.That(await DuelBetWallet.ClaimAsync(player).ConfigureAwait(false), Is.Zero);
        Assert.That(await DuelBetWallet.ClaimAsync(other).ConfigureAwait(false), Is.Zero);
        Assert.That(BlessBalance(player) + BlessBalance(other), Is.EqualTo(60));
    }

    /// <summary>Only explicit acceptance can charge both fighters, and an unfinished match refunds them.</summary>
    [Test]
    public async Task WagerRequiresConsentAndBothBalancesAsync()
    {
        var challenger = await CreateBettorAsync().ConfigureAwait(false);
        var context = (GameContext)challenger.GameContext;
        var opponent = await CreateBettorAsync(context).ConfigureAwait(false);
        challenger.SelectedCharacter!.Name = "Alice";
        opponent.SelectedCharacter!.Name = "Bob";
        var area = new DuelArea
        {
            FirstPlayerGate = new ExitGate { Map = challenger.CurrentMap!.Definition },
            SecondPlayerGate = new ExitGate { Map = challenger.CurrentMap!.Definition },
        };
        var config = new Mock<DuelConfiguration>();
        config.SetupGet(value => value.DuelAreas).Returns([area]);
        context.Configuration.DuelConfiguration = config.Object;
        context.DuelRoomManager = new(config.Object);
        context.FeaturePlugIns.AddPlugIn(new DuelBettingFeaturePlugIn(), true);

        challenger.Account!.WCoinC = 25;
        opponent.Account!.WCoinC = 9;
        await challenger.AddObserverAsync(opponent).ConfigureAwait(false);
        var actions = new DuelBettingActions();
        var challenge = JsonSerializer.SerializeToUtf8Bytes(new { TargetId = opponent.Id, TargetName = opponent.Name, Amount = 10 });
        await actions.ExecuteAsync(challenger, 0, (byte)DuelBetOperation.Challenge, challenge).ConfigureAwait(false);
        var room = challenger.DuelRoom!;
        var wager = room.Wager!;
        Assert.That(wager.Amount, Is.EqualTo(10));
        var reply = JsonSerializer.SerializeToUtf8Bytes(new { wager.OfferId, Accepted = true });
        await new DuelStartResponseHandlerPlugIn().HandlePacketAsync(opponent, CreateDuelReply(challenger, true, true)).ConfigureAwait(false);
        await actions.ExecuteAsync(challenger, 0, (byte)DuelBetOperation.RespondToWager, reply).ConfigureAwait(false);
        Assert.That(room.State, Is.EqualTo(DuelState.DuelRequested));
        Assert.That(challenger.Account.WCoinC, Is.EqualTo(25));
        await actions.ExecuteAsync(opponent, 0, (byte)DuelBetOperation.RespondToWager, reply).ConfigureAwait(false);
        Assert.That(room.State, Is.EqualTo(DuelState.DuelStartFailed));
        Assert.That(challenger.Account.WCoinC, Is.EqualTo(25));
        Assert.That(opponent.Account.WCoinC, Is.EqualTo(9));

        await actions.ExecuteAsync(challenger, 0, (byte)DuelBetOperation.Challenge, challenge).ConfigureAwait(false);
        room = challenger.DuelRoom!;
        await actions.ExecuteAsync(opponent, 0, (byte)DuelBetOperation.RespondToWager, reply).ConfigureAwait(false);
        Assert.That(room.State, Is.EqualTo(DuelState.DuelRequested));
        opponent.Account.WCoinC = 30;
        reply = JsonSerializer.SerializeToUtf8Bytes(new { room.Wager!.OfferId, Accepted = true });
        await actions.ExecuteAsync(opponent, 0, (byte)DuelBetOperation.RespondToWager, reply).ConfigureAwait(false);
        Assert.That(room.State, Is.EqualTo(DuelState.DuelAccepted));
        Assert.That(challenger.Account.WCoinC, Is.EqualTo(15));
        Assert.That(opponent.Account.WCoinC, Is.EqualTo(20));
        await room.CancelDuelAsync().ConfigureAwait(false);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (opponent.DuelRoom is not null)
        {
            await Task.Delay(20, timeout.Token).ConfigureAwait(false);
        }

        Assert.That(await DuelBetWallet.ClaimAsync(challenger).ConfigureAwait(false), Is.EqualTo(10));
        Assert.That(await DuelBetWallet.ClaimAsync(opponent).ConfigureAwait(false), Is.EqualTo(10));
        Assert.That(challenger.Account.WCoinC, Is.EqualTo(25));
        Assert.That(opponent.Account.WCoinC, Is.EqualTo(30));
    }

    /// <summary>The fighter takes both stakes. A correct spectator pick pays double, including in a one-sided market.</summary>
    /// <param name="opposingBet">Whether a spectator also picked the losing fighter.</param>
    [TestCase(true)]
    [TestCase(false)]
    public async Task WCoinWinnerTakesPotAndSpectatorGetsDoubleAsync(bool opposingBet)
    {
        var winner = await CreateBettorAsync().ConfigureAwait(false);
        var other = await CreateBettorAsync(winner.GameContext).ConfigureAwait(false);
        var correct = await CreateBettorAsync(winner.GameContext).ConfigureAwait(false);
        var incorrect = await CreateBettorAsync(winner.GameContext).ConfigureAwait(false);
        foreach (var participant in new[] { winner, other, correct, incorrect })
        {
            participant.Account!.WCoinC = 30;
        }

        var settings = new DuelBettingSettings { FeePercent = 15 };
        var wager = new DuelWager(15, settings);
        var room = new DuelRoom(new(), winner, other) { Wager = wager, State = DuelState.DuelAccepted };
        Assert.That(await wager.FundAsync(winner, other).ConfigureAwait(false), Is.EqualTo(DuelBetRejection.None));
        var book = new DuelBetBook(winner.Account!.GetId(), other.Account!.GetId(), DateTime.UtcNow.AddSeconds(20), settings, wCoinMatch: true);
        var match = new DuelBetRoom(room, book);
        var ticket = new DuelBetTicket(Guid.NewGuid(), correct.Account!.GetId(), DuelBetCurrency.WCoinC, DuelBetSide.Requester, 10);
        Assert.That(await match.PlaceAsync(correct, ticket with { Currency = DuelBetCurrency.Bless }).ConfigureAwait(false), Is.EqualTo(DuelBetRejection.CurrencyDisabled));
        Assert.That(await match.PlaceAsync(correct, ticket).ConfigureAwait(false), Is.EqualTo(DuelBetRejection.None));
        Assert.That(await match.PlaceAsync(correct, ticket).ConfigureAwait(false), Is.EqualTo(DuelBetRejection.None));
        if (opposingBet)
        {
            var wrong = new DuelBetTicket(Guid.NewGuid(), incorrect.Account!.GetId(), DuelBetCurrency.WCoinC, DuelBetSide.Opponent, 5);
            Assert.That(await match.PlaceAsync(incorrect, wrong).ConfigureAwait(false), Is.EqualTo(DuelBetRejection.None));
            var context = (GameContext)winner.GameContext;
            await context.AddPlayerAsync(winner).ConfigureAwait(false);
            await context.AddPlayerAsync(correct).ConfigureAwait(false);
        }

        await match.SettleAsync(DuelBetSide.Requester).ConfigureAwait(false);
        Assert.That(await DuelBetWallet.ClaimAsync(winner).ConfigureAwait(false), Is.EqualTo(opposingBet ? 0 : 30));
        Assert.That(await DuelBetWallet.ClaimAsync(other).ConfigureAwait(false), Is.Zero);
        Assert.That(await DuelBetWallet.ClaimAsync(correct).ConfigureAwait(false), Is.EqualTo(opposingBet ? 0 : 20));
        Assert.That(await DuelBetWallet.ClaimAsync(correct).ConfigureAwait(false), Is.Zero);
        Assert.That(await DuelBetWallet.ClaimAsync(incorrect).ConfigureAwait(false), Is.Zero);
        Assert.That(winner.Account!.WCoinC, Is.EqualTo(45));
        Assert.That(other.Account!.WCoinC, Is.EqualTo(15));
        Assert.That(correct.Account!.WCoinC, Is.EqualTo(40));
        Assert.That(incorrect.Account!.WCoinC, Is.EqualTo(opposingBet ? 25 : 30));
    }

    /// <summary>After an abandoned match lease expires, the stake can be recovered from a new session.</summary>
    [Test]
    public async Task ExpiredEscrowRefundsTheAccountAsync()
    {
        var player = await CreateBettorAsync().ConfigureAwait(false);
        var ticket = new DuelBetTicket(Guid.NewGuid(), player.Account!.GetId(), DuelBetCurrency.Bless, DuelBetSide.Requester, 10);
        var escrow = await DuelBetWallet.DebitAsync(player, Guid.Empty, ticket, Now).ConfigureAwait(false);
        Assert.That(escrow, Is.Not.Null.And.Not.EqualTo(Guid.Empty));
        var nextSession = await CreateBettorAsync(player.GameContext).ConfigureAwait(false);
        nextSession.Account = new BasicModel.Account { Id = ticket.AccountId, JewelBankBalances = player.Account!.JewelBankBalances };

        Assert.That(await DuelBetWallet.ClaimAsync(nextSession).ConfigureAwait(false), Is.EqualTo(10));
        Assert.That(await DuelBetWallet.ClaimAsync(nextSession).ConfigureAwait(false), Is.Zero);
        Assert.That(BlessBalance(nextSession), Is.EqualTo(30));
    }

    /// <summary>The custom view uses a C2 frame and preserves the match identity and eligibility.</summary>
    [Test]
    public async Task BoardWireFormatAsync()
    {
        var (player, output) = CastleSiegeRemoteViewTestHelper.CreatePlayer();
        var duel = Guid.NewGuid();
        await new DuelBettingView(player).ShowAsync(
            13, true, string.Empty, [new(duel, "Alice", "Bob", 0, 0, true, true, 30, [], 0, 4, FirstTo: 10)], 30, 5, 10).ConfigureAwait(false);
        var data = output.ToArray();
        Assert.That(data[0], Is.EqualTo(0xC2));
        Assert.That((data[1] << 8) + data[2], Is.EqualTo(data.Length));
        Assert.That(data[3..5], Is.EqualTo(new byte[] { 0xFD, 0x10 }));
        using var json = JsonDocument.Parse(data.AsMemory(5));
        Assert.That(json.RootElement.GetProperty("RequestId").GetInt32(), Is.EqualTo(13));
        Assert.That(json.RootElement.GetProperty("PublicSeconds").GetInt32(), Is.EqualTo(30));
        Assert.That(json.RootElement.GetProperty("PrivateSeconds").GetInt32(), Is.EqualTo(5));
        var row = json.RootElement.GetProperty("Duels")[0];
        Assert.That(row.GetProperty("Duel").GetGuid(), Is.EqualTo(duel));
        Assert.That(row.GetProperty("CanBet").GetBoolean(), Is.True);
        Assert.That(row.GetProperty("SecondsLeft").GetInt32(), Is.EqualTo(30));
        Assert.That(row.GetProperty("RequesterClass").GetInt32(), Is.Zero);
        Assert.That(row.GetProperty("FirstTo").GetInt32(), Is.EqualTo(10));
    }

    /// <summary>The custom invitation describes the server rules before the standard accept or decline exchange.</summary>
    [Test]
    public async Task InvitationWireFormatAsync()
    {
        var (player, output) = CastleSiegeRemoteViewTestHelper.CreatePlayer();
        var id = Guid.NewGuid();
        await new DuelBettingView(player).ShowInvitationAsync(new("Alice", 4, true, 30, 10, id, 15)).ConfigureAwait(false);
        var data = output.ToArray();
        Assert.That(data[3..5], Is.EqualTo(new byte[] { 0xFD, 0x10 }));
        using var json = JsonDocument.Parse(data.AsMemory(5));
        var offer = json.RootElement.GetProperty("Invitation");
        Assert.That(offer.GetProperty("Requester").GetString(), Is.EqualTo("Alice"));
        Assert.That(offer.GetProperty("Public").GetBoolean(), Is.True);
        Assert.That(offer.GetProperty("Seconds").GetInt32(), Is.EqualTo(30));
        Assert.That(offer.GetProperty("FirstTo").GetInt32(), Is.EqualTo(10));
        Assert.That(offer.GetProperty("OfferId").GetGuid(), Is.EqualTo(id));
        Assert.That(offer.GetProperty("Wager").GetInt32(), Is.EqualTo(15));
    }

    private static byte[] CreateDuelReply(Player requester, bool windowsClient, bool accepted)
    {
        const int invitationIdOffset = 4;
        const int replyIdOffset = 5;
        const int idLength = sizeof(ushort);
        var invitation = new byte[DuelInvitationPacket.Length];
        _ = new DuelInvitationPacket(invitation) { RequesterId = requester.Id, RequesterName = requester.Name };
        var reply = new byte[DuelStartResponse.Length];
        _ = new DuelStartResponse(reply) { Response = accepted };

        // Both clients read the invitation ID as little-endian. Windows then writes it as big-endian.
        var replyId = reply.AsSpan(replyIdOffset, idLength);
        invitation.AsSpan(invitationIdOffset, idLength).CopyTo(replyId);
        if (windowsClient)
        {
            replyId.Reverse();
        }

        invitation.AsSpan(invitationIdOffset + idLength).CopyTo(reply.AsSpan(replyIdOffset + idLength));
        return reply;
    }

    private static async ValueTask<Player> CreateBettorAsync(IGameContext? context = null)
    {
        var player = context is null
            ? await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false)
            : await PlayerTestHelper.CreatePlayerAsync(context).ConfigureAwait(false);
        player.Account = new BasicModel.Account { Id = Guid.NewGuid(), JewelBankBalances = "{\"7181:0\":30}" };
        await player.PlayerState.TryAdvanceToAsync(PlayerState.EnteredWorld).ConfigureAwait(false);
        return player;
    }

    private static long BlessBalance(Player player)
        => JewelBankLedger.Read(player.Account!.JewelBankBalances, [7181]).Single().Amount;

    private static DuelBetTicket Ticket(int id, int amount, DuelBetSide side = DuelBetSide.Requester, DuelBetCurrency currency = DuelBetCurrency.Bless)
        => new(new Guid(id, 0, 0, new byte[8]), new Guid(id + 100, 0, 0, new byte[8]), currency, side, amount);
}
