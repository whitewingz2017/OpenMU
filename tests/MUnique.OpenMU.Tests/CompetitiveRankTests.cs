// <copyright file="CompetitiveRankTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using Moq;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.CashShop;
using MUnique.OpenMU.GameLogic.RankTitles;
using MUnique.OpenMU.GameLogic.Views.CashShop;
using MUnique.OpenMU.Interfaces;
using MUnique.OpenMU.Persistence;

/// <summary>Verifies earned rank eligibility, persistent standings, weekly boundaries and death notifications.</summary>
[TestFixture]
public class CompetitiveRankTests
{
    /// <summary>Even an old configured positive price cannot sell an earned title.</summary>
    [TestCase(1)]
    [TestCase(7)]
    [TestCase(9)]
    public async Task EarnedTitlesCannotBePurchasedOrInheritedFromOldOwnershipAsync(int rank)
    {
        var player = await CreateAsync("Alpha").ConfigureAwait(false);
        RankTitleFeaturePlugIn.Settings(player.GameContext)!.Titles.Single(title => title.Rank == rank).Price = 100;
        player.Account!.WCoinC = 500;
        await new RankTitleActions().ExecuteAsync(player, 1, 1, rank, 100).ConfigureAwait(false);
        Assert.That(player.Account.WCoinC, Is.EqualTo(500));
        Assert.That(player.SelectedCharacter!.OwnedRankTitlesJson, Is.EqualTo("[]"));
        player.SelectedCharacter.OwnedRankTitlesJson = $"[{rank}]";
        player.SelectedCharacter.ActiveRankTitle = rank;
        Assert.That(RankTitleRules.Active(player), Is.Zero);
    }

    /// <summary>Gold tickets grant account-wide VIP for their configured duration, extend it, and expire.</summary>
    [Test]
    public async Task GoldTicketVipExtendsAndExpiresAsync()
    {
        var player = await CreateAsync("Alpha").ConfigureAwait(false);
        var delivery = new GoldTicketVipDeliveryPlugIn();
        var product = new CashShopProduct { ItemDefinition = new() { Group = 13, Number = 124 }, Duration = TimeSpan.FromDays(1) };
        Assert.That(await delivery.DeliverAsync(player, product).ConfigureAwait(false), Is.EqualTo(CashShopUseResult.Success));
        var first = player.Account!.VipExpiresAtUtc!.Value;
        Assert.That(RankTitleRules.Active(player), Is.EqualTo(1));
        await new RankTitleActions().ExecuteAsync(player, 1, 2, 0, 0).ConfigureAwait(false);
        Assert.That(RankTitleRules.Active(player), Is.Zero, "Unequip must also hide automatically inherited titles.");
        await new RankTitleActions().ExecuteAsync(player, 2, 2, 1, 0).ConfigureAwait(false);
        Assert.That(RankTitleRules.Active(player), Is.EqualTo(1));
        Assert.That(await delivery.DeliverAsync(player, product).ConfigureAwait(false), Is.EqualTo(CashShopUseResult.Success));
        Assert.That(player.Account.VipExpiresAtUtc, Is.EqualTo(first.AddDays(1)));
        using var fresh = player.GameContext.PersistenceContextProvider.CreateNewPlayerContext(player.GameContext.Configuration);
        await player.SaveProgressAsync().ConfigureAwait(false);
        Assert.That((await fresh.GetByIdAsync<Account>(player.Account.GetId()).ConfigureAwait(false))!.VipExpiresAtUtc, Is.EqualTo(first.AddDays(1)));
        player.Account.VipExpiresAtUtc = DateTime.UtcNow.AddSeconds(-1);
        Assert.That(RankTitleRules.Active(player), Is.Zero);
        product.Duration = TimeSpan.Zero;
        Assert.That(delivery.CanDeliver(product), Is.False);
    }

    /// <summary>Standings include offline players, survive a feature restart and transfer only after a higher score.</summary>
    [Test]
    public async Task WeeklyAndDuelLeadersPersistTransferAndResetAsync()
    {
        var first = await CreateAsync("Zeta").ConfigureAwait(false);
        var second = await CreateAsync("Alpha", first.GameContext).ConfigureAwait(false);
        var feature = first.GameContext.FeaturePlugIns.GetPlugIn<RankTitleFeaturePlugIn>()!;
        await feature.AttackableGotKilledAsync(second, first).ConfigureAwait(false);
        Assert.That(RankTitleRules.Active(first), Is.EqualTo(9));
        Assert.That(first.SelectedCharacter!.PlayerKillCount, Is.Zero);
        await feature.AttackableGotKilledAsync(first, second).ConfigureAwait(false);
        Assert.That(feature.HasCompetitiveTitle(first, 9), Is.True, "The earlier tied score keeps the rank.");
        await feature.AttackableGotKilledAsync(first, second).ConfigureAwait(false);
        Assert.That(feature.HasCompetitiveTitle(second, 9), Is.True);
        Assert.That(feature.HasCompetitiveTitle(first, 9), Is.False);
        await feature.RecordDuelWinAsync(first, second).ConfigureAwait(false);
        await feature.RecordDuelWinAsync(second, first).ConfigureAwait(false);
        Assert.That(feature.HasCompetitiveTitle(first, 7), Is.True);
        await feature.RecordDuelWinAsync(second, first).ConfigureAwait(false);
        Assert.That(feature.HasCompetitiveTitle(second, 7), Is.True);
        var restarted = new RankTitleFeaturePlugIn();
        await restarted.RefreshLeadersAsync(first.GameContext, true).ConfigureAwait(false);
        Assert.That(restarted.HasCompetitiveTitle(second, 7), Is.True);
        Assert.That(restarted.HasCompetitiveTitle(second, 9), Is.True);
        first.SelectedCharacter.PlayerKillWeekStartUtc = RankTitleFeaturePlugIn.WeekStartUtc(DateTime.UtcNow).AddDays(-7);
        second.SelectedCharacter!.PlayerKillWeekStartUtc = first.SelectedCharacter.PlayerKillWeekStartUtc;
        await restarted.RefreshLeadersAsync(first.GameContext, true).ConfigureAwait(false);
        Assert.That(restarted.HasCompetitiveTitle(second, 9), Is.False);
        await feature.AttackableGotKilledAsync(second, first).ConfigureAwait(false);
        Assert.That(first.SelectedCharacter.WeeklyPlayerKills, Is.EqualTo(1));
        Assert.That(feature.HasCompetitiveTitle(first, 9), Is.True);
        Assert.That(second.SelectedCharacter.DuelWins, Is.EqualTo(2), "Weekly resets leave duel wins intact.");
    }

