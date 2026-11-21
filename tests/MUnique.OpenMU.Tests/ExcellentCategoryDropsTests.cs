// <copyright file="ExcellentCategoryDropsTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using Moq;
using MUnique.OpenMU.DataModel;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.Attributes;
using Models = MUnique.OpenMU.Persistence.BasicModel;

/// <summary>Checks excellent categories don't leak into each other or share a chance.</summary>
[TestFixture]
public class ExcellentCategoryDropsTests
{
    /// <summary>Explicit item lists still respect the selected excellent category.</summary>
    [TestCase(SpecialItemType.ExcellentAccessories, 13)]
    [TestCase(SpecialItemType.ExcellentEquipment, 0)]
    public void ExplicitListSelectsOnlyItsCategory(SpecialItemType type, int expectedGroup)
    {
        var config = new Models.GameConfiguration();
        var group = new Models.DropItemGroup { ItemType = type };
        group.PossibleItems.Add(CreateItem(0, InventoryConstants.LeftHandSlot));
        group.PossibleItems.Add(CreateItem(13, InventoryConstants.Ring1Slot));
        var item = new DefaultDropGenerator(config, Randomizer(0)).GenerateItemDrop(group);
        Assert.That(item, Is.Not.Null);
        Assert.That(item!.Definition!.Group, Is.EqualTo(expectedGroup));
        Assert.That(item.ItemOptions, Has.Count.EqualTo(1));
    }

    /// <summary>A group with no eligible accessory can't silently drop other equipment.</summary>
    [Test]
    public void MissingAccessoryDoesNotFallBackToEquipment()
    {
        var config = new Models.GameConfiguration();
        var group = new Models.DropItemGroup { ItemType = SpecialItemType.ExcellentAccessories };
        group.PossibleItems.Add(CreateItem(0, InventoryConstants.LeftHandSlot));
        Assert.That(new DefaultDropGenerator(config, Randomizer(0)).GenerateItemDrop(group), Is.Null);
    }

    /// <summary>Zero chance stays disabled even at the lower endpoint of the random roll.</summary>
    [Test]
    public void ZeroChanceGroupsAreDisabled()
    {
        var config = new Models.GameConfiguration();
        var group = new Models.DropItemGroup { ItemType = SpecialItemType.ExcellentAccessories, Chance = 0 };
        group.PossibleItems.Add(CreateItem(13, InventoryConstants.Ring1Slot));
        var result = new DefaultDropGenerator(config, Randomizer(0)).GenerateItemDrop(new[] { group });
        Assert.That(result.Item, Is.Null);
    }

    /// <summary>Each chance owns a separate interval of the monster's drop roll.</summary>
    [TestCase(.05, .1, 0)]
    [TestCase(.25, .1, 13)]
    [TestCase(.25, 0, -1)]
    public async Task MonsterDropChancesAreIndependentAsync(double roll, double accessoryChance, int expectedGroup)
    {
        var config = new Models.GameConfiguration();
        config.Items.Add(CreateItem(0, InventoryConstants.LeftHandSlot));
        config.Items.Add(CreateItem(13, InventoryConstants.PendantSlot));
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        player.CurrentMap!.Definition.DropItemGroups.Clear();
        player.SelectedCharacter!.DropItemGroups.Clear();
        var monster = new Models.MonsterDefinition { NumberOfMaximumItemDrops = 1 };
        monster.Attributes.Add(new Models.MonsterAttribute { AttributeDefinition = Stats.Level, Value = 1 });
        monster.DropItemGroups.Add(new Models.DropItemGroup { ItemType = SpecialItemType.ExcellentEquipment, Chance = .2 });
        monster.DropItemGroups.Add(new Models.DropItemGroup { ItemType = SpecialItemType.ExcellentAccessories, Chance = accessoryChance });
        var (items, _) = await new DefaultDropGenerator(config, Randomizer(roll))
            .GenerateItemDropsAsync(monster, 0, player).ConfigureAwait(false);
        Assert.That(items.Select(item => (int)item.Definition!.Group),
            expectedGroup < 0 ? Is.Empty : Is.EquivalentTo(new[] { expectedGroup }));
    }

    private static ItemDefinition CreateItem(byte group, int slot)
    {
        var definition = new Models.ItemDefinition { Group = group, Number = 0, Width = 1, Height = 1, Durability = 1, DropLevel = 1, DropsFromMonsters = true };
        var slotType = new Models.ItemSlotType();
        slotType.ItemSlots.Add(slot);
        definition.ItemSlot = slotType;
        var options = new Models.ItemOptionDefinition { MaximumOptionsPerItem = 1 };
        options.PossibleOptions.Add(new Models.IncreasableItemOption { OptionType = ItemOptionTypes.Excellent, Number = 1 });
        definition.PossibleItemOptions.Add(options);
        return definition;
    }

    private static IRandomizer Randomizer(double roll)
    {
        var random = new Mock<IRandomizer>();
        random.Setup(value => value.NextInt(It.IsAny<int>(), It.IsAny<int>())).Returns((int minimum, int _) => minimum);
        random.Setup(value => value.NextDouble()).Returns(roll);
        return random.Object;
    }
}
