// <copyright file="RankTitleTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using System.Text.Json;
using Moq;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.RankTitles;
using MUnique.OpenMU.GameLogic.Views.Inventory;
using MUnique.OpenMU.GameServer.MessageHandler.Cosmetics;
using MUnique.OpenMU.Network.Packets.ClientToServer;
using MUnique.OpenMU.Persistence;

/// <summary>Verifies WCoinC charges, permanent ownership, equipment and failure recovery.</summary>
[TestFixture]
public class RankTitleTests
{
    /// <summary>Concurrent requests and a second purchase never charge twice for the same title.</summary>
    [TestCase(1)]
    [TestCase(18)]
    public async Task PurchaseReplayAndEquipAsync(int rank)
    {
        var player = await CreateAsync().ConfigureAwait(false);
        var actions = new RankTitleActions();
        await Task.WhenAll(actions.ExecuteAsync(player, 1, 1, rank, 100).AsTask(),
            actions.ExecuteAsync(player, 1, 1, rank, 100).AsTask()).ConfigureAwait(false);
        await actions.ExecuteAsync(player, 2, 1, rank, 100).ConfigureAwait(false);
        Assert.That(player.Account!.WCoinC, Is.EqualTo(400));
        Assert.That(JsonSerializer.Deserialize<int[]>(player.SelectedCharacter!.OwnedRankTitlesJson), Is.EqualTo(new[] { rank }));
        await actions.ExecuteAsync(player, 3, 2, rank, 0).ConfigureAwait(false);
        Assert.That(RankTitleRules.Active(player), Is.EqualTo(rank));
        await actions.ExecuteAsync(player, 4, 2, 0, 0).ConfigureAwait(false);
        Assert.That(RankTitleRules.Active(player), Is.Zero);
        Assert.That(player.Account.WCoinC, Is.EqualTo(400));
    }

    /// <summary>Forged identifiers, prices, equipment and operations never spend coins or grant titles.</summary>
    [TestCase(1, 1, 99)]
    [TestCase(1, 1, -1)]
    [TestCase(1, 19, 100)]
    [TestCase(2, 1, 0)]
    [TestCase(2, 0, 100)]
    [TestCase(3, 1, 100)]
    public async Task InvalidRequestDoesNotSpendAsync(byte operation, int rank, int price)
    {
        var player = await CreateAsync().ConfigureAwait(false);
        await new RankTitleActions().ExecuteAsync(player, 1, operation, rank, price).ConfigureAwait(false);
        Assert.That(player.Account!.WCoinC, Is.EqualTo(500));
        Assert.That(player.SelectedCharacter!.OwnedRankTitlesJson, Is.EqualTo("[]"));
        Assert.That(player.SelectedCharacter.ActiveRankTitle, Is.Zero);
    }

    /// <summary>Insufficient balance and disabled prices reject purchases.</summary>
    [TestCase(99, 100)]
    [TestCase(500, 0)]
    public async Task UnavailablePurchaseAsync(int coins, int price)
    {
        var player = await CreateAsync().ConfigureAwait(false);
        player.Account!.WCoinC = coins;
        RankTitleFeaturePlugIn.Settings(player.GameContext)!.Titles.First().Price = price;
        await new RankTitleActions().ExecuteAsync(player, 1, 1, 1, price).ConfigureAwait(false);
        Assert.That(player.Account.WCoinC, Is.EqualTo(coins));
        Assert.That(player.SelectedCharacter!.OwnedRankTitlesJson, Is.EqualTo("[]"));
    }

    /// <summary>A failed save restores coins, ownership and the equipped title.</summary>
    [TestCase(false)]
    [TestCase(true)]
    public async Task SaveFailureRollsBackAsync(bool equip)
    {
        var player = await CreateAsync().ConfigureAwait(false);
        var character = player.SelectedCharacter!;
        character.OwnedRankTitlesJson = equip ? "[1]" : "[]";
        CashShopActionsTest.SaveFailingContext.Install(player, () => true);
        await new RankTitleActions().ExecuteAsync(player, 1, equip ? (byte)2 : (byte)1, 1, equip ? 0 : 100).ConfigureAwait(false);
        Assert.That(player.Account!.WCoinC, Is.EqualTo(500));
        Assert.That(character.OwnedRankTitlesJson, Is.EqualTo(equip ? "[1]" : "[]"));
        Assert.That(character.ActiveRankTitle, Is.Zero);
        Mock.Get(player.ViewPlugIns.GetPlugIn<IRankTitleView>()!)
            .Verify(view => view.ShowAsync(1, "RankTitles_SaveFailed"), Times.Once);
    }

    /// <summary>Corrupt persisted ownership is rejected instead of silently overwriting it.</summary>
    [TestCase("null")]
    [TestCase("[0]")]
    [TestCase("[65536]")]
    [TestCase("broken")]
    public async Task InvalidOwnershipIsPreservedAsync(string owned)
    {
        var player = await CreateAsync().ConfigureAwait(false);
        player.SelectedCharacter!.OwnedRankTitlesJson = owned;
        await new RankTitleActions().ExecuteAsync(player, 1, 1, 1, 100).ConfigureAwait(false);
        Assert.That(player.Account!.WCoinC, Is.EqualTo(500));
        Assert.That(player.SelectedCharacter.OwnedRankTitlesJson, Is.EqualTo(owned));
    }

