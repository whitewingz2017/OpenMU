// <copyright file="JewelStatisticsTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.PlugIns.GameEvents;
using MUnique.OpenMU.Persistence;
using MUnique.OpenMU.Persistence.InMemory;
using Basic = MUnique.OpenMU.Persistence.BasicModel;

/// <summary>Checks jewel normalization, ownership and cross-context counters.</summary>
[TestFixture]
public class JewelStatisticsTests
{
    /// <summary>Counts all slots and bundle sizes, without counting expired or unrelated items.</summary>
    [Test]
    public void BundlesAndExpansionSlotsUseSingleJewelUnits()
    {
        var config = new Basic.GameConfiguration();
        var bless = new Basic.ItemDefinition { Group = 14, Number = 13 };
        var bundle = new Basic.ItemDefinition { Group = 12, Number = 30 };
        config.JewelMixes.Add(new Basic.JewelMix { SingleJewel = bless, MixedJewel = bundle });
        var now = DateTime.UtcNow;
        Item[] items = [
            new Basic.Item { Definition = bless, ItemSlot = 200 },
            new Basic.Item { Definition = bundle, Level = 0 },
            new Basic.Item { Definition = bundle, Level = 1 },
            new Basic.Item { Definition = bundle, Level = 2 },
            new Basic.Item { Definition = bless, ExpiresAtUtc = now.AddSeconds(-1) },
            new Basic.Item { Definition = bundle, Level = 3 },
            new Basic.Item { Definition = new Basic.ItemDefinition { Group = 0, Number = 0 } },
        ];
        Assert.That(JewelStatistics.Count(items, config, [7181], now), Is.EqualTo(new[] { new JewelAmount(7181, 61) }));
    }

    /// <summary>The counter survives disposal of contexts and concurrent increments from several servers.</summary>
    [Test]
    public async Task ConcurrentCountersShareOneDatabaseAsync()
    {
        var provider = new InMemoryPersistenceContextProvider();
        await Task.WhenAll(Enumerable.Range(0, 500).Select(_ => Task.Run(async () =>
        {
            using var context = provider.CreateNewPlayerContext(new Basic.GameConfiguration());
            await context.RecordJewelDropAsync(7181, 1, DateTime.UtcNow).ConfigureAwait(false);
        }))).ConfigureAwait(false);
        using var reader = provider.CreateNewPlayerContext(new Basic.GameConfiguration());
        var result = await reader.GetJewelStatisticsAsync([7181, 7182]).ConfigureAwait(false);
        Assert.That(result.Produced, Is.EquivalentTo(new[] { new JewelAmount(7181, 500), new JewelAmount(7182, 0) }));
        using var independent = new InMemoryPersistenceContextProvider().CreateNewPlayerContext(new Basic.GameConfiguration());
        Assert.That((await independent.GetJewelStatisticsAsync([7181]).ConfigureAwait(false)).Produced.Single().Amount, Is.Zero);
    }

    /// <summary>Accounts remain counted while offline; bank entries are counted once per account.</summary>
    [Test]
    public async Task OfflineInventoryVaultAndBankAreIncludedAsync()
    {
        var provider = new InMemoryPersistenceContextProvider();
        using (var context = provider.CreateNewPlayerContext(new Basic.GameConfiguration()))
        {
            var account = context.CreateNew<Account>();
            account.JewelBankBalances = "{\"7181:0\":100}";
            account.Vault = context.CreateNew<ItemStorage>();
            var character = context.CreateNew<Character>();
            character.Inventory = context.CreateNew<ItemStorage>();
            account.Characters.Add(character);
            var definition = context.CreateNew<MUnique.OpenMU.DataModel.Configuration.Items.ItemDefinition>();
            definition.Group = 14;
            definition.Number = 13;
            var vaultJewel = context.CreateNew<Item>();
            vaultJewel.Definition = definition;
            vaultJewel.ItemSlot = 200;
            account.Vault.Items.Add(vaultJewel);
            var inventoryJewel = context.CreateNew<Item>();
            inventoryJewel.Definition = definition;
            character.Inventory.Items.Add(inventoryJewel);
            await context.SaveChangesAsync().ConfigureAwait(false);
        }

        using var reader = provider.CreateNewPlayerContext(new Basic.GameConfiguration());
        var result = await reader.GetJewelStatisticsAsync([7181]).ConfigureAwait(false);
        Assert.That(result.Owners.SelectMany(owner => owner.Amounts).Sum(amount => amount.Amount), Is.EqualTo(102));
        Assert.That(result.Produced.Single().Amount, Is.Zero, "existing holdings are not historic production");
    }

    /// <summary>A jewel is recorded while it is still on the ground, before any pickup.</summary>
    [Test]
    public async Task GroundDropIsRecordedBeforePickupAsync()
    {
        var player = await PlayerTestHelper.CreatePlayerAsync(GameContextTestHelper.CreateGameContext()).ConfigureAwait(false);
        await player.PlayerState.TryAdvanceToAsync(PlayerState.EnteredWorld).ConfigureAwait(false);
        var item = new Basic.Item { Definition = new Basic.ItemDefinition { Group = 14, Number = 13 } };
        await using var dropped = new DroppedItem(item, player.Position, player.CurrentMap!, null, [player]);
        await player.CurrentMap!.AddAsync(dropped).ConfigureAwait(false);
        await new JewelDropStatisticsPlugIn().MonsterItemDroppedAsync(null!, player, dropped).ConfigureAwait(false);
        using var reader = player.GameContext.PersistenceContextProvider.CreateNewPlayerContext(player.GameContext.Configuration);
        var stats = await reader.GetJewelStatisticsAsync([7181]).ConfigureAwait(false);
        Assert.That(stats.Produced.Single().Amount, Is.EqualTo(1));
        Assert.That(player.Inventory!.Items, Does.Not.Contain(item));
    }

    /// <summary>Untrusted or corrupt reporting ledgers cannot produce negative balances or crash the status.</summary>
    /// <param name="json">The invalid ledger.</param>
    [TestCase("[]")]
    [TestCase("not json")]
    [TestCase("{\"7181:0\":-5}")]
    [TestCase("{\"7181:0\":\"bad\"}")]
    public void InvalidBankValuesDoNotCreateJewels(string json)
        => Assert.That(JewelBankLedger.Read(json, [7181]).Sum(amount => amount.Amount), Is.Zero);
}
