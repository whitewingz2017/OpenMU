// <copyright file="CosmeticSetConversionTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Tests;

using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.Persistence.Initialization.Updates;
using MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Items;
using MUnique.OpenMU.Persistence.InMemory;
using BasicModel = MUnique.OpenMU.Persistence.BasicModel;

/// <summary>Checks conversion conserves item identities and rolls back when storage cannot fit.</summary>
[TestFixture]
internal class CosmeticSetConversionTests
{
    /// <summary>Each old piece becomes its own full-set item, including equipped pieces and warehouse items.</summary>
    [Test]
    public async Task EveryPieceBecomesAFullSetAndConversionIsIdempotentAsync()
    {
        var provider = new InMemoryPersistenceContextProvider();
        using var context = provider.CreateNewContext();
        var configuration = context.CreateNew<GameConfiguration>();
        FenrirMountFixture.Add(context, configuration);
        CosmeticArmorItems.Ensure(context, configuration);
        var character = context.CreateNew<Character>();
        character.Inventory = context.CreateNew<ItemStorage>();
        var account = context.CreateNew<Account>();
        account.Vault = context.CreateNew<ItemStorage>();
        var items = new List<Item>();
        for (byte part = 0; part < 5; part++)
        {
            var item = context.CreateNew<Item>();
            item.Definition = configuration.Items.Single(d => d.Group == 7 + part && d.Number == 100);
            item.ItemSlot = (byte)(240 + part);
            character.Inventory.Items.Add(item);
            items.Add(item);
        }

        var stored = context.CreateNew<Item>();
        stored.Definition = configuration.Items.Single(d => d.Group == 7 && d.Number == 315);
        account.Vault.Items.Add(stored);
        var identities = items.Select(i => i.GetId()).ToArray();
        await CosmeticSetConversion.ApplyAsync(context, configuration).ConfigureAwait(false);
        await CosmeticSetConversion.ApplyAsync(context, configuration).ConfigureAwait(false);
        Assert.That(items.Select(i => i.GetId()), Is.EqualTo(identities));
        Assert.That(items.All(i => i.Definition!.Group == 8 && i.Definition.Number == 100), Is.True);
        Assert.That(character.Inventory.Items, Has.Count.EqualTo(5));
        Assert.That(character.Inventory.Items.Count(i => i.ItemSlot == 241), Is.EqualTo(1));
        Assert.That(character.Inventory.Items.Count(i => i.ItemSlot is >= 12 and < 76), Is.EqualTo(4));
        Assert.That(stored.Definition!.Group, Is.EqualTo(8));
        Assert.That(stored.Definition.Number, Is.EqualTo(315));
        _ = new MUnique.OpenMU.GameLogic.Storage(64, 0, 12, character.Inventory);
        _ = new MUnique.OpenMU.GameLogic.Storage(120, 0, 0, account.Vault);
    }

    /// <summary>Items loaded with old configuration references receive the new configured bonus pool.</summary>
    [Test]
    public async Task OldDefinitionReferencesUseTheUpdatedOptionsAsync()
    {
        var provider = new InMemoryPersistenceContextProvider();
        using var context = provider.CreateNewContext();
        var configuration = context.CreateNew<GameConfiguration>();
        FenrirMountFixture.Add(context, configuration);
        CosmeticArmorItems.Ensure(context, configuration);
        var definition = configuration.Items.Single(d => d.Group == 0 && d.Number == 100);
        var options = context.CreateNew<ItemOptionDefinition>();
        var option = context.CreateNew<IncreasableItemOption>();
        option.OptionType = ItemOptionTypes.Excellent;
        option.Number = 1;
        options.PossibleOptions.Add(option);
        definition.PossibleItemOptions.Add(options);
        var previousDefinition = new BasicModel.ItemDefinition
        {
            Id = definition.GetId(), Group = 0, Number = 100, Width = 1, Height = 1, ItemSlot = definition.ItemSlot,
        };
        var item = context.CreateNew<Item>();
        item.Definition = previousDefinition;
        item.ItemSlot = 12;
        var character = context.CreateNew<Character>();
        character.Inventory = context.CreateNew<ItemStorage>();
        character.Inventory.Items.Add(item);
        await CosmeticSetConversion.ApplyAsync(context, configuration).ConfigureAwait(false);
        Assert.That(item.Definition, Is.SameAs(definition));
        Assert.That(item.ItemOptions.Single().ItemOption, Is.SameAs(option));
    }

