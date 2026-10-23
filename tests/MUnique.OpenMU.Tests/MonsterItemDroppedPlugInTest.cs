// <copyright file="MonsterItemDroppedPlugInTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using System.Runtime.InteropServices;
using Moq;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.NPC;
using MUnique.OpenMU.GameLogic.PlugIns;
using BasicModel = MUnique.OpenMU.Persistence.BasicModel;
using MonsterAttribute = MUnique.OpenMU.Persistence.BasicModel.MonsterAttribute;
using MonsterDefinition = MUnique.OpenMU.Persistence.BasicModel.MonsterDefinition;

/// <summary>
/// Tests for the <see cref="IMonsterItemDroppedPlugIn"/>.
/// </summary>
[TestFixture]
public class MonsterItemDroppedPlugInTest
{
    /// <summary>
    /// Tests that the item which a killed monster dropped is reported with the monster and the killer.
    /// </summary>
    [Test]
    public async Task DroppedItemIsReportedAsync()
    {
        var gameContext = GameContextTestHelper.CreateGameContext();
        var plugIn = new RecordingPlugIn();
        gameContext.PlugInManager.RegisterPlugInAtPlugInPoint<IMonsterItemDroppedPlugIn>(plugIn);
        var item = new BasicModel.Item { Definition = new BasicModel.ItemDefinition() };
        var dropGenerator = new Mock<IDropGenerator>();
        dropGenerator.Setup(g => g.GenerateItemDropsAsync(It.IsAny<DataModel.Configuration.MonsterDefinition>(), It.IsAny<int>(), It.IsAny<Player>()))
            .Returns(ValueTask.FromResult<(IEnumerable<Item> Items, uint? Money)>((new[] { item }, null)));
        var killer = await PlayerTestHelper.CreatePlayerAsync(gameContext).ConfigureAwait(false);
        var monster = await CreateMonsterAsync(gameContext, dropGenerator.Object).ConfigureAwait(false);

        await monster.ReflectDamageAsync(killer, 1_000_000).ConfigureAwait(false);

        // The drop happens delayed, after the death animation.
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (plugIn.Drops.IsEmpty && DateTime.UtcNow < deadline)
        {
            await Task.Delay(50).ConfigureAwait(false);
        }

        Assert.That(plugIn.Drops, Has.Count.EqualTo(1));
        var (reportedMonster, reportedKiller, droppedItem) = plugIn.Drops.Single();
        Assert.That(reportedMonster, Is.SameAs(monster));
        Assert.That(reportedKiller, Is.SameAs(killer));
        Assert.That(droppedItem.Item, Is.SameAs(item));
    }

    /// <summary>
    /// Checks that the same Zen calculation precedes ground drops and direct payments.
    /// </summary>
    /// <param name="shouldDropMoney">Whether to put the money on the ground.</param>
    [TestCase(true)]
    [TestCase(false)]
    public async Task LevelBasedMoneyUsesBothPayoutPathsAsync(bool shouldDropMoney)
    {
        var gameContext = GameContextTestHelper.CreateGameContext();
        gameContext.Configuration.ShouldDropMoney = shouldDropMoney;
        gameContext.Configuration.GlobalBaseAttributeValues.Add(new BasicModel.ConstValueAttribute(1, Stats.MoneyAmountRate, AggregateType.AddRaw));
        gameContext.PlugInManager.RegisterPlugInAtPlugInPoint<IMonsterMoneyAmountPlugIn>(new LevelBasedMoneyPlugIn
        {
            Configuration = new() { BaseZen = 0, ZenPerMonsterLevel = 7, MaximumRandomZen = 0 },
        });
        var recorder = new RecordingMoneyPlugIn();
        gameContext.PlugInManager.RegisterPlugInAtPlugInPoint<IMonsterMoneyDroppedPlugIn>(recorder);
        var generator = new Mock<IDropGenerator>();
        generator.Setup(g => g.GenerateItemDropsAsync(It.IsAny<DataModel.Configuration.MonsterDefinition>(), It.IsAny<int>(), It.IsAny<Player>()))
            .Returns(ValueTask.FromResult<(IEnumerable<Item> Items, uint? Money)>((Array.Empty<Item>(), 1_000_000)));
        var killer = await PlayerTestHelper.CreatePlayerAsync(gameContext).ConfigureAwait(false);
        var originalMoney = killer.Money;
        var monster = await CreateMonsterAsync(gameContext, generator.Object).ConfigureAwait(false);
        await monster.ReflectDamageAsync(killer, 1_000_000).ConfigureAwait(false);

        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (recorder.Drops.IsEmpty && killer.Money == originalMoney && DateTime.UtcNow < deadline)
        {
            await Task.Delay(50).ConfigureAwait(false);
        }

        if (shouldDropMoney)
        {
            Assert.That(recorder.Drops, Has.Count.EqualTo(1));
            var drop = recorder.Drops.Single();
            Assert.That(drop.Amount, Is.EqualTo(700));
            Assert.That(await drop.TryPickUpByAsync(killer).ConfigureAwait(false), Is.True);
        }

        Assert.That(killer.Money - originalMoney, Is.EqualTo(700));
    }

    private static async ValueTask<Monster> CreateMonsterAsync(IGameContext gameContext, IDropGenerator dropGenerator)
    {
        // A unique id, because the attributes of monster definitions are cached by their id.
        var monsterDefinition = new MonsterDefinition { Id = Guid.NewGuid(), ObjectKind = NpcObjectKind.Monster };
        monsterDefinition.Attributes.Add(new MonsterAttribute { AttributeDefinition = Stats.MaximumHealth, Value = 100 });
        monsterDefinition.Attributes.Add(new MonsterAttribute { AttributeDefinition = Stats.Level, Value = 100 });
        var map = await gameContext.GetMapAsync(0).ConfigureAwait(false);
        var spawnArea = new MonsterSpawnArea
        {
            MonsterDefinition = monsterDefinition,
            GameMap = map!.Definition,
            X1 = 100,
            Y1 = 100,
            X2 = 100,
            Y2 = 100,
            Quantity = 1,
        };

        var monster = new Monster(
            spawnArea,
            monsterDefinition,
            map,
            dropGenerator,
            new Mock<INpcIntelligence>().Object,
            gameContext.PlugInManager,
            gameContext.PathFinderPool);
        monster.Initialize();
        await map.AddAsync(monster).ConfigureAwait(false);
        return monster;
    }

    [Guid("2FA16252-B785-431F-AC7C-88DD28B876F8")]
    private sealed class RecordingMoneyPlugIn : IMonsterMoneyDroppedPlugIn
    {
        public System.Collections.Concurrent.ConcurrentBag<DroppedMoney> Drops { get; } = new();

        public ValueTask MonsterMoneyDroppedAsync(AttackableNpcBase monster, Player killer, DroppedMoney droppedMoney)
        {
            this.Drops.Add(droppedMoney);
            return ValueTask.CompletedTask;
        }
    }

    [Guid("9635516F-6DC6-446E-88D3-8DD33CA5C42D")]
    private sealed class RecordingPlugIn : IMonsterItemDroppedPlugIn
    {
        public System.Collections.Concurrent.ConcurrentBag<(AttackableNpcBase Monster, Player Killer, DroppedItem Item)> Drops { get; } = new();

        public ValueTask MonsterItemDroppedAsync(AttackableNpcBase monster, Player killer, DroppedItem droppedItem)
        {
            this.Drops.Add((monster, killer, droppedItem));
            return ValueTask.CompletedTask;
        }
    }
}
