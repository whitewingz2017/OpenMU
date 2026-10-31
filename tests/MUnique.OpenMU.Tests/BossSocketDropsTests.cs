// <copyright file="BossSocketDropsTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic.PlugIns;
using Basic = MUnique.OpenMU.Persistence.BasicModel;

[TestFixture]
public class BossSocketDropsTests
{
    [TestCase(0)]
    [TestCase(3)]
    public void NormalMonstersCannotDropSocketEquipment(int sockets)
    {
        var item = new Basic.Item { Definition = new Basic.ItemDefinition { MaximumSockets = 5 }, SocketCount = sockets };
        Assert.That(new BossSocketDropsPlugIn().CanDrop(new Basic.MonsterDefinition { Number = 100 }, new Basic.MonsterSpawnArea(), 0, item), Is.False);
    }

    [TestCase(135)]
    [TestCase(79)]
    [TestCase(275)]
    [TestCase(459)]
    [TestCase(504)]
    [TestCase(511)]
    [TestCase(529)]
    public void ConfiguredBossCanDropSocketEquipment(short number)
    {
        var item = new Basic.Item { Definition = new Basic.ItemDefinition { MaximumSockets = 5 } };
        Assert.That(new BossSocketDropsPlugIn().CanDrop(new Basic.MonsterDefinition { Number = number }, new Basic.MonsterSpawnArea(), 0, item), Is.True);
    }

    [TestCase(9, 10, true)]
    [TestCase(32, 10, true)]
    [TestCase(9, 1, false)]
    [TestCase(32, 3, false)]
    public void DevilSquareOnlyAllowsBossWave(short map, byte wave, bool expected)
    {
        var spawn = new Basic.MonsterSpawnArea { SpawnTrigger = SpawnTrigger.OnceAtWaveStart, WaveNumber = wave };
        var item = new Basic.Item { Definition = new Basic.ItemDefinition { MaximumSockets = 5 } };
        Assert.That(new BossSocketDropsPlugIn().CanDrop(new Basic.MonsterDefinition { Number = 49 }, spawn, map, item), Is.EqualTo(expected));
    }

    [Test]
    public void NormalItemsAndConfiguredCustomBossAreSupported()
    {
        var plugin = new BossSocketDropsPlugIn { Configuration = new BossSocketDropsConfiguration() };
        var monster = new Basic.MonsterDefinition { Number = 999 };
        var spawn = new Basic.MonsterSpawnArea();
        var item = new Basic.Item { Definition = new Basic.ItemDefinition() };
        Assert.That(plugin.CanDrop(monster, spawn, 0, item), Is.True);
        item.Definition.MaximumSockets = 5;
        Assert.That(plugin.CanDrop(monster, spawn, 0, item), Is.False);
        plugin.Configuration.BossMonsterNumbers.Add(999);
        Assert.That(plugin.CanDrop(monster, spawn, 0, item), Is.True);
    }
}