    /// <summary>Legacy floating Fenrirs become rideable items without replacing gear or their identifiers.</summary>
    [TestCase(248)]
    [TestCase(12)]
    public async Task LegacyMountLeavesSkinSlotAndKeepsIdentityAsync(byte slot)
    {
        var provider = new InMemoryPersistenceContextProvider();
        using var context = provider.CreateNewContext();
        var configuration = context.CreateNew<GameConfiguration>();
        FenrirMountFixture.Add(context, configuration);
        CosmeticArmorItems.Ensure(context, configuration);
        var character = context.CreateNew<Character>();
        character.Inventory = context.CreateNew<ItemStorage>();
        var definition = configuration.Items.Single(d => d.Group == 13 && d.Number == 300);
        var item = context.CreateNew<Item>();
        item.Definition = definition;
        item.ItemSlot = slot;
        item.Durability = 1;
        character.Inventory.Items.Add(item);
        var gear = context.CreateNew<Item>();
        gear.Definition = configuration.Items.Single(d => d.Group == 13 && d.Number == 37);
        gear.ItemSlot = 8;
        character.Inventory.Items.Add(gear);
        var id = item.GetId();
        await CosmeticSetConversion.ApplyAsync(context, configuration).ConfigureAwait(false);
        Assert.That(item.GetId(), Is.EqualTo(id));
        Assert.That(item.ItemSlot, Is.InRange(12, 75));
        Assert.That(item.Durability, Is.EqualTo(255));
        Assert.That(item.HasSkill, Is.True);
        Assert.That(gear.ItemSlot, Is.EqualTo(8));
        item.Durability = 200;
        await CosmeticSetConversion.ApplyAsync(context, configuration).ConfigureAwait(false);
        Assert.That(item.Durability, Is.EqualTo(200));
        _ = new MUnique.OpenMU.GameLogic.Storage(64, 0, 12, character.Inventory);
    }

    /// <summary>Insufficient bag space restores the original definition and equipped position.</summary>
    [Test]
    public void FullBagRollsBackEveryConvertedItem()
    {
        var provider = new InMemoryPersistenceContextProvider();
        using var context = provider.CreateNewContext();
        var configuration = context.CreateNew<GameConfiguration>();
        FenrirMountFixture.Add(context, configuration);
        CosmeticArmorItems.Ensure(context, configuration);
        var character = context.CreateNew<Character>();
        character.Inventory = context.CreateNew<ItemStorage>();
        var item = context.CreateNew<Item>();
        item.Definition = configuration.Items.Single(d => d.Group == 7 && d.Number == 100);
        item.ItemSlot = 240;
        character.Inventory.Items.Add(item);
        var originalDefinition = item.Definition;
        var mount = context.CreateNew<Item>();
        mount.Definition = configuration.Items.Single(d => d.Group == 13 && d.Number == 300);
        mount.ItemSlot = 248;
        mount.Durability = 1;
        character.Inventory.Items.Add(mount);
        var ordinary = context.CreateNew<ItemDefinition>();
        ordinary.Width = 1;
        ordinary.Height = 1;
        for (byte slot = 12; slot < 76; slot++)
        {
            var filler = context.CreateNew<Item>();
            filler.Definition = ordinary;
            filler.ItemSlot = slot;
            character.Inventory.Items.Add(filler);
        }

        Assert.ThrowsAsync<InvalidOperationException>(async () => await CosmeticSetConversion.ApplyAsync(context, configuration).ConfigureAwait(false));
        Assert.That(item.Definition, Is.SameAs(originalDefinition));
        Assert.That(item.ItemSlot, Is.EqualTo(240));
        Assert.That(character.Inventory.Items, Has.Count.EqualTo(66));
        Assert.That(mount.ItemSlot, Is.EqualTo(248));
        Assert.That(mount.Durability, Is.EqualTo(1));
        Assert.That(mount.HasSkill, Is.False);
    }
}
