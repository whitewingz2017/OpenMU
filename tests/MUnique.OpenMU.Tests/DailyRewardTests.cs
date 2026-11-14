// <copyright file="DailyRewardTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.DailyRewards;
using BasicModel = MUnique.OpenMU.Persistence.BasicModel;

/// <summary>Tests the calendar month daily login rewards.</summary>
[TestFixture]
public class DailyRewardTests
{
    private static readonly DateTime Now = new(2026, 11, 14, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>Today's reward grants every configured part once per account and day.</summary>
    [Test]
    public async Task ClaimGrantsTodaysRewardOnceAsync()
    {
        var (player, actions, today) = await CreateAsync().ConfigureAwait(false);
        await actions.ExecuteAsync(player, 1, 1, today, Now).ConfigureAwait(false);

        Assert.That(player.Money, Is.EqualTo(1000));
        Assert.That(player.Account!.WCoinC, Is.EqualTo(10));
        Assert.That(player.Account.WCoinP, Is.EqualTo(20));
        Assert.That(player.Account.GoblinPoints, Is.EqualTo(30));
        var item = player.Inventory!.Items.Single();
        Assert.That(item.Level, Is.EqualTo(3));
        Assert.That(DailyRewardCalendar.IsClaimed(DailyRewardCalendar.ClaimedDays(player.Account, DailyRewardCalendar.Today(Now).Month), today), Is.True);

        await actions.ExecuteAsync(player, 2, 1, today, Now).ConfigureAwait(false);
        Assert.That(player.Money, Is.EqualTo(1000));
        Assert.That(player.Account.WCoinC, Is.EqualTo(10));
        Assert.That(player.Inventory.Items.Count, Is.EqualTo(1));
    }

    /// <summary>Only the server's current day can be claimed; a replayed request is ignored.</summary>
    /// <param name="dayOffset">The offset of the claimed day to today.</param>
    /// <param name="requestId">The request identifier.</param>
    [TestCase(1, 1u)]
    [TestCase(-1, 1u)]
    [TestCase(0, 0u)]
    public async Task OtherDaysAndReplaysAreRejectedAsync(int dayOffset, uint requestId)
    {
        var (player, actions, today) = await CreateAsync().ConfigureAwait(false);
        await actions.ExecuteAsync(player, requestId, 1, (byte)(today + dayOffset), Now).ConfigureAwait(false);
        Assert.That(player.Money, Is.Zero);
        Assert.That(player.Account!.WCoinC, Is.Zero);
        Assert.That(player.Inventory!.Items, Is.Empty);
        Assert.That(player.Account.DailyRewardClaimedDays, Is.Zero);
    }

    /// <summary>Days claimed in a previous month don't count in the current month.</summary>
    [Test]
    public async Task ClaimedDaysStartOverEachMonthAsync()
    {
        var (player, actions, today) = await CreateAsync().ConfigureAwait(false);
        var month = DailyRewardCalendar.Today(Now).Month;
        player.Account!.DailyRewardMonth = month - 1;
        player.Account.DailyRewardClaimedDays = DailyRewardCalendar.DayBit(today);
        await actions.ExecuteAsync(player, 1, 1, today, Now).ConfigureAwait(false);
        Assert.That(player.Account.DailyRewardMonth, Is.EqualTo(month));
        Assert.That(player.Account.DailyRewardClaimedDays, Is.EqualTo(DailyRewardCalendar.DayBit(today)));
        Assert.That(player.Money, Is.EqualTo(1000));
    }

    /// <summary>An item which doesn't fit leaves the whole reward unclaimed.</summary>
    [Test]
    public async Task FullInventoryClaimsNothingAsync()
    {
        var (player, actions, today) = await CreateAsync().ConfigureAwait(false);
        var settings = DailyRewardFeaturePlugIn.Settings(player.GameContext)!;
        settings.GetDay(today)!.Item!.Width = 9;
        await actions.ExecuteAsync(player, 1, 1, today, Now).ConfigureAwait(false);
        Assert.That(player.Money, Is.Zero);
        Assert.That(player.Account!.WCoinC, Is.Zero);
        Assert.That(player.Account.DailyRewardClaimedDays, Is.Zero);
        Assert.That(player.Inventory!.Items, Is.Empty);
    }

    /// <summary>A day without a reward can't be claimed, and invalid settings disable the feature.</summary>
    [Test]
    public async Task EmptyDayAndInvalidSettingsClaimNothingAsync()
    {
        var (player, actions, today) = await CreateAsync().ConfigureAwait(false);
        var settings = DailyRewardFeaturePlugIn.Settings(player.GameContext)!;
        var reward = settings.Days.Single(day => day.Day == today);
        settings.Days.Remove(reward);
        await actions.ExecuteAsync(player, 1, 1, today, Now).ConfigureAwait(false);
        Assert.That(player.Account!.DailyRewardClaimedDays, Is.Zero);

        settings.Days.Add(reward);
        settings.Days.Add(new DailyRewardDay { Day = today, Zen = 1 });
        Assert.That(settings.IsValid(), Is.False);
        Assert.That(DailyRewardFeaturePlugIn.Settings(player.GameContext), Is.Null);
        await actions.ExecuteAsync(player, 2, 1, today, Now).ConfigureAwait(false);
        Assert.That(player.Money, Is.Zero);
    }

    /// <summary>The calendar uses the days of the current month.</summary>
    [Test]
    public void CalendarUsesTheLocalMonth()
    {
        var (month, day, daysInMonth) = DailyRewardCalendar.Today(new DateTime(2026, 2, 10, 12, 0, 0, DateTimeKind.Utc), TimeZoneInfo.Utc);
        Assert.That(month, Is.EqualTo(202602));
        Assert.That(day, Is.EqualTo(10));
        Assert.That(daysInMonth, Is.EqualTo(28));
    }

    private static async Task<(Player Player, DailyRewardActions Actions, byte Today)> CreateAsync()
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        await player.PlayerState.TryAdvanceToAsync(PlayerState.EnteredWorld).ConfigureAwait(false);
        player.Money = 0;
        var definition = new BasicModel.ItemDefinition { Group = 14, Number = 13, Width = 1, Height = 1, Durability = 1, MaximumItemLevel = 15, Name = "Jewel of Bless" };
        player.GameContext.Configuration.Items.Add(definition);
        var today = (byte)DailyRewardCalendar.Today(Now).Day;
        var settings = new DailyRewardSettings();
        var reward = settings.Days.Single(day => day.Day == today);
        reward.Zen = 1000;
        reward.WCoinC = 10;
        reward.WCoinP = 20;
        reward.GoblinPoints = 30;
        reward.Item = definition;
        reward.ItemLevel = 3;
        player.GameContext.FeaturePlugIns.AddPlugIn(new DailyRewardFeaturePlugIn { Configuration = settings }, true);
        return (player, new DailyRewardActions(), today);
    }
}
