// <copyright file="BossSocketDropsPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using System.ComponentModel;
using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.NPC;
using MUnique.OpenMU.PlugIns;

/// <summary>Prevents normal monsters from dropping socket equipment through any drop group.</summary>
[PlugIn]
[Display(Name = "Boss-only socket drops", Description = "Blocks socket equipment drops except for configured bosses and the Devil Square boss wave.")]
[Guid("b63031b7-53a4-457a-a87a-7486897c4761")]
public sealed class BossSocketDropsPlugIn : IMonsterItemDropPlugIn,
    ISupportCustomConfiguration<BossSocketDropsConfiguration>, ISupportDefaultCustomConfiguration
{
    /// <inheritdoc />
    public BossSocketDropsConfiguration? Configuration { get; set; }

    /// <inheritdoc />
    public object CreateDefaultConfig() => new BossSocketDropsConfiguration();

    /// <inheritdoc />
    public void ItemDropping(AttackableNpcBase monster, Player killer, Item item, CancelEventArgs eventArgs)
    {
        if (!this.CanDrop(monster.Definition, monster.SpawnArea, monster.CurrentMap.Definition.Number, item))
        {
            eventArgs.Cancel = true;
        }
    }

    /// <summary>Checks the configured boss restriction, including socket-capable items with zero sockets.</summary>
    /// <param name="monster">The killed monster definition.</param>
    /// <param name="spawn">Its spawn definition.</param>
    /// <param name="mapNumber">The map number.</param>
    /// <param name="item">The proposed drop.</param>
    /// <returns>Whether this restriction permits the drop.</returns>
    public bool CanDrop(MonsterDefinition monster, MonsterSpawnArea spawn, short mapNumber, Item item)
    {
        if (item.Definition?.MaximumSockets is not > 0 && item.SocketCount <= 0)
        {
            return true;
        }

        var configuration = this.Configuration ??= new BossSocketDropsConfiguration();
        if (configuration.DevilSquareMapNumbers.Contains(mapNumber))
        {
            return spawn.SpawnTrigger == SpawnTrigger.OnceAtWaveStart && spawn.WaveNumber == configuration.DevilSquareBossWave;
        }

        return configuration.BossMonsterNumbers.Contains(monster.Number);
    }
}
