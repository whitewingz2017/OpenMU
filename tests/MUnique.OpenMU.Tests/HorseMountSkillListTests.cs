// <copyright file="HorseMountSkillListTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using Moq;
using MUnique.OpenMU.DataModel;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.Attributes;
using BasicModel = MUnique.OpenMU.Persistence.BasicModel;

/// <summary>Checks mount skills through equipment events and login, with legacy Dark Lord-only skill data.</summary>
[TestFixture]
public class HorseMountSkillListTests
{
    /// <summary>The Season 6 protocol skill number for Dark Horse Earthquake.</summary>
    private const ushort EarthquakeSkillNumber = 62;

    /// <summary>Custom horses grant their configured skill to allowed riders; the original horse keeps its restriction.</summary>
    /// <param name="number">The horse item number.</param>
    /// <param name="allowedClass">Whether the mount allows this rider's class.</param>
    /// <param name="expected">Whether Earthquake should become available.</param>
    [TestCase(309, true, true)]
    [TestCase(369, true, true)]
    [TestCase(309, false, false)]
    [TestCase(4, true, false)]
    public async Task HorseSkillAppearsOnEquipAndLoginAsync(short number, bool allowedClass, bool expected)
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        var item = CreateHorseItem(player, number, allowedClass);
        Assert.That(await player.Inventory!.AddItemAsync((byte)InventoryConstants.PetSlot, item).ConfigureAwait(false), Is.True);
        Assert.That(player.SkillList!.ContainsSkill(EarthquakeSkillNumber), Is.EqualTo(expected));
        using var loginSkills = new SkillList(player);
        Assert.That(loginSkills.ContainsSkill(EarthquakeSkillNumber), Is.EqualTo(expected));
        await player.Inventory.RemoveItemAsync(item).ConfigureAwait(false);
        Assert.That(player.SkillList.ContainsSkill(EarthquakeSkillNumber), Is.False);
        Assert.That(loginSkills.ContainsSkill(EarthquakeSkillNumber), Is.False);
    }

    private static Item CreateHorseItem(Player player, short number, bool allowedClass)
    {
        var skill = new BasicModel.Skill { Number = (short)EarthquakeSkillNumber, DamageType = DamageType.Physical };
        var slot = new BasicModel.ItemSlotType();
        slot.ItemSlots.Add(InventoryConstants.PetSlot);
        var definitionMock = new Mock<ItemDefinition>();
        definitionMock.SetupAllProperties();
        definitionMock.Setup(d => d.BasePowerUpAttributes).Returns(new List<ItemBasePowerUpDefinition>());
        definitionMock.Setup(d => d.QualifiedCharacters).Returns(new List<CharacterClass>());
        var definition = definitionMock.Object;
        definition.Group = 13;
        definition.Number = number;
        definition.Skill = skill;
        definition.ItemSlot = slot;
        definition.Width = 2;
        definition.Height = 2;
        definition.BasePowerUpAttributes.Add(new BasicModel.ItemBasePowerUpDefinition { TargetAttribute = Stats.IsHorseEquipped, BaseValue = 1 });
        if (allowedClass)
        {
            definition.QualifiedCharacters.Add(player.SelectedCharacter!.CharacterClass!);
        }

        return new Item { Definition = definition, HasSkill = true, Durability = 255 };
    }
}
