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
using MUnique.OpenMU.GameServer;
using MUnique.OpenMU.GameServer.RemoteView.Inventory;
using MUnique.OpenMU.Persistence.InMemory;
using BasicModel = MUnique.OpenMU.Persistence.BasicModel;

/// <summary>Checks that shop quotes carry configured attributes without requiring the tooltip feature.</summary>
[TestFixture]
public class SkinShopAttributeCatalogTests
{
    /// <summary>Identical lists are shared, level values are retained, and empty lists are explicit.</summary>
    [Test]
    public void AttributesAreQuotedIndependentlyOfTooltips()
    {
        var (player, output) = CastleSiegeRemoteViewTestHelper.CreatePlayer();
        using (output)
        {
            var configuration = new BasicModel.GameConfiguration();
            Mock.Get((IGameServerContext)player.GameContext).Setup(context => context.Configuration).Returns(configuration);
            player.GameContext.FeaturePlugIns.AddPlugIn(new SkinShopFeaturePlugIn(), true);
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

            using var json = JsonDocument.Parse(SkinShopView.Serialize(player, 1, "Ready", true));
            var items = json.RootElement.GetProperty("Items");
            var pools = json.RootElement.GetProperty("AttributePools");
            Assert.That(items[0].GetProperty("AttributePool").GetInt32(), Is.EqualTo(items[1].GetProperty("AttributePool").GetInt32()));
            var line = pools[items[0].GetProperty("AttributePool").GetInt32()][0];
            Assert.That(line.GetProperty("Text").GetString(), Does.Contain("+5%"));
            Assert.That(line.GetProperty("ByLevel").GetProperty("7").GetString(), Does.Contain("+7%"));
            Assert.That(pools[items[2].GetProperty("AttributePool").GetInt32()].GetArrayLength(), Is.Zero);
        }
    }

    /// <summary>The complete default shop still fits the existing two-byte C2 packet length.</summary>
    [Test]
    public async Task DefaultCatalogFitsPacketAsync()
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
            player.GameContext.FeaturePlugIns.AddPlugIn(new SkinShopFeaturePlugIn(), true);
            var payload = SkinShopView.Serialize(player, 1, "Ready", true);
            Assert.That(payload.Length + 5, Is.LessThanOrEqualTo(ushort.MaxValue));
            TestContext.Out.WriteLine($"Shop catalog including attributes: {payload.Length + 5} bytes.");
        }
    }
}
