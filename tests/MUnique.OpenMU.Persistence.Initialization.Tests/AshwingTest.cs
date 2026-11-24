// <copyright file="AshwingTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Tests;

using MUnique.OpenMU.Persistence.Initialization.Updates;
using MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Maps;

/// <summary>
/// Checks fresh, upgraded and already-upgraded installations.
/// </summary>
[TestFixture]
internal class AshwingTest
{
    /// <summary>
    /// Checks that one phoenix and one spawn survive installation and repeated updates.
    /// </summary>
    [Test]
    public async Task InstallAndRepairAsync()
    {
        var (context, config) = await TestGameConfiguration.CreateSeason6Async().ConfigureAwait(false);
        using var _ = context;
        var map = config.Maps.Single(m => m.Number == Icarus.Number);
        var monster = config.Monsters.Single(m => m.Number == Ashwing.Number);
        var spawn = map.MonsterSpawns.Single(s => s.MonsterDefinition == monster);
        Assert.That((spawn.X1, spawn.Y1, spawn.Quantity), Is.EqualTo(((byte)47, (byte)189, (short)1)));
        map.MonsterSpawns.Remove(spawn);
        config.Monsters.Remove(monster);
        await context.DeleteAsync(spawn).ConfigureAwait(false);
        await context.DeleteAsync(monster).ConfigureAwait(false);
        var update = new AshwingUpdatePlugIn();
        await update.ApplyUpdateAsync(context, config).ConfigureAwait(false);
        await update.ApplyUpdateAsync(context, config).ConfigureAwait(false);
        var restored = config.Monsters.Single(m => m.Number == Ashwing.Number);
        Assert.That(restored.AttackSkill?.Number, Is.EqualTo(4));
        Assert.That(map.MonsterSpawns.Single(s => s.MonsterDefinition == restored).Quantity, Is.EqualTo(1));
    }
}