    /// <summary>Refresh sends a ready response for an account whose real WCoinC balance is zero.</summary>
    [Test]
    public async Task RefreshWithZeroBalanceAsync()
    {
        var player = await CreateAsync().ConfigureAwait(false);
        player.Account!.WCoinC = 0;
        await new RankTitleActions().ExecuteAsync(player, 1, 0, 0, 0).ConfigureAwait(false);
        Mock.Get(player.ViewPlugIns.GetPlugIn<IRankTitleView>()!)
            .Verify(view => view.ShowAsync(1, null), Times.Once);
    }

    /// <summary>A different character starts a new request sequence without inheriting title ownership.</summary>
    [Test]
    public async Task CharacterSwitchResetsReplayStateAsync()
    {
        var player = await CreateAsync().ConfigureAwait(false);
        var actions = new RankTitleActions();
        await actions.ExecuteAsync(player, 9, 1, 1, 100).ConfigureAwait(false);
        var nextPlayer = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        var nextCharacter = nextPlayer.SelectedCharacter!;
        await player.SetSelectedCharacterAsync(nextCharacter).ConfigureAwait(false);
        await actions.ExecuteAsync(player, 1, 1, 2, 100).ConfigureAwait(false);
        Assert.That(JsonSerializer.Deserialize<int[]>(nextCharacter.OwnedRankTitlesJson), Is.EqualTo(new[] { 2 }));
        Assert.That(player.Account!.WCoinC, Is.EqualTo(300));
    }

    /// <summary>Pending administrator grants are applied once before a title purchase.</summary>
    [Test]
    public async Task PendingCoinGrantCanFundPurchaseAsync()
    {
        var player = await CreateAsync().ConfigureAwait(false);
        player.Account!.WCoinC = 0;
        var grant = player.PersistenceContext.CreateNew<CashShopCoinGrant>();
        grant.AccountId = player.Account.GetId();
        grant.CoinType = CashShopCoinType.WCoinC;
        grant.Amount = 150;
        grant.CreatedAt = DateTime.UtcNow;
        await player.PersistenceContext.SaveChangesAsync().ConfigureAwait(false);
        var actions = new RankTitleActions();
        await actions.ExecuteAsync(player, 1, 1, 1, 100).ConfigureAwait(false);
        await actions.ExecuteAsync(player, 2, 0, 0, 0).ConfigureAwait(false);
        Assert.That(player.Account.WCoinC, Is.EqualTo(50));
        Assert.That(grant.AppliedAt, Is.Not.Null);
        Assert.That(JsonSerializer.Deserialize<int[]>(player.SelectedCharacter!.OwnedRankTitlesJson), Is.EqualTo(new[] { 1 }));
    }

    /// <summary>A fresh action session still reads permanent ownership and equipped rank.</summary>
    [Test]
    public async Task NewSessionRetainsOwnedTitleAsync()
    {
        var player = await CreateAsync().ConfigureAwait(false);
        await new RankTitleActions().ExecuteAsync(player, 1, 1, 1, 100).ConfigureAwait(false);
        await new RankTitleActions().ExecuteAsync(player, 1, 2, 1, 0).ConfigureAwait(false);
        Assert.That(RankTitleRules.Active(player), Is.EqualTo(1));
        Assert.That(player.Account!.WCoinC, Is.EqualTo(400));
    }

    /// <summary>The registered custom-packet handler routes FD 08 into the account-backed purchase flow.</summary>
    [Test]
    public async Task PacketHandlerPurchasesTitleAsync()
    {
        var player = await CreateAsync().ConfigureAwait(false);
        var bytes = new byte[RankTitleRequestExtended.Length];
        var request = new RankTitleRequestExtended(bytes) { Operation = 1, Rank = 1, QuotedPrice = 100, RequestId = 7 };
        await new CosmeticArmorHandlerPlugIn().HandlePacketAsync(player, bytes).ConfigureAwait(false);
        Assert.That(player.Account!.WCoinC, Is.EqualTo(400));
        Assert.That(JsonSerializer.Deserialize<int[]>(player.SelectedCharacter!.OwnedRankTitlesJson), Is.EqualTo(new[] { 1 }));
        Mock.Get(player.ViewPlugIns.GetPlugIn<IRankTitleView>()!)
            .Verify(view => view.ShowAsync(request.RequestId, "RankTitles_Purchased"), Times.Once);
    }

    private static async ValueTask<Player> CreateAsync()
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        await player.PlayerState.TryAdvanceToAsync(PlayerState.EnteredWorld).ConfigureAwait(false);
        player.GameContext.FeaturePlugIns.AddPlugIn(new RankTitleFeaturePlugIn(), true);
        player.Account!.WCoinC = 500;
        return player;
    }
}
