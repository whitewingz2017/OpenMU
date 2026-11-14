// <copyright file="RestoreFieldSpawnQuantitiesUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Persistence.Initialization.Properties;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Sets the quantity of the automatic monster spawns of the field maps back to the default of the
/// data initialization, e.g. after they were thinned out.
/// </summary>
/// <remarks>
/// Devias, Noria and Elvenland are left out, because <see cref="FieldMonsterSpawnsUpdatePlugInSeason6"/>
/// already resets their spawns to the Season 6 layout. Event maps are left out, too.
/// The update is optional, because it overwrites quantities which a server owner may have customized.
/// </remarks>
[PlugIn]
[Display(Name = nameof(PlugInResources.RestoreFieldSpawnQuantitiesUpdatePlugIn_Name), Description = nameof(PlugInResources.RestoreFieldSpawnQuantitiesUpdatePlugIn_Description), ResourceType = typeof(PlugInResources))]
[Guid("56A6B7C1-AF33-45E6-9F9D-A0DD8156F890")]
public sealed class RestoreFieldSpawnQuantitiesUpdatePlugIn : UpdatePlugInBase
{
    /// <summary>
    /// The numbers of the maps whose spawns are not touched: event maps, and the maps whose
    /// spawns are reset by <see cref="FieldMonsterSpawnsUpdatePlugInSeason6"/>.
    /// </summary>
    internal static readonly IReadOnlySet<short> SkippedMapNumbers = new HashSet<short>
    {
        2, 3, 51, // Devias, Noria, Elvenland
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

    /// <summary>
    /// The default quantities of the spawns which don't spawn a single monster, by map number and spawn number.
    /// All other automatic monster spawns of the field maps spawn one monster.
    /// </summary>
    internal static readonly IReadOnlyDictionary<(short MapNumber, short SpawnNumber), short> DefaultQuantities = new Dictionary<(short, short), short>
    {
        // Lorencia
        [(0, 120)] = 45,
        [(0, 121)] = 45,
        [(0, 122)] = 40,
        [(0, 123)] = 20,
        [(0, 124)] = 20,
        [(0, 125)] = 15,
        [(0, 126)] = 45,
        [(0, 127)] = 45,
        [(0, 128)] = 15,
        // Vulcanus
        [(63, 21)] = 3,
        [(63, 22)] = 3,
        [(63, 23)] = 4,
        [(63, 24)] = 4,
        [(63, 25)] = 4,
        [(63, 26)] = 2,
        [(63, 27)] = 2,
        [(63, 28)] = 2,
        [(63, 30)] = 3,
        [(63, 33)] = 2,
        [(63, 34)] = 2,
    };

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override string Name => PlugInResources.RestoreFieldSpawnQuantitiesUpdatePlugIn_Name;

    /// <inheritdoc />
    public override string Description => PlugInResources.RestoreFieldSpawnQuantitiesUpdatePlugIn_Description;

    /// <inheritdoc />
    public override bool IsMandatory => false;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 11, 14, 0, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    public override IEnumerable<UpdateDependency> DependsOn => [typeof(FieldMonsterSpawnsUpdatePlugInSeason6).GUID];

    /// <summary>
    /// Gets the default quantity of a spawn.
    /// </summary>
    /// <param name="mapNumber">The map number.</param>
    /// <param name="spawnId">The id of the spawn.</param>
    /// <returns>The default quantity.</returns>
    internal static short GetDefaultQuantity(short mapNumber, Guid spawnId)
    {
        foreach (var ((map, number), quantity) in DefaultQuantities)
        {
            if (map == mapNumber && GuidHelper.CreateGuid<MonsterSpawnArea>(map, number) == spawnId)
            {
                return quantity;
            }
        }

        return 1;
    }

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        // Only the quantity is set, so applying it again leads to the same state.
        foreach (var map in gameConfiguration.Maps.Where(map => !SkippedMapNumbers.Contains(map.Number)))
        {
            foreach (var spawn in map.MonsterSpawns.Where(spawn => spawn.SpawnTrigger == SpawnTrigger.Automatic
                                                                   && spawn.MonsterDefinition?.ObjectKind == NpcObjectKind.Monster))
            {
                spawn.Quantity = GetDefaultQuantity(map.Number, spawn.GetId());
            }
        }

        return ValueTask.CompletedTask;
    }
}
