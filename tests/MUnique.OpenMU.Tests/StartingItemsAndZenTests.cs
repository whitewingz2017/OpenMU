namespace MUnique.OpenMU.Tests;
using Moq;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.Persistence.Initialization.PlugIns.CharacterCreated;
using MUnique.OpenMU.PlugIns;
using Basic = MUnique.OpenMU.Persistence.BasicModel;
[TestFixture]
public class StartingItemsAndZenTests
{
    [Test]
    public async Task ClassSpecificStarterItemsFitWithoutEquipmentOverlapAsync()
    {
        var player = await PlayerTestHelper.CreatePlayerAsync();
        var character = player.SelectedCharacter!;
        character.CharacterClass = new Basic.CharacterClass { Number = 0 };
        character.Inventory = new Basic.ItemStorage();
        var definition = new Basic.ItemDefinition { Group = 13, Number = 450, Width = 2, Height = 2, Durability = 100 };
        Mock.Get(player.GameContext.Configuration).Setup(c => c.Items).Returns(new List<ItemDefinition> { definition });
        var config = new StartingItemsConfiguration();
        var profile = config.Classes.Single(c => c.Class == StartingCharacterClass.DarkWizard);
        profile.Enabled = true; profile.Items.Add(new() { Group = 13, Number = 450, Quantity = 2, Level = 3 });
        var plugin = new StartingItemsPlugIn { Configuration = config };
        plugin.CharacterCreated(player.Account!, character, player.PersistenceContext, player.GameContext.Configuration, player.Logger);
        Assert.That(character.Inventory.Items.Select(i => i.ItemSlot), Is.EquivalentTo(new byte[] { 12, 14 }));
        Assert.That(character.Inventory.Items.All(i => i.Level == 3 && i.Durability == 100), Is.True);
        character.Inventory = new Basic.ItemStorage(); character.CharacterClass.Number = 4;
        plugin.CharacterCreated(player.Account!, character, player.PersistenceContext, player.GameContext.Configuration, player.Logger);
        Assert.That(character.Inventory.Items, Is.Empty);
    }
    [Test]
    public async Task InvalidRewardCannotGrantPartialItemsAsync()
    {
        var player = await PlayerTestHelper.CreatePlayerAsync();
        var character = player.SelectedCharacter!; character.CharacterClass = new Basic.CharacterClass { Number = 0 }; character.Inventory = new Basic.ItemStorage();
        Mock.Get(player.GameContext.Configuration).Setup(c => c.Items).Returns(new List<ItemDefinition> { new Basic.ItemDefinition { Group = 14, Number = 0, Width = 1, Height = 1 } });
        var config = new StartingItemsConfiguration(); var profile = config.Classes[0]; profile.Enabled = true;
        profile.Items.Add(new() { Group = 14, Number = 0 }); profile.Items.Add(new() { Group = 15, Number = 511 });
        Assert.Throws<InvalidOperationException>(() => new StartingItemsPlugIn { Configuration = config }.CharacterCreated(player.Account!, character, player.PersistenceContext, player.GameContext.Configuration, player.Logger));
        Assert.That(character.Inventory.Items, Is.Empty);
    }
    [Test]
    public void StarterOverrideUsesActivePersistedProfileOnly()
    {
        var config = new Basic.GameConfiguration(); var settings = new StartingItemsConfiguration(); settings.Classes[0].Enabled = true;
        var plugin = new Basic.PlugInConfiguration { TypeId = typeof(StartingItemsPlugIn).GUID, IsActive = true };
        plugin.SetConfiguration(settings, null); config.PlugInConfigurations.Add(plugin);
        Assert.That(StartingItemsPlugIn.ReplacesLegacy(config, 0), Is.True);
        Assert.That(StartingItemsPlugIn.ReplacesLegacy(config, 4), Is.False);
        plugin.IsActive = false; Assert.That(StartingItemsPlugIn.ReplacesLegacy(config, 0), Is.False);
    }
    [TestCase(100u, 2d, 0, 1000, 200u)]
    [TestCase(100u, 0.5d, 0, 1000, 50u)]
    [TestCase(100u, 2d, 0, 150, 150u)]
    [TestCase(10u, 1d, 100, 1000, 100u)]
    [TestCase(0u, 10d, 100, 1000, 0u)]
    [TestCase(uint.MaxValue, 10000d, 0, int.MaxValue, (uint)int.MaxValue)]
    public void ZenAmountsAreBounded(uint amount, double multiplier, int minimum, int maximum, uint expected)
        => Assert.That(ZenDropPlugIn.Apply(amount, new() { AmountMultiplier = multiplier, MinimumAmount = minimum, MaximumAmount = maximum }), Is.EqualTo(expected));
    [Test]
    public void InvalidZenConfigPreservesExistingAmounts()
    {
        Assert.That(ZenDropPlugIn.Apply(100, new() { AmountMultiplier = double.NaN }), Is.EqualTo(100));
        Assert.That(ZenDropPlugIn.Apply(100, new() { MinimumAmount = 100, MaximumAmount = 10 }), Is.EqualTo(100));
        Assert.That(ZenDropPlugIn.Apply(100, null), Is.EqualTo(100));
    }
}
