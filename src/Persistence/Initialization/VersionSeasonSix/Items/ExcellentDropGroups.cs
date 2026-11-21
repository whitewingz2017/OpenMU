// <copyright file="ExcellentDropGroups.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Items;

using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Interfaces;
using MUnique.OpenMU.Persistence.Initialization.Properties;

/// <summary>Separates ordinary monster excellent drops into equipment and accessory groups.</summary>
internal static class ExcellentDropGroups
{
    /// <summary>Splits unrestricted excellent groups once and retains operator-configured chances.</summary>
    /// <param name="context">The persistence context.</param>
    /// <param name="config">The configuration being updated.</param>
    /// <returns>The newly created accessory groups.</returns>
    internal static IReadOnlyList<DropItemGroup> Ensure(IContext context, GameConfiguration config)
    {
        var added = new List<DropItemGroup>();
        foreach (var equipment in config.DropItemGroups.Where(group => group.ItemType == SpecialItemType.Excellent
                     && group is not ItemDropItemGroup && group.PossibleItems.Count == 0).ToArray())
        {
            var accessories = context.CreateNew<DropItemGroup>();
            accessories.ItemType = SpecialItemType.ExcellentAccessories;
            accessories.Description = LocalizedString.FromResource(() => ExcellentDropResources.ExcellentAccessoriesDropGroup);
            accessories.Chance = equipment.Chance;
            accessories.ItemLevel = equipment.ItemLevel;
            accessories.MinimumMonsterLevel = equipment.MinimumMonsterLevel;
            accessories.MaximumMonsterLevel = equipment.MaximumMonsterLevel;
            accessories.Monster = equipment.Monster;
            config.DropItemGroups.Add(accessories);
            foreach (var map in config.Maps.Where(map => map.DropItemGroups.Contains(equipment)))
            {
                map.DropItemGroups.Add(accessories);
            }

            foreach (var monster in config.Monsters.Where(monster => monster.DropItemGroups.Contains(equipment)))
            {
                monster.DropItemGroups.Add(accessories);
            }

            equipment.ItemType = SpecialItemType.ExcellentEquipment;
            added.Add(accessories);
        }

        return added;
    }
}
