// <copyright file="SparseFieldSpawnsUpdateTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Tests;

using Microsoft.Extensions.Logging.Abstractions;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Persistence.Initialization.Updates;
using MUnique.OpenMU.Persistence.InMemory;

/// <summary>
/// Tests that the optional sparse field spawns update thins out the floors of the field maps only.
/// </summary>
[TestFixture]
internal class SparseFieldSpawnsUpdateTest
{
    /// <summary>
    /// Field maps are thinned out, while every monster type of a floor keeps at least one monster.
    /// </summary>
    [Test]
    public async Task FieldMapsAreThinnedOutAsync()
    {
        var (context, configuration) = await CreateSeason6ConfigurationAsync().ConfigureAwait(false);
        using var _ = context;
        var before = CountFieldMonstersPerMap(configuration);

        await new SparseFieldSpawnsUpdatePlugIn().ApplyUpdateAsync(context, configuration).ConfigureAwait(false);

        var after = CountFieldMonstersPerMap(configuration);
        foreach (var (mapNumber, count) in before)
        {
            TestContext.Out.WriteLine($"Map {mapNumber}: {count} -> {after[mapNumber]}");
            Assert.That(after[mapNumber], Is.LessThanOrEqualTo(count));
        }

        Assert.That(after.Values.Sum(), Is.LessThan(before.Values.Sum() / 3));
        foreach (var map in configuration.Maps)
        {
            foreach (var (_, floorSpawns) in GetFloors(configuration, map))
            {
                foreach (var monsterSpawns in floorSpawns.GroupBy(s => s.MonsterDefinition))
                {
                    Assert.That(monsterSpawns.Sum(s => (int)s.Quantity), Is.GreaterThan(0), $"No {monsterSpawns.Key!.Designation} left on map {map.Number}");
                }
            }
        }
    }

    /// <summary>
    /// Lost Tower 7 keeps about <see cref="SparseFieldSpawnsUpdatePlugIn.MonstersPerFloor"/> monsters.
    /// </summary>
    [Test]
    public async Task LostTowerSevenKeepsAFloorOfMonstersAsync()
    {
        var (context, configuration) = await CreateSeason6ConfigurationAsync().ConfigureAwait(false);
        using var _ = context;

        await new SparseFieldSpawnsUpdatePlugIn().ApplyUpdateAsync(context, configuration).ConfigureAwait(false);

        var lostTower = configuration.Maps.First(m => m.Number == 4);
        var floors = GetFloors(configuration, lostTower);
        Assert.That(floors, Has.Count.EqualTo(7));

        // The warp destination of Lost Tower 7 is at 8, 85.
        var lostTowerSeven = floors.Single(floor => floor.Gate is { X1: 8, Y1: 85 }).Spawns.Sum(s => (int)s.Quantity);
        Assert.That(lostTowerSeven, Is.InRange(30, SparseFieldSpawnsUpdatePlugIn.MonstersPerFloor));
    }

    /// <summary>
    /// Spawns of event maps, invasions and NPCs stay untouched.
    /// </summary>
    [Test]
    public async Task OtherSpawnsStayUntouchedAsync()
    {
        var (context, configuration) = await CreateSeason6ConfigurationAsync().ConfigureAwait(false);
        using var _ = context;
        var otherSpawns = configuration.Maps
            .SelectMany(map => map.MonsterSpawns.Where(s => !SparseFieldSpawnsUpdatePlugIn.IsFieldMonsterSpawn(map, s)))
            .ToDictionary(s => s, s => s.Quantity);

        await new SparseFieldSpawnsUpdatePlugIn().ApplyUpdateAsync(context, configuration).ConfigureAwait(false);

        Assert.That(otherSpawns.Where(pair => pair.Key.Quantity != pair.Value), Is.Empty);
    }

    /// <summary>
    /// Applying the update again, e.g. over the quantities which version 1 left, leads to the same state.
    /// </summary>
    [Test]
    public async Task ReapplicationLeadsToTheSameStateAsync()
    {
        var (context, configuration) = await CreateSeason6ConfigurationAsync().ConfigureAwait(false);
        using var _ = context;
        var update = new SparseFieldSpawnsUpdatePlugIn();
        await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        var quantities = configuration.Maps.SelectMany(m => m.MonsterSpawns).ToDictionary(s => s, s => s.Quantity);
        foreach (var map in configuration.Maps)
        {
            foreach (var spawn in map.MonsterSpawns.Where(s => SparseFieldSpawnsUpdatePlugIn.IsFieldMonsterSpawn(map, s)))
            {
                spawn.Quantity = SparseFieldSpawnsUpdatePlugIn.IsSpot(spawn) ? (short)(spawn.X1 % 2) : (short)3;
            }
        }

        await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);

        Assert.That(quantities.Where(pair => pair.Key.Quantity != pair.Value), Is.Empty);
    }

    private static IReadOnlyList<(ExitGate? Gate, List<MonsterSpawnArea> Spawns)> GetFloors(GameConfiguration configuration, GameMapDefinition map)
        => SparseFieldSpawnsUpdatePlugIn.GetFloors(
            SparseFieldSpawnsUpdatePlugIn.GetFloorGates(configuration, map),
            map.MonsterSpawns.Where(s => SparseFieldSpawnsUpdatePlugIn.IsFieldMonsterSpawn(map, s)).ToList());

    private static Dictionary<short, int> CountFieldMonstersPerMap(GameConfiguration configuration)
        => configuration.Maps
            .Where(map => !SparseFieldSpawnsUpdatePlugIn.ExcludedMapNumbers.Contains(map.Number))
            .GroupBy(map => map.Number)
            .ToDictionary(
                group => group.Key,
                group => group.SelectMany(map => map.MonsterSpawns.Where(s => SparseFieldSpawnsUpdatePlugIn.IsFieldMonsterSpawn(map, s))).Sum(s => (int)s.Quantity));

    private static async Task<(IContext Context, GameConfiguration GameConfiguration)> CreateSeason6ConfigurationAsync()
    {
        var contextProvider = new InMemoryPersistenceContextProvider();
        var dataInitialization = new VersionSeasonSix.DataInitialization(contextProvider, new NullLoggerFactory());
        await dataInitialization.CreateInitialDataAsync(1, true).ConfigureAwait(false);

        var context = contextProvider.CreateNewContext();
        var gameConfiguration = (await context.GetAsync<GameConfiguration>().ConfigureAwait(false)).First();
        return (context, gameConfiguration);
    }
}
