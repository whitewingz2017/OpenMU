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
/// where only a few monsters are scattered over a map. Spawns of event and quest maps stay untouched.
/// </summary>
/// <remarks>
/// Single spawn points are switched off by setting their quantity to zero instead of deleting them,
/// so a later update can bring them back.
/// </remarks>
[PlugIn]
[Display(Name = nameof(PlugInResources.SparseFieldSpawnsUpdatePlugIn_Name), Description = nameof(PlugInResources.SparseFieldSpawnsUpdatePlugIn_Description), ResourceType = typeof(PlugInResources))]
[Guid("D3401951-23EF-44EA-9CD8-191624442E8A")]
public sealed class SparseFieldSpawnsUpdatePlugIn : UpdatePlugInBase
{
    /// <summary>
    /// Of the single spawn points of a monster type on a map, one in this many stays active.
    /// </summary>
    internal const int KeepOneSpotIn = 10;

    /// <summary>
    /// The maximum quantity of a spawn area, whose monsters are placed randomly within the area.
    /// </summary>
    internal const short MaximumAreaQuantity = 3;

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
    public override DateTime CreatedAt => new(2026, 11, 13, 0, 0, 0, DateTimeKind.Utc);

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

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        foreach (var map in gameConfiguration.Maps)
        {
            var fieldSpawns = map.MonsterSpawns.Where(spawn => IsFieldMonsterSpawn(map, spawn)).ToList();
            foreach (var area in fieldSpawns.Where(spawn => !IsSpot(spawn) && spawn.Quantity > MaximumAreaQuantity))
            {
                area.Quantity = MaximumAreaQuantity;
            }

            // Every monster type keeps at least one spot; ordering by position spreads the kept spots over the map.
            foreach (var spotsOfMonster in fieldSpawns.Where(IsSpot).GroupBy(spawn => spawn.MonsterDefinition))
            {
                var index = 0;
                foreach (var spot in spotsOfMonster.OrderBy(spawn => spawn.X1).ThenBy(spawn => spawn.Y1))
                {
                    spot.Quantity = (short)(index % KeepOneSpotIn == 0 ? 1 : 0);
                    index++;
                }
            }
        }

        return ValueTask.CompletedTask;
    }
}
