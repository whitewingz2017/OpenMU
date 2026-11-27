// <copyright file="DailyRewardTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using System.Text.Json;
using Moq;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.DailyRewards;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.GameServer.RemoteView.Inventory;
using MUnique.OpenMU.Persistence;
using MUnique.OpenMU.PlugIns;
using BasicModel = MUnique.OpenMU.Persistence.BasicModel;

/// <summary>Tests the calendar month daily login rewards.</summary>
[TestFixture]
public class DailyRewardTests
{
    private static readonly DateTime Now = new(2026, 11, 14, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>Includes every preview item while issuing a claim offer for today's reward only.</summary>
    [Test]
    public async Task CalendarIncludesItemsForOtherDaysWithoutAnOfferAsync()
    {
        var (player, output) = CastleSiegeRemoteViewTestHelper.CreatePlayer();
        using (output)
        {
            var (_, today, _) = DailyRewardCalendar.Today(DateTime.UtcNow);
            var previewDay = today == 1 ? 2 : 1;
            var item = new BasicModel.ItemDefinition { Group = 14, Number = 0, Name = "Reward item" };
            var restricted = new DailyRewardItem { Item = item, ClassFamily = new BasicModel.CharacterClass { Number = 8 } };
            var settings = new DailyRewardSettings
            {
                Days = new[] { today, previewDay }.Select(day => new DailyRewardDay
                {
                    Day = day,
                    ChooseOneItem = true,
                    Items = [new DailyRewardItem { Item = item }, restricted, new DailyRewardItem { Item = item }],
                }).ToList(),
            };
            player.GameContext.FeaturePlugIns.AddPlugIn(new DailyRewardFeaturePlugIn { Configuration = settings }, true);
            await new DailyRewardView(player).ShowAsync(1, null).ConfigureAwait(false);

            using var json = JsonDocument.Parse(output.ToArray().AsMemory(5));
            var days = json.RootElement.GetProperty("Days").EnumerateArray().ToArray();
            var preview = days.Single(day => day.GetProperty("Day").GetInt32() == previewDay);
            var current = days.Single(day => day.GetProperty("Day").GetInt32() == today);
            Assert.That(preview.GetProperty("Items").GetArrayLength(), Is.EqualTo(2));
            Assert.That(preview.GetProperty("OfferKey").GetUInt32(), Is.Zero);
            Assert.That(current.GetProperty("OfferKey").GetUInt32(), Is.Not.Zero);
        }
    }

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
        Assert.That(item.ExpiresAtUtc, Is.Null);
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
        settings.GetDay(today)!.ItemDurationHours = 24;
        player.GameContext.PlugInManager.RegisterPlugIn<IPeriodicTaskPlugIn, ItemExpirationPlugIn>();
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

    /// <summary>Every item category receives the same deadline and is removed only when it expires.</summary>
    /// <param name="group">The reward item's group.</param>
    /// <param name="number">The reward item's number.</param>
    [TestCase(14, 13)]
    [TestCase(0, 0)]
    [TestCase(12, 320)]
    [TestCase(13, 300)]
    public async Task TimedRewardExpiresAfterOneDayAsync(byte group, short number)
    {
        var (player, actions, today) = await CreateAsync().ConfigureAwait(false);
        player.GameContext.PlugInManager.RegisterPlugIn<IPeriodicTaskPlugIn, ItemExpirationPlugIn>();
        var reward = DailyRewardFeaturePlugIn.Settings(player.GameContext)!.GetDay(today)!;
        reward.Item!.Group = group;
        reward.Item.Number = number;
        if (group == 12)
        {
            reward.Item.ItemSlot = new BasicModel.ItemSlotType { ItemSlots = { 247 } };
        }
        else if (group == 13)
        {
            reward.Item.ItemSlot = new BasicModel.ItemSlotType { ItemSlots = { 8 } };
            reward.Item.Skill = new BasicModel.Skill { DamageType = DamageType.Fenrir };
        }

        reward.ItemDurationHours = 24;
        await actions.ExecuteAsync(player, 1, 1, today, Now).ConfigureAwait(false);
        var item = player.Inventory!.Items.Single();
        Assert.That(item.ExpiresAtUtc, Is.EqualTo(Now.AddDays(1)));
        if (group == 13)
        {
            Assert.That(item.HasSkill, Is.True);
        }

        await ItemExpiration.RemoveExpiredAsync(player, Now.AddDays(1).AddSeconds(-1)).ConfigureAwait(false);
        Assert.That(player.Inventory.Items.Count(), Is.EqualTo(1));
        await ItemExpiration.RemoveExpiredAsync(player, Now.AddDays(1)).ConfigureAwait(false);
        Assert.That(player.Inventory.Items, Is.Empty);
    }

    /// <summary>Disabled enforcement or an unrepresentable expiration refuses the entire reward.</summary>
    /// <param name="enforcement">Whether the expiration plugin is active.</param>
    /// <param name="hours">The configured duration.</param>
    [TestCase(false, 24)]
    [TestCase(true, 596523)]
    public async Task UnsupportedExpirationClaimsNothingAsync(bool enforcement, int hours)
    {
        var (player, actions, today) = await CreateAsync().ConfigureAwait(false);
        if (enforcement)
        {
            player.GameContext.PlugInManager.RegisterPlugIn<IPeriodicTaskPlugIn, ItemExpirationPlugIn>();
        }

        DailyRewardFeaturePlugIn.Settings(player.GameContext)!.GetDay(today)!.ItemDurationHours = hours;
        await actions.ExecuteAsync(player, 1, 1, today, Now).ConfigureAwait(false);
        Assert.That(player.Inventory!.Items, Is.Empty);
        Assert.That(player.Account!.DailyRewardClaimedDays, Is.Zero);
        Assert.That(player.Account.WCoinC, Is.Zero);
        Assert.That(player.Money, Is.Zero);
    }

    /// <summary>Invalid durations are rejected by configuration validation.</summary>
    /// <param name="hours">The invalid duration.</param>
    [TestCase(-1)]
    [TestCase(int.MaxValue)]
    public void InvalidDurationDisablesCalendar(int hours)
    {
        var settings = new DailyRewardSettings();
        settings.Days.First().ItemDurationHours = hours;
        Assert.That(settings.IsValid(), Is.False);
    }

    /// <summary>All items in a bundle are granted with their own lifetime.</summary>
    [Test]
    public async Task BundleGrantsPermanentAndTimedItemsAsync()
    {
        var (player, actions, today) = await CreateAsync().ConfigureAwait(false);
        player.GameContext.PlugInManager.RegisterPlugIn<IPeriodicTaskPlugIn, ItemExpirationPlugIn>();
        var reward = DailyRewardFeaturePlugIn.Settings(player.GameContext)!.GetDay(today)!;
        var timed = AddSkin(reward, 24);
        await actions.ExecuteAsync(player, 1, DailyRewardActions.ClaimOperation, today, Now, OfferKey(reward)).ConfigureAwait(false);

        Assert.That(player.Inventory!.Items.Count, Is.EqualTo(2));
        Assert.That(player.Inventory.Items.Single(item => item.Definition == reward.Item).ExpiresAtUtc, Is.Null);
        Assert.That(player.Inventory.Items.Single(item => item.Definition == timed.Item).ExpiresAtUtc, Is.EqualTo(Now.AddHours(24)));
        Assert.That(player.Account!.WCoinC, Is.EqualTo(10));
    }

    /// <summary>Invalid or stale selections leave the day available, then the selected item and currency are granted once.</summary>
    /// <param name="selection">The one-based index the player selects.</param>
    [TestCase((byte)1)]
    [TestCase((byte)2)]
    public async Task ChoiceGrantsOnlyTheSelectedItemAsync(byte selection)
    {
        var (player, actions, today) = await CreateAsync().ConfigureAwait(false);
        var reward = DailyRewardFeaturePlugIn.Settings(player.GameContext)!.GetDay(today)!;
        var choice = AddSkin(reward, 48);
        reward.ChooseOneItem = true;
        var stale = OfferKey(reward);
        choice.ItemDurationHours = 72;
        var current = OfferKey(reward);
        await actions.ExecuteAsync(player, 1, DailyRewardActions.ClaimOperation, today, Now).ConfigureAwait(false);
        await actions.ExecuteAsync(player, 2, DailyRewardActions.ClaimOperation, today, Now, stale, selection).ConfigureAwait(false);
        await actions.ExecuteAsync(player, 3, DailyRewardActions.ClaimOperation, today, Now, current, 3).ConfigureAwait(false);

        Assert.That(player.Inventory!.Items, Is.Empty);
        Assert.That(player.Account!.DailyRewardClaimedDays, Is.Zero);
        Assert.That(player.Account.WCoinC, Is.Zero);

        if (selection == 2)
        {
            player.GameContext.PlugInManager.RegisterPlugIn<IPeriodicTaskPlugIn, ItemExpirationPlugIn>();
        }

        await actions.ExecuteAsync(player, 4, DailyRewardActions.ClaimOperation, today, Now, current, selection).ConfigureAwait(false);
        await actions.ExecuteAsync(player, 5, DailyRewardActions.ClaimOperation, today, Now, current, 1).ConfigureAwait(false);
        var item = player.Inventory.Items.Single();
        Assert.That(item.Definition, Is.SameAs(selection == 1 ? reward.Item : choice.Item));
        Assert.That(item.ExpiresAtUtc, Is.EqualTo(selection == 1 ? (DateTime?)null : Now.AddHours(72)));
        Assert.That(player.Account.WCoinC, Is.EqualTo(10));
    }

    /// <summary>Items already added during the attempt are removed if a later item cannot fit.</summary>
    [Test]
    public async Task PartialBundleDoesNotGrantAnythingAsync()
    {
        var (player, actions, today) = await CreateAsync().ConfigureAwait(false);
        var reward = DailyRewardFeaturePlugIn.Settings(player.GameContext)!.GetDay(today)!;
        var second = AddSkin(reward, 0);
        second.Item!.Width = 9;
        await actions.ExecuteAsync(player, 1, DailyRewardActions.ClaimOperation, today, Now, OfferKey(reward)).ConfigureAwait(false);

        Assert.That(player.Inventory!.Items, Is.Empty);
        Assert.That(player.Account!.DailyRewardClaimedDays, Is.Zero);
        Assert.That(player.Account.WCoinC, Is.Zero);
        Assert.That(player.Money, Is.Zero);

        second.Item.Width = 1;
        await actions.ExecuteAsync(player, 2, DailyRewardActions.ClaimOperation, today, Now, OfferKey(reward)).ConfigureAwait(false);
        Assert.That(player.Inventory.Items.Count, Is.EqualTo(2));
    }

    /// <summary>Saved offers preserve item references, order, durations and the choice setting.</summary>
    [Test]
    public void ConfiguredChoicesRoundTrip()
    {
        var primary = new BasicModel.ItemDefinition { Id = Guid.NewGuid(), Group = 12, Number = 319, Name = "Permanent skin" };
        var day = new DailyRewardDay { Day = 1, Item = primary, ChooseOneItem = true };
        var timed = AddSkin(day, 24);
        var skin = (BasicModel.ItemDefinition)timed.Item!;
        skin.Id = Guid.NewGuid();
        var family = new BasicModel.CharacterClass { Id = Guid.NewGuid(), Number = 4 };
        timed.ClassFamily = family;
        var source = new Mock<IDataSource<GameConfiguration>>();
        source.Setup(data => data.Get(primary.Id)).Returns(primary);
        source.Setup(data => data.Get(skin.Id)).Returns(skin);
        source.Setup(data => data.Get(family.Id)).Returns(family);
        var references = new ByDataSourceReferenceHandler(source.Object);
        var plugIn = new BasicModel.PlugInConfiguration();
        plugIn.SetConfiguration(new DailyRewardSettings { Days = [day] }, references);

        var reloaded = plugIn.GetConfiguration<DailyRewardSettings>(references);
        Assert.That(reloaded!.IsValid(), Is.True);
        var saved = reloaded.GetDay(1)!;
        Assert.That(saved.ChooseOneItem, Is.True);
        Assert.That(saved.GetItems().Select(item => item.Item), Is.EqualTo(new[] { primary, skin }));
        Assert.That(saved.GetItems().Select(item => item.ItemDurationHours), Is.EqualTo(new[] { 0, 24 }));
        Assert.That(saved.Items.Single().ClassFamily, Is.SameAs(family));
    }

    /// <summary>Complete sets for two class families fit on the same day and only the matching bundle can be claimed.</summary>
    [TestCase(4, 1)]
    [TestCase(6, 1)]
    [TestCase(7, 1)]
    [TestCase(8, 11)]
    [TestCase(10, 11)]
    [TestCase(11, 11)]
    public async Task ClaimProjectsBundlesAcrossPromotionsAsync(byte characterClass, short expectedItem)
    {
        var (player, actions, today) = await CreateAsync().ConfigureAwait(false);
        var classes = player.GameContext.Configuration.CharacterClasses;
        var knight = new BasicModel.CharacterClass { Number = 4, NextGenerationClass = new BasicModel.CharacterClass
            { Number = 6, NextGenerationClass = new BasicModel.CharacterClass { Number = 7 } } };
        var elf = new BasicModel.CharacterClass { Number = 8, NextGenerationClass = new BasicModel.CharacterClass
            { Number = 10, NextGenerationClass = new BasicModel.CharacterClass { Number = 11 } } };
        var gladiator = new BasicModel.CharacterClass { Number = 12 };
        foreach (var entry in new[] { knight, knight.NextGenerationClass!, knight.NextGenerationClass!.NextGenerationClass!,
                     elf, elf.NextGenerationClass!, elf.NextGenerationClass!.NextGenerationClass!, gladiator })
        {
            classes.Add(entry);
        }

        player.SelectedCharacter!.CharacterClass = classes.Single(entry => entry.Number == characterClass);
        var settings = DailyRewardFeaturePlugIn.Settings(player.GameContext)!;
        var day = settings.GetDay(today)!;
        day.Item = null;
        for (byte group = 7; group <= 11; group++)
        {
            var dragon = new BasicModel.ItemDefinition { Group = group, Number = 1, Width = 1, Height = 1, Durability = 1 };
            dragon.QualifiedCharacters.Add(knight.NextGenerationClass!);
            dragon.QualifiedCharacters.Add(gladiator);
            var silk = new BasicModel.ItemDefinition { Group = group, Number = 11, Width = 1, Height = 1, Durability = 1 };
            silk.QualifiedCharacters.Add(elf.NextGenerationClass!.NextGenerationClass!);
            day.Items.Add(new DailyRewardItem { Item = dragon, ClassFamily = knight });
            day.Items.Add(new DailyRewardItem { Item = silk });
        }

        Assert.That(settings.IsValid(player.GameContext.Configuration), Is.True);
        Assert.That(day.ForClass(gladiator)!.ItemCount, Is.Zero);
        var offer = day.ForClass(CharacterClassFamily.Of(player))!;
        Assert.That(offer.ItemCount, Is.EqualTo(5));

        await actions.ExecuteAsync(player, 1, DailyRewardActions.ClaimOperation, today, Now, OfferKey(day)).ConfigureAwait(false);
        Assert.That(player.Inventory!.Items, Is.Empty);
        await actions.ExecuteAsync(player, 2, DailyRewardActions.ClaimOperation, today, Now, OfferKey(offer)).ConfigureAwait(false);

        Assert.That(player.Inventory.Items.Select(item => item.Definition!.Number), Is.EqualTo(Enumerable.Repeat(expectedItem, 5)));
        Assert.That(player.Account!.WCoinC, Is.EqualTo(10));
    }

    /// <summary>An ineligible class cannot claim either alternative and a sole eligible choice claims as a single item.</summary>
    [Test]
    public async Task FilteredChoiceUsesTheVisibleOfferAsync()
    {
        var (player, actions, today) = await CreateAsync().ConfigureAwait(false);
        var day = DailyRewardFeaturePlugIn.Settings(player.GameContext)!.GetDay(today)!;
        var knight = new BasicModel.CharacterClass { Number = 4 };
        var elf = new BasicModel.CharacterClass { Number = 8 };
        day.Item!.QualifiedCharacters.Add(knight);
        AddSkin(day, 0).Item!.QualifiedCharacters.Add(elf);
        day.ChooseOneItem = true;
        day.Zen = day.WCoinC = day.WCoinP = day.GoblinPoints = 0;
        Assert.That(day.ForClass(CharacterClassFamily.Of(player)), Is.Null);
        await actions.ExecuteAsync(player, 1, DailyRewardActions.ClaimOperation, today, Now).ConfigureAwait(false);
        Assert.That(player.Account!.DailyRewardClaimedDays, Is.Zero);

        player.SelectedCharacter!.CharacterClass = knight;
        var offer = day.ForClass(knight)!;
        Assert.That(offer.ItemCount, Is.EqualTo(1));
        Assert.That(offer.ChooseOneItem, Is.False);
        await actions.ExecuteAsync(player, 2, DailyRewardActions.ClaimOperation, today, Now).ConfigureAwait(false);
        Assert.That(player.Inventory!.Items.Single().Definition, Is.SameAs(day.Item));
    }

    private static uint OfferKey(DailyRewardDay reward) => reward.GetOfferKey(DailyRewardCalendar.Today(Now).Month);

    private static DailyRewardItem AddSkin(DailyRewardDay reward, int hours)
    {
        var definition = new BasicModel.ItemDefinition { Group = 12, Number = 320, Width = 1, Height = 1, Durability = 1, Name = "Skin" };
        definition.ItemSlot = new BasicModel.ItemSlotType { ItemSlots = { 247 } };
        var entry = new DailyRewardItem { Item = definition, ItemDurationHours = hours };
        reward.Items.Add(entry);
        return entry;
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
