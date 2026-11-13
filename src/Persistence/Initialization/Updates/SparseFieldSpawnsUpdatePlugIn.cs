// <copyright file="SparseFieldSpawnsUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Persistence.Initialization.Properties;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Thins out the automatic monster spawns of the field maps for a "hard" server style,
/// where each floor has a limited number of single monsters, spread evenly over the floor.
/// Spawns of event and quest maps stay untouched.
/// </summary>
/// <remarks>
/// A floor is the part of a map which belongs to one of its warp destinations, e.g. Lost Tower 7.
/// Single spawn points are switched off by setting their quantity to zero instead of deleting them,
/// so a later update can bring them back.
/// </remarks>
[PlugIn]
[Display(Name = nameof(PlugInResources.SparseFieldSpawnsUpdatePlugIn_Name), Description = nameof(PlugInResources.SparseFieldSpawnsUpdatePlugIn_Description), ResourceType = typeof(PlugInResources))]
[Guid("D3401951-23EF-44EA-9CD8-191624442E8A")]
public sealed class SparseFieldSpawnsUpdatePlugIn : UpdatePlugInBase
{
    /// <summary>
    /// The number of monsters which spawn on a floor.
    /// </summary>
    internal const int MonstersPerFloor = 40;

    /// <summary>
    /// The minimum distance between two active spawn points; closer spawn points are not activated
    /// to fill up <see cref="MonstersPerFloor"/>.
    /// </summary>
    internal const int MinimumSpotDistance = 3;

    /// <summary>
    /// The maximum distance between two spawns of the same connected region of a floor.
    /// </summary>
    internal const int FloorLinkDistance = 8;

    /// <summary>
    /// The numbers of the event and quest maps, whose spawns stay untouched.
    /// </summary>
    internal static readonly IReadOnlySet<short> ExcludedMapNumbers = new HashSet<short>
    {
        9, 32, // Devil Square
        11, 12, 13, 14, 15, 16, 17, 52, // Blood Castle
        18, 19, 20, 21, 22, 23, 53, // Chaos Castle
        24, 25, 26, 27, 28, 29, 36, // Kalima
        34, // Crywolf Fortress
        39, // Kanturu event
        41, 42, // Barracks of Balgass, Balgass Refuge
        45, 46, 47, 48, 49, 50, // Illusion Temple
        58, // Raklion boss
        65, 66, 67, 68, // Doppelgaenger
        69, 70, 71, 72, // Fortress of Imperial Guardian
    };

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override string Name => PlugInResources.SparseFieldSpawnsUpdatePlugIn_Name;

    /// <inheritdoc />
    public override string Description => PlugInResources.SparseFieldSpawnsUpdatePlugIn_Description;

    /// <inheritdoc />
    public override int Version => 2;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 11, 13, 0, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    public override DateTime UpdatedAt => new(2026, 11, 13, 16, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    public override bool IsMandatory => false;

    /// <summary>
    /// Determines whether the spawn is affected by this update.
    /// </summary>
    /// <param name="map">The map of the spawn.</param>
    /// <param name="spawn">The spawn.</param>
    /// <returns><c>true</c>, if the spawn is an automatic monster spawn on a field map.</returns>
    internal static bool IsFieldMonsterSpawn(GameMapDefinition map, MonsterSpawnArea spawn)
        => !ExcludedMapNumbers.Contains(map.Number)
           && spawn.SpawnTrigger == SpawnTrigger.Automatic
           && spawn.MonsterDefinition?.ObjectKind == NpcObjectKind.Monster;

    /// <summary>
    /// Determines whether the spawn is a single spawn point instead of an area.
    /// </summary>
    /// <param name="spawn">The spawn.</param>
    /// <returns><c>true</c>, if the spawn is a single spawn point.</returns>
    internal static bool IsSpot(MonsterSpawnArea spawn)
        => spawn.X1 == spawn.X2 && spawn.Y1 == spawn.Y2;

    /// <summary>
    /// Gets the warp destination gates of the map, which define its floors.
    /// </summary>
    /// <param name="gameConfiguration">The game configuration.</param>
    /// <param name="map">The map.</param>
    /// <returns>The warp destination gates of the map.</returns>
    internal static IReadOnlyList<ExitGate> GetFloorGates(GameConfiguration gameConfiguration, GameMapDefinition map)
        => gameConfiguration.WarpList
            .Select(warp => warp.Gate)
            .OfType<ExitGate>()
            .Where(gate => Equals(gate.Map, map))
            .Distinct()
            .ToList();

    /// <summary>
    /// Splits the field spawns of a map into its floors. Spawns which are linked by a distance of up to
    /// <see cref="FloorLinkDistance"/> belong to the same region, and each region belongs to the floor of its closest floor gate.
    /// </summary>
    /// <param name="floorGates">The floor gates of the map.</param>
    /// <param name="fieldSpawns">The field spawns of the map.</param>
    /// <returns>The spawns per floor gate; the gate is <c>null</c>, if the map has no floor gates.</returns>
    internal static IReadOnlyList<(ExitGate? Gate, List<MonsterSpawnArea> Spawns)> GetFloors(IReadOnlyList<ExitGate> floorGates, IReadOnlyList<MonsterSpawnArea> fieldSpawns)
    {
        if (floorGates.Count == 0)
        {
            return [(null, fieldSpawns.ToList())];
        }

        var regionOf = Enumerable.Range(0, fieldSpawns.Count).ToArray();
        int FindRegion(int index)
        {
            while (regionOf[index] != index)
            {
                regionOf[index] = regionOf[regionOf[index]];
                index = regionOf[index];
            }

            return index;
        }

        for (var i = 0; i < fieldSpawns.Count; i++)
        {
            for (var j = 0; j < i; j++)
            {
                if (GetDistance(fieldSpawns[i], fieldSpawns[j]) <= FloorLinkDistance)
                {
                    regionOf[FindRegion(i)] = FindRegion(j);
                }
            }
        }

        var floors = floorGates.ToDictionary(gate => gate, _ => new List<MonsterSpawnArea>());
        foreach (var region in Enumerable.Range(0, fieldSpawns.Count).GroupBy(FindRegion).Select(group => group.Select(index => fieldSpawns[index]).ToList()))
        {
            var closestGate = floorGates.MinBy(gate => region.Min(spawn => GetDistance(gate, spawn)))!;
            floors[closestGate].AddRange(region);
        }

        return floors.Where(floor => floor.Value.Count > 0).Select(floor => ((ExitGate?)floor.Key, floor.Value)).ToList();
    }

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        // The result depends only on the positions of the spawns, not on their current quantity.
        // So applying it again, or over version 1, leads to the same state.
        foreach (var map in gameConfiguration.Maps)
        {
            var fieldSpawns = map.MonsterSpawns.Where(spawn => IsFieldMonsterSpawn(map, spawn)).ToList();
            if (fieldSpawns.Count == 0)
            {
                continue;
            }

            foreach (var (_, floorSpawns) in GetFloors(GetFloorGates(gameConfiguration, map), fieldSpawns))
            {
                ThinOutFloor(floorSpawns);
            }
        }

        return ValueTask.CompletedTask;
    }

