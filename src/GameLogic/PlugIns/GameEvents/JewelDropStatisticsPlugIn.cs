// <copyright file="JewelDropStatisticsPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.GameEvents;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.JewelBank;
using MUnique.OpenMU.GameLogic.NPC;
using MUnique.OpenMU.Persistence;
using MUnique.OpenMU.PlugIns;

/// <summary>Records newly generated monster jewels after a successful ground drop.</summary>
[PlugIn]
[Guid("7506E747-8F71-45C4-A9B0-819BD400F557")]
[Display(Name = nameof(PlugInResources.JewelDropStatistics_Name), Description = nameof(PlugInResources.JewelDropStatistics_Description), ResourceType = typeof(PlugInResources))]
public sealed class JewelDropStatisticsPlugIn : IMonsterItemDroppedPlugIn, ISupportCustomConfiguration<JewelStatisticsConfiguration>, ISupportDefaultCustomConfiguration
{
    /// <inheritdoc />
    public JewelStatisticsConfiguration? Configuration { get; set; }

    /// <inheritdoc />
    public object CreateDefaultConfig() => new JewelStatisticsConfiguration();

    /// <inheritdoc />
    public async ValueTask MonsterItemDroppedAsync(AttackableNpcBase monster, Player killer, DroppedItem droppedItem)
    {
        var configuration = this.Configuration ??= new JewelStatisticsConfiguration();
        var now = DateTime.UtcNow;
        var amounts = JewelStatistics.Count([droppedItem.Item], killer.GameContext.Configuration, configuration.ItemCodes, now);
        if (amounts.Count == 0)
        {
            return;
        }

        using var context = killer.GameContext.PersistenceContextProvider.CreateNewPlayerContext(killer.GameContext.Configuration);
        foreach (var amount in amounts)
        {
            await context.RecordJewelDropAsync(amount.ItemCode, amount.Amount, now).ConfigureAwait(false);
        }
    }
}
