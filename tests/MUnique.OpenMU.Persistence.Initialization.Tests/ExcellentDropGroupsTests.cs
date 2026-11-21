// <copyright file="ExcellentDropGroupsTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Tests;

using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.Persistence.InMemory;
using MUnique.OpenMU.Persistence.Initialization.Updates;

/// <summary>Checks fresh data, existing rates and repeated upgrades.</summary>
[TestFixture]
public class ExcellentDropGroupsTests
{
    /// <summary>Fresh Season Six maps have independent equipment and accessory groups at equal chances.</summary>
    [Test]
    public async Task FreshServerHasSeparateExcellentGroupsAsync()
    {
        var (context, config) = await TestGameConfiguration.CreateSeason6Async().ConfigureAwait(false);
        using (context)
        {
            var equipment = config.DropItemGroups.Single(group => group.ItemType == SpecialItemType.ExcellentEquipment);
            var accessories = config.DropItemGroups.Single(group => group.ItemType == SpecialItemType.ExcellentAccessories);
            Assert.That(accessories.Chance, Is.EqualTo(equipment.Chance));
            Assert.That(accessories.PossibleItems, Is.Empty);
            var maps = config.Maps.Where(map => map.DropItemGroups.Contains(equipment)).ToArray();
            Assert.That(maps, Is.Not.Empty);
            foreach (var map in maps)
            {
                Assert.That(map.DropItemGroups, Does.Contain(accessories));
            }
        }
    }

    /// <summary>The upgrade copies custom rates, preserves item boxes and doesn't overwrite later adjustments.</summary>
    [Test]
    public async Task UpgradeRetainsRatesAndIsIdempotentAsync()
    {
        var provider = new InMemoryPersistenceContextProvider();
        using var context = provider.CreateNewContext();
        var config = context.CreateNew<GameConfiguration>();
        var equipment = context.CreateNew<DropItemGroup>();
        equipment.ItemType = SpecialItemType.Excellent;
        equipment.Chance = .025;
        equipment.MinimumMonsterLevel = 20;
        config.DropItemGroups.Add(equipment);
        var map = context.CreateNew<GameMapDefinition>();
        map.DropItemGroups.Add(equipment);
        config.Maps.Add(map);
        var box = context.CreateNew<ItemDropItemGroup>();
        box.ItemType = SpecialItemType.Excellent;
        var boxItem = context.CreateNew<ItemDefinition>();
        boxItem.DropItems.Add(box);
        config.Items.Add(boxItem);
        var update = new SeparateExcellentDropsUpdatePlugIn();
        await update.ApplyUpdateAsync(context, config).ConfigureAwait(false);
        var accessories = config.DropItemGroups.Single(group => group.ItemType == SpecialItemType.ExcellentAccessories);
        Assert.That(equipment.ItemType, Is.EqualTo(SpecialItemType.ExcellentEquipment));
        Assert.That(accessories.Chance, Is.EqualTo(.025));
        Assert.That(accessories.MinimumMonsterLevel, Is.EqualTo(20));
        Assert.That(map.DropItemGroups, Does.Contain(accessories));
        Assert.That(box.ItemType, Is.EqualTo(SpecialItemType.Excellent));
        accessories.Chance = .005;
        await update.ApplyUpdateAsync(context, config).ConfigureAwait(false);
        Assert.That(config.DropItemGroups, Has.Count.EqualTo(2));
        Assert.That(map.DropItemGroups, Has.Count.EqualTo(2));
        Assert.That(accessories.Chance, Is.EqualTo(.005));
        Assert.That(equipment.Chance, Is.EqualTo(.025));
    }
}