    /// <summary>The durable mailbox letter names the actual killer, and self/monster deaths do not count.</summary>
    [Test]
    public async Task DeathMailAndScoreValidationAsync()
    {
        var killer = await CreateAsync("Killer").ConfigureAwait(false);
        var victim = await CreateAsync("Victim", killer.GameContext).ConfigureAwait(false);
        var feature = killer.GameContext.FeaturePlugIns.GetPlugIn<RankTitleFeaturePlugIn>()!;
        await feature.AttackableGotKilledAsync(victim, killer).ConfigureAwait(false);
        using var context = killer.GameContext.PersistenceContextProvider.CreateNewPlayerContext(killer.GameContext.Configuration);
        var body = (await context.GetAsync<LetterBody>().ConfigureAwait(false)).Single();
        Assert.That(body.Header!.ReceiverName, Is.EqualTo("Victim"));
        Assert.That(body.Header.SenderName, Is.EqualTo("Killer"));
        Assert.That(body.Message, Does.Contain("Killer"));
        Assert.That(body.Attachment, Is.Null);
        await feature.AttackableGotKilledAsync(killer, killer).ConfigureAwait(false);
        await feature.AttackableGotKilledAsync(victim, null).ConfigureAwait(false);
        killer.Account!.IsBot = true;
        await feature.AttackableGotKilledAsync(victim, killer).ConfigureAwait(false);
        Assert.That(killer.SelectedCharacter!.WeeklyPlayerKills, Is.EqualTo(1));
        Assert.That(await context.GetAsync<LetterBody>().ConfigureAwait(false), Has.Count.EqualTo(1));
    }

    /// <summary>A failed save cannot grant a score or a competitive title.</summary>
    [Test]
    public async Task FailedScoreSaveRollsBackAsync()
    {
        var killer = await CreateAsync("Killer").ConfigureAwait(false);
        var victim = await CreateAsync("Victim", killer.GameContext).ConfigureAwait(false);
        CashShopActionsTest.SaveFailingContext.Install(killer, () => true);
        var feature = killer.GameContext.FeaturePlugIns.GetPlugIn<RankTitleFeaturePlugIn>()!;
        await feature.RecordDuelWinAsync(killer, victim).ConfigureAwait(false);
        Assert.That(killer.SelectedCharacter!.DuelWins, Is.Zero);
        Assert.That(RankTitleRules.Active(killer), Is.Zero);
    }

    /// <summary>Local Sunday and Monday map to the proper UTC week despite the timezone offset.</summary>
    [Test]
    public void MondayBoundaryUsesServerTimeZone()
    {
        var zone = TimeZoneInfo.CreateCustomTimeZone("Test", TimeSpan.FromHours(8), "Test", "Test");
        Assert.That(RankTitleFeaturePlugIn.WeekStartUtc(new(2026, 11, 8, 15, 59, 59, DateTimeKind.Utc), zone),
            Is.EqualTo(new DateTime(2026, 11, 1, 16, 0, 0, DateTimeKind.Utc)));
        Assert.That(RankTitleFeaturePlugIn.WeekStartUtc(new(2026, 11, 8, 16, 0, 0, DateTimeKind.Utc), zone),
            Is.EqualTo(new DateTime(2026, 11, 8, 16, 0, 0, DateTimeKind.Utc)));
    }

    private static async ValueTask<Player> CreateAsync(string name, IGameContext? game = null)
    {
        var player = game is null ? await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false)
            : await PlayerTestHelper.CreatePlayerAsync(game).ConfigureAwait(false);
        if (player.GameContext.FeaturePlugIns.GetPlugIn<RankTitleFeaturePlugIn>() is null)
        {
            player.GameContext.FeaturePlugIns.AddPlugIn(new RankTitleFeaturePlugIn(), true);
        }

        var account = player.PersistenceContext.CreateNew<Account>();
        account.LoginName = name;
        var character = player.PersistenceContext.CreateNew<Character>();
        character.Name = name;
        character.CharacterClass = player.SelectedCharacter!.CharacterClass;
        character.CurrentMap = player.SelectedCharacter.CurrentMap;
        character.Inventory = player.PersistenceContext.CreateNew<ItemStorage>();
        foreach (var attribute in player.SelectedCharacter.Attributes)
        {
            character.Attributes.Add(player.PersistenceContext.CreateNew<MUnique.OpenMU.AttributeSystem.StatAttribute>(attribute.Definition!, attribute.Value));
        }

        account.Characters.Add(character);
        await player.SetAccountAsync(account).ConfigureAwait(false);
        await player.SetSelectedCharacterAsync(character).ConfigureAwait(false);
        await player.SaveProgressAsync().ConfigureAwait(false);
        await player.PlayerState.TryAdvanceToAsync(PlayerState.EnteredWorld).ConfigureAwait(false);
        return player;
    }
}
