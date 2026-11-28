// <copyright file="SkinShopAttributeCatalogTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.Cosmetics;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.GameServer;
using MUnique.OpenMU.GameServer.RemoteView.Inventory;
using MUnique.OpenMU.Persistence.InMemory;
using BasicModel = MUnique.OpenMU.Persistence.BasicModel;

/// <summary>Checks that shop quotes carry configured attributes without requiring the tooltip feature.</summary>
[TestFixture]
public class SkinShopAttributeCatalogTests
{
    /// <summary>Entering the world receives effects even before the shop is opened or any skins are owned.</summary>
    [Test]
    public async Task CosmeticSnapshotCarriesEffectsWithoutShopOrOwnershipAsync()
    {
        var (player, output) = CastleSiegeRemoteViewTestHelper.CreatePlayer();
        using (output)
        {
            var configuration = new BasicModel.GameConfiguration();
            Mock.Get((IGameServerContext)player.GameContext).Setup(context => context.Configuration).Returns(configuration);
            var slot = new BasicModel.ItemSlotType();
            slot.ItemSlots.Add(241);
            configuration.Items.Add(new BasicModel.ItemDefinition { Group = 8, Number = 320, ItemSlot = slot });
            var settings = new SkinShopSettings();
            settings.Items[(8 * 512) + 320] = new SkinShopItemSettings { Enabled = false, ExcellentEffect = true, AuraColor = SkinAuraColor.Gold };
            player.GameContext.FeaturePlugIns.AddPlugIn(new SkinShopFeaturePlugIn { Configuration = settings }, true);
            await new CosmeticArmorView(player).ShowAsync(1, "Ready").ConfigureAwait(false);
            using var json = JsonDocument.Parse(output.ToArray().AsMemory(5));
            Assert.That(json.RootElement.GetProperty("ExcellentEffectItems").EnumerateArray().Select(item => item.GetInt32()), Is.EqualTo(new[] { (8 * 512) + 320 }));
            Assert.That(json.RootElement.GetProperty("Skins").GetArrayLength(), Is.Zero);
            Assert.That(json.RootElement.GetProperty("SkinAuraColors")[0].EnumerateArray().Select(field => field.GetInt32()),
                Is.EqualTo(new[] { (8 * 512) + 320, (int)SkinAuraColor.Gold }));
        }
    }

    /// <summary>Identical lists are shared, level values are retained, and empty lists are explicit.</summary>
    [Test]
    public void AttributesAreQuotedIndependentlyOfTooltips()
    {
        var (player, output) = CastleSiegeRemoteViewTestHelper.CreatePlayer();
        using (output)
        {
            var configuration = new BasicModel.GameConfiguration();
            Mock.Get((IGameServerContext)player.GameContext).Setup(context => context.Configuration).Returns(configuration);
            var settings = new SkinShopSettings();
            player.GameContext.FeaturePlugIns.AddPlugIn(new SkinShopFeaturePlugIn { Configuration = settings }, true);
            for (short number = 320; number <= 322; number++)
            {
                var slot = new BasicModel.ItemSlotType();
                slot.ItemSlots.Add(241);
                var item = new BasicModel.ItemDefinition { Id = Guid.NewGuid(), Group = 8, Number = number, Name = "Armor", ItemSlot = slot };
                if (number < 322)
                {
                    var bonus = new BasicModel.ItemBasePowerUpDefinition
                    {
                        TargetAttribute = Stats.DefenseIgnoreChance,
                        AggregateType = AggregateType.AddRaw,
                        BaseValue = .05f,
                        BonusPerLevelTable = new BasicModel.ItemLevelBonusTable(),
                    };
                    bonus.BonusPerLevelTable.BonusPerLevel.Add(new BasicModel.LevelBonus { Level = 7, AdditionalValue = .02f });
                    item.BasePowerUpAttributes.Add(bonus);
                }

                configuration.Items.Add(item);
            }

            settings.Items[(8 * 512) + 321] = new SkinShopItemSettings { Enabled = false, ExcellentEffect = true };
            var restored = JsonSerializer.Deserialize<SkinShopSettings>(JsonSerializer.Serialize(settings))!;
            Assert.That(restored.ExcellentEffectItems(configuration.Items), Is.EqualTo(new[] { (8 * 512) + 321 }));
            Assert.That(JsonSerializer.Deserialize<SkinShopItemSettings>("{}")!.ExcellentEffect, Is.False);

            using var json = JsonDocument.Parse(SkinShopView.Serialize(player, 1, "Ready", true));
            var items = json.RootElement.GetProperty("Items");
            var pools = json.RootElement.GetProperty("AttributePools");
            Assert.That(json.RootElement.GetProperty("Pools").EnumerateArray().All(pool => pool.GetProperty("Bonuses").GetArrayLength() == 0), Is.True);
            Assert.That(items[0].GetProperty("AttributePool").GetInt32(), Is.EqualTo(items[1].GetProperty("AttributePool").GetInt32()));
            var line = pools[items[0].GetProperty("AttributePool").GetInt32()][0];
            Assert.That(line.GetProperty("Text").GetString(), Does.Contain("+5%"));
            Assert.That(line.GetProperty("ByLevel").GetProperty("7").GetString(), Does.Contain("+7%"));
            Assert.That(pools[items[2].GetProperty("AttributePool").GetInt32()].GetArrayLength(), Is.Zero);
            Assert.That(json.RootElement.GetProperty("ExcellentEffectItems").EnumerateArray().Select(item => item.GetInt32()), Is.EqualTo(new[] { (8 * 512) + 321 }));
            using var compact = JsonDocument.Parse(SkinShopView.Serialize(player, 1, "Ready", true, true));
            var compactItem = compact.RootElement.GetProperty("Items")[0];
            Assert.That(compactItem.EnumerateArray().Select(field => field.GetRawText()),
                Is.EqualTo(items[0].EnumerateObject().Select(field => field.Value.GetRawText())));
            Assert.That(compact.RootElement.GetProperty("AttributePools").GetRawText(), Is.EqualTo(pools.GetRawText()));
            using var confirmation = JsonDocument.Parse(SkinShopView.Serialize(player, 2, "Ready", false));
            Assert.That(confirmation.RootElement.GetProperty("ExcellentEffectItems").GetArrayLength(), Is.EqualTo(1));
        }
    }

