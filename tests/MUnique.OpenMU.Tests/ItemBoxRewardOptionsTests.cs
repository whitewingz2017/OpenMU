// <copyright file="ItemBoxRewardOptionsTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using Moq;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic;
using BasicModel = MUnique.OpenMU.Persistence.BasicModel;

/// <summary>Checks exact box rewards, compatibility and preservation of shared random option settings.</summary>
[TestFixture]
public class ItemBoxRewardOptionsTests
{
    /// <summary>Box settings override only the generated item, including removal of random options.</summary>
    [TestCase(true, true, 7, 5)]
    [TestCase(false, false, 0, 0)]
    public void ExactOptionsOverrideRandomOptions(bool luck, bool skill, byte normalLevel, byte flags)
    {
        var definition = CreateEquipment();
        var group = new BasicModel.ItemDropItemGroup
        {
            ItemType = SpecialItemType.Excellent,
            MinimumLevel = 9,
            MaximumLevel = 9,
            HasLuck = luck,
            HasSkill = skill,
            OptionLevel = normalLevel,
            ExcellentOptions = flags,
        };
        group.PossibleItems.Add(definition);
        var item = Generate(group);
        Assert.Multiple(() =>
        {
            Assert.That(item.Level, Is.EqualTo(9));
            Assert.That(item.HasSkill, Is.EqualTo(skill));
            Assert.That(item.ItemOptions.Any(link => link.ItemOption?.OptionType == ItemOptionTypes.Luck), Is.EqualTo(luck));
            Assert.That(item.ItemOptions.Where(link => link.ItemOption?.OptionType == ItemOptionTypes.Option)
                .Select(link => link.Level), Is.EqualTo(normalLevel == 0 ? Array.Empty<int>() : [normalLevel]));
            Assert.That(item.ItemOptions.Where(link => link.ItemOption?.OptionType == ItemOptionTypes.Excellent)
                .Select(link => link.ItemOption!.Number).Order(), Is.EqualTo(flags == 0 ? Array.Empty<int>() : [1, 3]));
            Assert.That(definition.PossibleItemOptions.All(options => options.AddChance == 1 && options.AddsRandomly), Is.True);
        });
    }

    /// <summary>Null overrides keep existing item and excellent-group behavior.</summary>
    [Test]
    public void DefaultsKeepRandomOptions()
    {
        var group = new BasicModel.ItemDropItemGroup { ItemType = SpecialItemType.Excellent };
        group.PossibleItems.Add(CreateEquipment());
        var item = Generate(group);
        Assert.That(item.ItemOptions.Any(link => link.ItemOption?.OptionType == ItemOptionTypes.Luck), Is.True);
        Assert.That(item.ItemOptions.Count(link => link.ItemOption?.OptionType == ItemOptionTypes.Excellent), Is.EqualTo(1));
        Assert.That(item.HasSkill, Is.True);
    }

    /// <summary>Jewels and non-equipment don't receive unsupported attributes.</summary>
    [Test]
    public void UnsupportedOptionsAreSkipped()
    {
        var group = new BasicModel.ItemDropItemGroup { HasLuck = true, HasSkill = true, OptionLevel = 7, ExcellentOptions = 63 };
        group.PossibleItems.Add(new BasicModel.ItemDefinition { MaximumItemLevel = 0, Durability = 1 });
        var item = Generate(group);
        Assert.That(item.ItemOptions, Is.Empty);
        Assert.That(item.HasSkill, Is.False);
    }

    /// <summary>Normal options respect the selected item's own supported option levels.</summary>
    [Test]
    public void OptionLevelIsCappedToSupportedLevel()
    {
        var definition = CreateEquipment();
        var normal = definition.PossibleItemOptions.SelectMany(options => options.PossibleOptions)
            .Single(option => option.OptionType == ItemOptionTypes.Option);
        foreach (var level in normal.LevelDependentOptions.Where(level => level.Level > 3).ToArray())
        {
            normal.LevelDependentOptions.Remove(level);
        }

        var group = new BasicModel.ItemDropItemGroup { OptionLevel = 7 };
        group.PossibleItems.Add(definition);
        Assert.That(Generate(group).ItemOptions.Single(link => link.ItemOption == normal).Level, Is.EqualTo(3));
    }

    /// <summary>Overriding the normal option doesn't strip unrelated fixed pet options.</summary>
    [Test]
    public void FixedOptionsArePreserved()
    {
        var definition = CreateEquipment();
        var fixedOption = new BasicModel.IncreasableItemOption { Id = Guid.NewGuid(), Number = 4, OptionType = ItemOptionTypes.Option };
        var options = new BasicModel.ItemOptionDefinition { Id = Guid.NewGuid(), AddsRandomly = true, AddChance = 1, MaximumOptionsPerItem = 1 };
        options.PossibleOptions.Add(fixedOption);
        definition.PossibleItemOptions.Add(options);
        var group = new BasicModel.ItemDropItemGroup { OptionLevel = 0 };
        group.PossibleItems.Add(definition);
        Assert.That(Generate(group).ItemOptions.Any(link => link.ItemOption == fixedOption), Is.True);
    }

    private static Item Generate(ItemDropItemGroup group)
    {
        var random = new Mock<IRandomizer>();
        random.Setup(value => value.NextInt(It.IsAny<int>(), It.IsAny<int>())).Returns((int minimum, int _) => minimum);
        random.Setup(value => value.NextRandomBool(It.IsAny<double>())).Returns(true);
        return new DefaultDropGenerator(new BasicModel.GameConfiguration(), random.Object).GenerateItemDrop(group)!;
    }

    private static BasicModel.ItemDefinition CreateEquipment()
    {
        var definition = new BasicModel.ItemDefinition { MaximumItemLevel = 15, Durability = 10, ItemSlot = new BasicModel.ItemSlotType(), Skill = new BasicModel.Skill() };
        definition.QualifiedCharacters.Add(new BasicModel.CharacterClass());
        foreach (var type in new[] { ItemOptionTypes.Luck, ItemOptionTypes.Option, ItemOptionTypes.Excellent })
        {
            var options = new BasicModel.ItemOptionDefinition { Id = Guid.NewGuid(), AddsRandomly = true, AddChance = 1, MaximumOptionsPerItem = 1 };
            for (var number = 1; number <= (type == ItemOptionTypes.Excellent ? 6 : 1); number++)
            {
                var option = new BasicModel.IncreasableItemOption { Id = Guid.NewGuid(), OptionType = type, Number = number };
                if (type == ItemOptionTypes.Option)
                {
                    for (var level = 2; level <= 7; level++)
                    {
                        option.LevelDependentOptions.Add(new BasicModel.ItemOptionOfLevel { Id = Guid.NewGuid(), Level = level });
                    }
                }

                options.PossibleOptions.Add(option);
            }

            definition.PossibleItemOptions.Add(options);
        }

        return definition;
    }
}
