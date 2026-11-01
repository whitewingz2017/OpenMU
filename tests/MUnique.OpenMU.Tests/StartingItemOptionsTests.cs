// <copyright file="StartingItemOptionsTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using Moq;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.Persistence.Initialization.PlugIns.CharacterCreated;
using Basic = MUnique.OpenMU.Persistence.BasicModel;

/// <summary>Checks starter option persistence and rejection before partial grants.</summary>
[TestFixture]
public class StartingItemOptionsTests
{
    /// <summary>Configured Luck, excellent options, skill and +16 survive conversion to persistent items.</summary>
    [Test]
    public async Task GrantsConfiguredOptionsAsync()
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        var definition = CreateDefinition();
        definition.Skill = new Basic.Skill();
        var character = player.SelectedCharacter!;
        character.Inventory = new Basic.ItemStorage();
        character.CharacterClass = new Basic.CharacterClass { Number = 4 };
        Mock.Get(player.GameContext.Configuration).Setup(c => c.Items).Returns(new[] { definition });
        var configuration = new StartingItemsConfiguration();
        var profile = configuration.Classes.Single(c => c.Class == StartingCharacterClass.DarkKnight);
        profile.Enabled = true;
        profile.Items.Add(new StartingItemReward { Group = 8, Number = 1, Level = 9, Quantity = 2, Luck = true, AdditionalOptionLevel = 4, ExcellentOptions = new List<int> { 2, 3, 4 } });
        new StartingItemsPlugIn { Configuration = configuration }.CharacterCreated(player.Account!, character, player.PersistenceContext, player.GameContext.Configuration, player.Logger);

        Assert.That(character.Inventory.Items, Has.Count.EqualTo(2));
        Assert.That(character.Inventory.Items.Select(i => i.ItemSlot), Is.EquivalentTo(new byte[] { 12, 14 }));
        foreach (var item in character.Inventory.Items)
        {
            Assert.That(item.Level, Is.EqualTo(9));
            Assert.That(item.HasSkill, Is.True);
            Assert.That(item.ItemOptions.Count(o => o.ItemOption!.OptionType == ItemOptionTypes.Luck), Is.EqualTo(1));
            Assert.That(item.ItemOptions.Single(o => o.ItemOption!.OptionType == ItemOptionTypes.Option).Level, Is.EqualTo(4));
            Assert.That(item.ItemOptions.Where(o => o.ItemOption!.OptionType == ItemOptionTypes.Excellent).Select(o => o.ItemOption!.Number), Is.EquivalentTo(new[] { 2, 3, 4 }));
        }

        Assert.That(character.Inventory.Items.First().ItemOptions.First(), Is.Not.SameAs(character.Inventory.Items.Last().ItemOptions.First()));
    }

    /// <summary>Unsupported or duplicate options reject the complete plan before persisting rewards.</summary>
    /// <param name="invalid">The invalid option to request.</param>
    [TestCase("excellent")]
    [TestCase("duplicate")]
    [TestCase("luck")]
    [TestCase("additional")]
    [TestCase("skill")]
    [TestCase("level")]
    public async Task InvalidOptionsRejectAllRewardsAsync(string invalid)
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        var definition = CreateDefinition();
        var character = player.SelectedCharacter!;
        character.Inventory = new Basic.ItemStorage();
        character.CharacterClass = new Basic.CharacterClass { Number = 4 };
        Mock.Get(player.GameContext.Configuration).Setup(c => c.Items).Returns(new[] { definition });
        var configuration = new StartingItemsConfiguration();
        var profile = configuration.Classes.Single(c => c.Class == StartingCharacterClass.DarkKnight);
        profile.Enabled = true;
        profile.Items.Add(new StartingItemReward { Group = 8, Number = 1 });
        var reward = new StartingItemReward { Group = 8, Number = 1 };
        switch (invalid)
        {
            case "excellent": reward.ExcellentOptions.Add(6); break;
            case "duplicate": reward.ExcellentOptions = new List<int> { 2, 2 }; break;
            case "luck": definition.PossibleItemOptions.First().PossibleOptions.Clear(); reward.Luck = true; break;
            case "additional": reward.AdditionalOptionLevel = 7; break;
            case "skill": reward.Skill = true; break;
            case "level": reward.Level = 16; break;
        }

        profile.Items.Add(reward);
        Assert.Throws<InvalidOperationException>(() => new StartingItemsPlugIn { Configuration = configuration }.CharacterCreated(player.Account!, character, player.PersistenceContext, player.GameContext.Configuration, player.Logger));
        Assert.That(character.Inventory.Items, Is.Empty);
    }

    private static Basic.ItemDefinition CreateDefinition()
    {
        var definition = new Basic.ItemDefinition { Group = 8, Number = 1, Width = 2, Height = 3, MaximumItemLevel = 15 };
        var options = new Basic.ItemOptionDefinition();
        options.PossibleOptions.Add(new Basic.IncreasableItemOption { OptionType = ItemOptionTypes.Luck });
        var additional = new Basic.IncreasableItemOption { OptionType = ItemOptionTypes.Option };
        additional.LevelDependentOptions.Add(new Basic.ItemOptionOfLevel { Level = 4 });
        options.PossibleOptions.Add(additional);
        foreach (var number in new short[] { 2, 3, 4 })
        {
            options.PossibleOptions.Add(new Basic.IncreasableItemOption { OptionType = ItemOptionTypes.Excellent, Number = number });
        }

        definition.PossibleItemOptions.Add(options);
        return definition;
    }
}