    /// <summary>The complete default shop still fits the existing two-byte C2 packet length.</summary>
    /// <param name="compact">Whether the client requests compact rows.</param>
    [TestCase(false)]
    [TestCase(true)]
    public async Task DefaultCatalogFitsPacketAsync(bool compact)
    {
        var provider = new InMemoryPersistenceContextProvider();
        var initializer = new Persistence.Initialization.VersionSeasonSix.DataInitialization(provider, NullLoggerFactory.Instance);
        await initializer.CreateInitialDataAsync(1, true).ConfigureAwait(false);
        using var context = provider.CreateNewContext();
        var configuration = (await context.GetAsync<MUnique.OpenMU.DataModel.Configuration.GameConfiguration>().ConfigureAwait(false)).Single();
        var (player, output) = CastleSiegeRemoteViewTestHelper.CreatePlayer();
        using (output)
        {
            Mock.Get((IGameServerContext)player.GameContext).Setup(game => game.Configuration).Returns(configuration);
            var settings = new SkinShopSettings();
            foreach (var definition in configuration.Items.Where(FenrirMountRules.IsShopItem))
            {
                settings.Items[(definition.Group * 512) + definition.Number] = new SkinShopItemSettings { ExcellentEffect = true, AuraColor = SkinAuraColor.Blue };
            }

            player.GameContext.FeaturePlugIns.AddPlugIn(new SkinShopFeaturePlugIn { Configuration = settings }, true);
            player.GameContext.PlugInManager.RegisterPlugIn<IPeriodicTaskPlugIn, MUnique.OpenMU.GameLogic.PlugIns.ItemExpirationPlugIn>();
            settings.RentalOneDayPrice = 25;
            settings.RentalThreeDaysPrice = 65;
            settings.RentalSevenDaysPrice = 125;
            var payload = SkinShopView.Serialize(player, 1, "Ready", true, compact);
            Assert.That(payload.Length + 5, Is.LessThanOrEqualTo(ushort.MaxValue));
            using var json = JsonDocument.Parse(payload);
            Assert.That(json.RootElement.GetProperty("RentalOneDayPrice").GetInt32(), Is.EqualTo(25));
            Assert.That(json.RootElement.GetProperty("RentableItems").GetArrayLength(), Is.GreaterThan(0));
            var wings = json.RootElement.GetProperty("Items").EnumerateArray()
                .Select(item => (compact ? item[0] : item.GetProperty("ItemType")).GetInt32())
                .Where(type => type is >= (12 * 512) + 400 and <= (12 * 512) + 504);
            Assert.That(wings, Is.EqualTo(compact ? Enumerable.Range((12 * 512) + 400, 105) : []));
            TestContext.Out.WriteLine($"Shop catalog including attributes (compact: {compact}): {payload.Length + 5} bytes.");
            foreach (var item in settings.Items.Values)
            {
                item.RentalOneDayPrice = int.MaxValue;
                item.RentalThreeDaysPrice = int.MaxValue;
                item.RentalSevenDaysPrice = int.MaxValue;
            }

            var largeQuotes = SkinShopView.Serialize(player, 2, "Ready", true, compact);
            Assert.That(largeQuotes.Length + 5, Is.LessThanOrEqualTo(ushort.MaxValue));
            using var largeCatalog = JsonDocument.Parse(largeQuotes);
            Assert.That(largeCatalog.RootElement.GetProperty("RentableItems").GetArrayLength(), Is.Zero);
            Assert.That(largeCatalog.RootElement.GetProperty("RentalQuotes").GetArrayLength(),
                Is.EqualTo(compact ? largeCatalog.RootElement.GetProperty("Items").GetArrayLength() : 0));
            TestContext.Out.WriteLine($"Shop catalog with item rental quotes (compact: {compact}): {largeQuotes.Length + 5} bytes.");
        }
    }
}