    private static void ThinOutFloor(IReadOnlyList<MonsterSpawnArea> spawns)
    {
        var areas = spawns.Where(spawn => !IsSpot(spawn)).ToList();
        var spots = spawns.Where(IsSpot).ToList();
        if (spots.Count == 0)
        {
            // Monsters of an area are placed randomly within it, so the floor's monsters are distributed by area size.
            var totalSize = areas.Sum(GetSize);
            foreach (var area in areas)
            {
                area.Quantity = (short)Math.Max(1, Math.Round((double)MonstersPerFloor * GetSize(area) / totalSize));
            }

            return;
        }

        foreach (var area in areas)
        {
            area.Quantity = 1;
        }

        var selectedSpots = SelectSpreadSpots(spots, MonstersPerFloor - areas.Count);
        foreach (var spot in spots)
        {
            spot.Quantity = (short)(selectedSpots.Contains(spot) ? 1 : 0);
        }
    }

    /// <summary>
    /// Selects spots which are spread evenly, by repeatedly taking the spot which is farthest away from all selected ones.
    /// Every monster type keeps at least one spot, rare types first.
    /// </summary>
    private static HashSet<MonsterSpawnArea> SelectSpreadSpots(IReadOnlyList<MonsterSpawnArea> spots, int count)
    {
        var spotsPerMonster = spots.GroupBy(spot => spot.MonsterDefinition!).ToDictionary(group => group.Key, group => group.Count());
        var candidates = spots
            .OrderBy(spot => spotsPerMonster[spot.MonsterDefinition!])
            .ThenBy(spot => spot.MonsterDefinition!.Number)
            .ThenBy(spot => spot.X1)
            .ThenBy(spot => spot.Y1)
            .ToList();

        var selected = candidates.DistinctBy(spot => spot.MonsterDefinition).ToList();
        var remaining = candidates.Except(selected).ToList();
        var distances = remaining.ToDictionary(spot => spot, spot => selected.Min(other => GetDistance(spot, other)));
        while (selected.Count < count && remaining.Count > 0)
        {
            var farthest = remaining.MaxBy(spot => distances[spot])!;
            if (distances[farthest] < MinimumSpotDistance)
            {
                break;
            }

            selected.Add(farthest);
            remaining.Remove(farthest);
            foreach (var spot in remaining)
            {
                distances[spot] = Math.Min(distances[spot], GetDistance(spot, farthest));
            }
        }

        return selected.ToHashSet();
    }

    private static int GetDistance(MonsterSpawnArea a, MonsterSpawnArea b)
        => Math.Max(Math.Abs(a.X1 + a.X2 - b.X1 - b.X2), Math.Abs(a.Y1 + a.Y2 - b.Y1 - b.Y2)) / 2;

    private static int GetDistance(ExitGate gate, MonsterSpawnArea spawn)
        => Math.Max(Math.Abs(gate.X1 + gate.X2 - spawn.X1 - spawn.X2), Math.Abs(gate.Y1 + gate.Y2 - spawn.Y1 - spawn.Y2)) / 2;

    private static int GetSize(MonsterSpawnArea area)
        => (area.X2 - area.X1 + 1) * (area.Y2 - area.Y1 + 1);
}
