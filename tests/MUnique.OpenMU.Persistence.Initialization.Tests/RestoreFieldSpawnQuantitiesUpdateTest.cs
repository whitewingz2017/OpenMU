// <copyright file="RestoreFieldSpawnQuantitiesUpdateTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Tests;

using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Persistence.Initialization.Updates;

/// <summary>
/// Tests the <see cref="RestoreFieldSpawnQuantitiesUpdatePlugIn"/>.
/// </summary>
[TestFixture]
internal class RestoreFieldSpawnQuantitiesUpdateTest
{
    /// <summary>
    /// Tests that the update sets the quantities of the field monster spawns back to the ones of a new
    /// database, keeps the ones of the skipped maps, and that applying it twice doesn't change anything.
    /// </summary>
    [Test]
    public async Task UpdateRestoresDefaultQuantitiesAsync()
    {
        var (context, gameConfiguration) = await TestGameConfiguration.CreateSeason6Async().ConfigureAwait(false);
        using var _ = context;
        var spawns = gameConfiguration.Maps
            .SelectMany(map => map.MonsterSpawns.Select(spawn => (Map: map, Spawn: spawn)))
            .Where(entry => entry.Spawn.SpawnTrigger == SpawnTrigger.Automatic
                            && entry.Spawn.MonsterDefinition?.ObjectKind == NpcObjectKind.Monster)
            .ToList();
        var defaults = spawns.ToDictionary(entry => entry.Spawn, entry => entry.Spawn.Quantity);

        // The state of a database whose spawns were thinned out.
        foreach (var (_, spawn) in spawns)
        {
            spawn.Quantity = 0;
        }

        var update = new RestoreFieldSpawnQuantitiesUpdatePlugIn();
        await update.ApplyUpdateAsync(context, gameConfiguration).ConfigureAwait(false);
        await update.ApplyUpdateAsync(context, gameConfiguration).ConfigureAwait(false);

        Assert.That(spawns.Any(entry => defaults[entry.Spawn] > 1 && !RestoreFieldSpawnQuantitiesUpdatePlugIn.SkippedMapNumbers.Contains(entry.Map.Number)), Is.True);
        foreach (var (map, spawn) in spawns)
        {
            var expected = RestoreFieldSpawnQuantitiesUpdatePlugIn.SkippedMapNumbers.Contains(map.Number) ? 0 : defaults[spawn];
            Assert.That(spawn.Quantity, Is.EqualTo(expected), $"Map {map.Number}, spawn {spawn.GetId()}");
        }
    }
}
