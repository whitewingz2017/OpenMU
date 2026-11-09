// <copyright file="CosmeticArmorInitializationTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Tests;

using Microsoft.Extensions.Logging.Abstractions;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.Persistence.Initialization.Updates;
using MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Items;
using MUnique.OpenMU.Persistence.InMemory;

/// <summary>Verifies both fresh initialization and updates of the five zero-stat skins.</summary>
[TestFixture]
internal class CosmeticArmorInitializationTests
{
    [Test]
    public async Task FreshSeasonSixContainsTradableCosmeticsAsync()
    {
        var provider = new InMemoryPersistenceContextProvider();
        await new VersionSeasonSix.DataInitialization(provider, new NullLoggerFactory()).CreateInitialDataAsync(1, true).ConfigureAwait(false);
        using var context = provider.CreateNewContext();
        var configuration = (await context.GetAsync<GameConfiguration>().ConfigureAwait(false)).First();
        AssertSkins(configuration);
        foreach (var group in new byte[] { 0, 2, 5 })
        {
            var skin = configuration.Items.Single(d => d.Group == group && d.Number == 101);
            Assert.That(skin.Width, Is.EqualTo(2));
            Assert.That(skin.Height, Is.EqualTo(2));
            Assert.That(skin.ItemSlot!.ItemSlots.Single(), Is.EqualTo(group == 5 ? 246 : 245));
        }
        foreach (var group in new byte[] { 0, 5, 6, 8 })
        {
            var definition = configuration.Items.Single(d => d.Group == group && d.Number == 100);
            Assert.That(GameLogic.Cosmetics.SkinBonuses.Choices(definition).Count(), Is.EqualTo(6));
            Assert.That(GameLogic.Cosmetics.SkinBonuses.Choices(definition).All(o => o.OptionType == ItemOptionTypes.Excellent), Is.True);
        }

        foreach (var group in new byte[] { 0, 2, 5 })
        {
            var skin = configuration.Items.Single(d => d.Group == group && d.Number == 101);
            Assert.That(skin.QualifiedCharacters, Is.Not.Empty);
            Assert.That(GameLogic.Cosmetics.SkinBonuses.Choices(skin).Count(), Is.EqualTo(6));
        }

        Assert.That(configuration.Items.Single(d => d.Group == 4 && d.Number == 100).QualifiedCharacters.Select(c => c.Number), Is.EquivalentTo(new byte[] { 8, 10, 11 }));
        Assert.That(configuration.Items.Single(d => d.Group == 5 && d.Number == 100).QualifiedCharacters.Select(c => c.Number), Is.EquivalentTo(new byte[] { 0, 2, 3, 12, 13 }));
        Assert.That(configuration.Items.Single(d => d.Group == 5 && d.Number == 302).QualifiedCharacters.Select(c => c.Number), Is.EquivalentTo(new byte[] { 23 }));
        Assert.That(configuration.Items.Single(d => d.Group == 0 && d.Number == 101).QualifiedCharacters.Select(c => c.Number), Is.EquivalentTo(new byte[] { 24, 25 }));
        Assert.That(configuration.Items.Single(d => d.Group == 2 && d.Number == 101).QualifiedCharacters.Select(c => c.Number), Is.EquivalentTo(new byte[] { 16, 17 }));

        var book = configuration.Items.Single(d => d.Group == 5 && d.Number == 101);
        Assert.That(book.PossibleItemOptions.Single().GetId(), Is.EqualTo(GuidHelper.CreateGuid<ItemOptionDefinition>(ItemOptionDefinitionNumbers.ExcellentWizardry)));

        var wing = configuration.Items.Single(d => d.Group == 12 && d.Number == 210);
        Assert.That(GameLogic.Cosmetics.SkinBonuses.Choices(wing).Count(), Is.EqualTo(4));
        Assert.That(GameLogic.Cosmetics.SkinBonuses.Choices(wing).All(o => o.OptionType == ItemOptionTypes.Wing), Is.True);
        Assert.That(GameLogic.Cosmetics.SkinBonuses.Choices(configuration.Items.Single(d => d.Group == 13 && d.Number == 200)), Is.Empty);
        Assert.That(configuration.Items.Count(i => i.ItemSlot?.ItemSlots.Any(s => s >= 240 && s <= 248) == true), Is.EqualTo(442));
    }

    [Test]
    public async Task ExistingConfigurationUpdateIsIdempotentAsync()
    {
        var provider = new InMemoryPersistenceContextProvider();
        using var context = provider.CreateNewContext();
        var configuration = context.CreateNew<GameConfiguration>();
        FenrirMountFixture.Add(context, configuration);
        var update = new CosmeticArmorItemsUpdatePlugIn();
        await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        AssertSkins(configuration);
        foreach (var group in new byte[] { 0, 2, 5 })
        {
            var skin = configuration.Items.Single(d => d.Group == group && d.Number == 101);
            Assert.That(skin.Width, Is.EqualTo(2));
            Assert.That(skin.Height, Is.EqualTo(2));
            Assert.That(skin.ItemSlot!.ItemSlots.Single(), Is.EqualTo(group == 5 ? 246 : 245));
        }
        Assert.That(configuration.Items.Count(i => i.ItemSlot?.ItemSlots.Any(s => s >= 240 && s <= 248) == true), Is.EqualTo(442));
        Assert.That(configuration.ItemSlotTypes.Count, Is.EqualTo(10));
        var entry = (await context.GetAsync<ConfigurationUpdate>().ConfigureAwait(false)).Single();
        Assert.Multiple(() =>
        {
            Assert.That(entry.CreatedAt?.Kind, Is.EqualTo(DateTimeKind.Utc));
            Assert.That(entry.UpdatedAt?.Kind, Is.EqualTo(DateTimeKind.Utc));
            Assert.That(entry.InstalledAt?.Kind, Is.EqualTo(DateTimeKind.Utc));
        });
    }

    [Test]
    public async Task VersionNineUpgradeAddsReferenceWingsWithoutReplacingExistingItemsAsync()
    {
        var provider = new InMemoryPersistenceContextProvider();
        using var context = provider.CreateNewContext();
        var configuration = context.CreateNew<GameConfiguration>();
        FenrirMountFixture.Add(context, configuration);
        var update = new CosmeticArmorItemsUpdatePlugIn();
        await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        foreach (var wing in configuration.Items.Where(i => i.Group == 12 && i.Number is 382 or 383 or 384).ToArray())
        {
            configuration.Items.Remove(wing);
            await context.DeleteAsync(wing).ConfigureAwait(false);
        }

        var entry = (await context.GetAsync<ConfigurationUpdate>().ConfigureAwait(false)).Single();
        entry.Version = 9;
        var existing = configuration.Items.Single(i => i.Group == 12 && i.Number == 371);
        var existingId = existing.GetId();
        var previousCount = configuration.Items.Count;
        await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        AssertSkins(configuration);
        Assert.That(configuration.Items.Count, Is.EqualTo(previousCount + 3));
        Assert.That(configuration.Items.Single(i => i.Group == 12 && i.Number == 371), Is.SameAs(existing));
        Assert.That(existing.GetId(), Is.EqualTo(existingId));
        Assert.That(entry.Version, Is.EqualTo(new CosmeticArmorItemsUpdatePlugIn().Version));
        Assert.That((await context.GetAsync<ConfigurationUpdate>().ConfigureAwait(false)).Count(), Is.EqualTo(1));
    }

    [Test]
    public void ExistingItemIdIsNeverOverwritten()
    {
        var provider = new InMemoryPersistenceContextProvider();
        using var context = provider.CreateNewContext();
        var configuration = context.CreateNew<GameConfiguration>();
        var occupied = context.CreateNew<ItemDefinition>();
        occupied.Group = 7;
        occupied.Number = 315;
        occupied.Name = "Unrelated custom item";
        configuration.Items.Add(occupied);
        Assert.Throws<InvalidOperationException>(() => CosmeticArmorItems.Ensure(context, configuration));
        Assert.That(configuration.Items.Single(), Is.SameAs(occupied));
        Assert.That(configuration.ItemSlotTypes, Is.Empty);
    }

    /// <summary>Existing imported display names are updated without replacing definitions or their identifiers.</summary>
    [Test]
    public void LegacyBrandNamesAreRenamedInPlace()
    {
        var provider = new InMemoryPersistenceContextProvider();
        using var context = provider.CreateNewContext();
        var configuration = context.CreateNew<GameConfiguration>();
        FenrirMountFixture.Add(context, configuration);
        CosmeticArmorItems.Ensure(context, configuration);
        var imported = configuration.Items.First(d => d.Number >= 300 && d.Group == 12);
        var originalName = imported.Name.ValueInNeutralLanguage;
        var originalId = imported.GetId();
        imported.Name = $"FinalMU {originalName}";
        CosmeticArmorItems.Ensure(context, configuration);
        Assert.That(imported.Name.ValueInNeutralLanguage, Is.EqualTo(originalName));
        Assert.That(imported.GetId(), Is.EqualTo(originalId));
        Assert.That(configuration.Items.All(d => !d.Name.ValueInNeutralLanguage.StartsWith("FinalMU ", StringComparison.Ordinal)), Is.True);
    }

    /// <summary>Restores promotion limits and missing catalog items without replacing owned definitions.</summary>
    [TestCase(20, false)]
    [TestCase(23, true)]
    public async Task EquipmentCatalogRetainsSourceClassesAndRepairsPreviousImportsAsync(byte classNumber, bool allowed)
    {
        var provider = new InMemoryPersistenceContextProvider();
        await new VersionSeasonSix.DataInitialization(provider, new NullLoggerFactory()).CreateInitialDataAsync(1, true).ConfigureAwait(false);
        using var context = provider.CreateNewContext();
        var configuration = (await context.GetAsync<GameConfiguration>().ConfigureAwait(false)).First();
        var equipment = configuration.Items.Where(i => i.Number >= 300 && i.Group <= 8 && i.ItemSlot?.ItemSlots.Any(s => s is 241 or 245 or 246) == true).ToArray();
        Assert.That(equipment, Has.Length.EqualTo(280));
        Assert.That(equipment.Count(i => i.Group <= 6), Is.EqualTo(141));
        Assert.That(equipment.Count(i => i.Group == 8 && i.Number != 315), Is.EqualTo(138));
        Assert.That(equipment.All(i => i.QualifiedCharacters.Count > 0 && i.Width == 2 && i.Height == 2), Is.True);
        var previous = configuration.Items.Single(i => i.Group == 5 && i.Number == 302);
        var identity = previous.GetId();
        previous.QualifiedCharacters.Clear();
        foreach (var characterClass in configuration.CharacterClasses.Where(c => c.Number is 20 or 22 or 23))
        {
            previous.QualifiedCharacters.Add(characterClass);
        }

        var character = context.CreateNew<Character>();
        character.CharacterClass = configuration.CharacterClasses.Single(c => c.Number == classNumber);
        character.Inventory = context.CreateNew<ItemStorage>();
        var owned = context.CreateNew<Item>();
        owned.Definition = previous;
        owned.ItemSlot = 246;
        character.Inventory.Items.Add(owned);
        var ownedIdentity = owned.GetId();
        var entry = (await context.GetAsync<ConfigurationUpdate>().ConfigureAwait(false)).Single(e => e.Key == new CosmeticArmorItemsUpdatePlugIn().Key);
        entry.Version = 10;
        var added = configuration.Items.Single(i => i.Group == 1 && i.Number == 300);
        configuration.Items.Remove(added);
        var update = new CosmeticArmorItemsUpdatePlugIn();
        await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        Assert.That(previous.GetId(), Is.EqualTo(identity));
        Assert.That(owned.GetId(), Is.EqualTo(ownedIdentity));
        Assert.That(owned.Definition, Is.SameAs(previous));
        Assert.That(character.Inventory.Items.Single(), Is.SameAs(owned));
        Assert.That(owned.ItemSlot == 246, Is.EqualTo(allowed));
        Assert.That(entry.Version, Is.EqualTo(new CosmeticArmorItemsUpdatePlugIn().Version));
        Assert.That(previous.QualifiedCharacters.Select(c => c.Number), Is.EquivalentTo(new byte[] { 23 }));
        Assert.That(configuration.Items.Count(i => i.Group == 1 && i.Number == 300), Is.EqualTo(1));
        Assert.That(configuration.Items.Single(i => i.Group == 0 && i.Number == 304).QualifiedCharacters.Select(c => c.Number), Is.EquivalentTo(new byte[] { 25 }));
    }

    /// <summary>Fresh and updated imports retain class and promotion limits for weapons and armor models.</summary>
    [Test]
    public async Task ModelClassLimitsRepairUnrestrictedImportsAsync()
    {
        var provider = new InMemoryPersistenceContextProvider();
        using var context = provider.CreateNewContext();
        var configuration = context.CreateNew<GameConfiguration>();
        FenrirMountFixture.Add(context, configuration);
        CosmeticArmorItems.Ensure(context, configuration);
        (byte Group, short Number, byte[] Classes)[] cases = [
            (4, 303, [11]), (4, 304, [11]), (0, 311, [7]),
            (1, 304, [3, 7, 13]), (2, 307, [17]), (3, 302, [7, 13]),
            (5, 306, [3, 13]), (5, 302, [23]), (0, 304, [25]),
            (8, 344, [11, 23]), (8, 345, [11, 23]), (8, 327, [3, 7, 13, 17, 25]),
        ];
        foreach (var (group, number, classes) in cases)
        {
            var definition = configuration.Items.Single(i => i.Group == group && i.Number == number);
            Assert.That(definition.QualifiedCharacters.Select(c => c.Number), Is.EquivalentTo(classes));
            definition.QualifiedCharacters.Clear();
            foreach (var characterClass in configuration.CharacterClasses)
            {
                definition.QualifiedCharacters.Add(characterClass);
            }
        }

        var update = new CosmeticArmorItemsUpdatePlugIn();
        await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        foreach (var (group, number, classes) in cases)
        {
            var definition = configuration.Items.Single(i => i.Group == group && i.Number == number);
            Assert.That(definition.QualifiedCharacters.Select(c => c.Number), Is.EquivalentTo(classes));
        }
    }

    /// <summary>Upgrades old ordinary pets without changing identity, bonuses, or existing Muuns.</summary>
    [TestCase(false, false)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public async Task OrdinaryPetMigrationPreservesOwnedItemsAsync(bool occupied, bool fullBag)
    {
        var provider = new InMemoryPersistenceContextProvider();
        await new VersionSeasonSix.DataInitialization(provider, new NullLoggerFactory()).CreateInitialDataAsync(1, true).ConfigureAwait(false);
        using var context = provider.CreateNewContext();
        var configuration = (await context.GetAsync<GameConfiguration>().ConfigureAwait(false)).First();
        var character = context.CreateNew<Character>();
        character.Inventory = context.CreateNew<ItemStorage>();
        var pet = context.CreateNew<Item>();
        pet.Definition = configuration.Items.Single(i => i.Group == 13 && i.Number == 0);
        pet.ItemSlot = DataModel.InventoryConstants.PetSlot;
        pet.Level = 7;
        pet.Durability = 49;
        character.Inventory.Items.Add(pet);
        var identity = pet.GetId();
        var previousDefinition = pet.Definition;
        var existing = context.CreateNew<Item>();
        existing.Definition = configuration.Items.Single(i => i.Group == 13 && i.Number == 200);
        existing.ItemSlot = GameLogic.Pets.CompanionEquipmentRules.Slot;
        if (occupied)
        {
            character.Inventory.Items.Add(existing);
        }

        if (fullBag)
        {
            var fillerDefinition = context.CreateNew<ItemDefinition>();
            fillerDefinition.Width = fillerDefinition.Height = 1;
            configuration.Items.Add(fillerDefinition);
            for (byte slot = DataModel.InventoryConstants.EquippableSlotsCount; slot < DataModel.InventoryConstants.FirstExtensionItemSlotIndex; slot++)
            {
                var filler = context.CreateNew<Item>();
                filler.Definition = fillerDefinition;
                filler.ItemSlot = slot;
                character.Inventory.Items.Add(filler);
            }
        }

        var entry = (await context.GetAsync<ConfigurationUpdate>().ConfigureAwait(false)).Single(e => e.Key == new CosmeticArmorItemsUpdatePlugIn().Key);
        entry.Version = 14;
        var update = new CosmeticArmorItemsUpdatePlugIn();
        await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        Assert.That(pet.ItemSlot, Is.EqualTo(DataModel.InventoryConstants.PetSlot));
        Assert.That(entry.Version, Is.EqualTo(new CosmeticArmorItemsUpdatePlugIn().Version));
        _ = new GameLogic.Storage(DataModel.InventoryConstants.InventoryRows * DataModel.InventoryConstants.RowSize,
            0, DataModel.InventoryConstants.EquippableSlotsCount, character.Inventory);

        Assert.That(pet.GetId(), Is.EqualTo(identity));
        Assert.That(pet.Definition, Is.SameAs(previousDefinition));
        Assert.That(pet.Level, Is.EqualTo(7));
        Assert.That(pet.Durability, Is.EqualTo(49));
        Assert.That(character.Inventory.Items.Count(i => i == pet), Is.EqualTo(1));
        if (occupied)
        {
            Assert.That(existing.ItemSlot, Is.EqualTo(GameLogic.Pets.CompanionEquipmentRules.Slot));
        }
    }

    [TestCase(309, 8, false)]
    [TestCase(369, 8, false)]
    [TestCase(309, 248, false)]
    [TestCase(369, 248, false)]
    [TestCase(309, 248, true)]
    [TestCase(369, 248, true)]
    [TestCase(309, 12, false)]
    [TestCase(369, 12, false)]
    public async Task VersionFourteenHorseMountUpgradePreservesOwnedItemAsync(short number, byte slot, bool occupied)
    {
        var provider = new InMemoryPersistenceContextProvider();
        using var context = provider.CreateNewContext();
        var configuration = context.CreateNew<GameConfiguration>();
        FenrirMountFixture.Add(context, configuration);
        var update = new CosmeticArmorItemsUpdatePlugIn();
        await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        var definition = configuration.Items.Single(i => i.Group == 13 && i.Number == number);
        definition.ItemSlot = configuration.ItemSlotTypes.Single(s => s.ItemSlots.Single() == 248);
        definition.Skill = null;
        definition.Durability = 1;
        definition.Name = $"{definition.Name.ValueInNeutralLanguage} Skin";
        var identity = definition.GetId();
        var character = context.CreateNew<Character>();
        character.Inventory = context.CreateNew<ItemStorage>();
        var item = context.CreateNew<Item>();
        item.Definition = definition;
        item.ItemSlot = slot;
        item.Durability = 1;
        character.Inventory.Items.Add(item);
        if (occupied)
        {
            var original = context.CreateNew<Item>();
            original.Definition = configuration.Items.Single(i => i.Group == 13 && i.Number == 37);
            original.ItemSlot = DataModel.InventoryConstants.PetSlot;
            character.Inventory.Items.Add(original);
        }

        var entry = (await context.GetAsync<ConfigurationUpdate>().ConfigureAwait(false)).Single();
        entry.Version = 14;
        await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        Assert.That(definition.GetId(), Is.EqualTo(identity));
        Assert.That(item.Definition, Is.SameAs(definition));
        Assert.That(item.HasSkill, Is.True);
        Assert.That(item.Durability, Is.EqualTo(255));
        Assert.That(item.ItemSlot == DataModel.InventoryConstants.PetSlot, Is.EqualTo(slot == 8 || (slot == 248 && !occupied)));
        Assert.That(entry.Version, Is.EqualTo(new CosmeticArmorItemsUpdatePlugIn().Version));
        item.Durability = 200;
        await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        Assert.That(item.Durability, Is.EqualTo(200));
    }

    [Test]
    public async Task VersionFifteenAddsMissingSetVisualsWithoutChangingOwnedArmorAsync()
    {
        var provider = new InMemoryPersistenceContextProvider();
        using var context = provider.CreateNewContext();
        var configuration = context.CreateNew<GameConfiguration>();
        FenrirMountFixture.Add(context, configuration);
        var update = new CosmeticArmorItemsUpdatePlugIn();
        await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        foreach (var visual in configuration.Items.Where(d => d.Group is >= 7 and <= 11 && d.ItemSlot is null).ToArray())
        {
            configuration.Items.Remove(visual);
            await context.DeleteAsync(visual).ConfigureAwait(false);
        }

        var entry = (await context.GetAsync<ConfigurationUpdate>().ConfigureAwait(false)).Single();
        entry.Version = 15;
        var armor = configuration.Items.Single(d => d.Group == 8 && d.Number == 418);
        var armorId = armor.GetId();
        var character = context.CreateNew<Character>();
        character.CharacterClass = armor.QualifiedCharacters.FirstOrDefault();
        character.Inventory = context.CreateNew<ItemStorage>();
        var item = context.CreateNew<Item>();
        item.Definition = armor;
        item.ItemSlot = GameLogic.Cosmetics.CosmeticArmorRules.FirstSlot + 1;
        item.Level = 15;
        item.Durability = 1;
        character.Inventory.Items.Add(item);

        await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        AssertSkins(configuration);
        var count = configuration.Items.Count;
        await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        Assert.That(configuration.Items.Count, Is.EqualTo(count));
        Assert.That(entry.Version, Is.EqualTo(17));
        Assert.That(armor.GetId(), Is.EqualTo(armorId));
        Assert.That(item.Definition, Is.SameAs(armor));
        Assert.That(item.ItemSlot, Is.EqualTo(GameLogic.Cosmetics.CosmeticArmorRules.FirstSlot + 1));
        Assert.That(item.Level, Is.EqualTo(15));
        Assert.That(item.Durability, Is.EqualTo(1));
        Assert.That(character.Inventory.Items.Single(), Is.SameAs(item));
    }

    private static void AssertReferenceWings(GameConfiguration configuration)
    {
        foreach (var (number, name) in new[] { (382, "Custom Wing 21"), (383, "Custom Wing 6"), (384, "Custom Wing 7") })
        {
            var wing = configuration.Items.Single(i => i.Group == 12 && i.Number == number);
            Assert.That(wing.Name.ValueInNeutralLanguage, Is.EqualTo(name));
            Assert.That(wing.ItemSlot!.ItemSlots.Single(), Is.EqualTo(247));
            Assert.That((wing.Width, wing.Height), Is.EqualTo((2, 2)));
            Assert.That(wing.Durability, Is.EqualTo(1));
            Assert.That(wing.IsTradable && wing.IsStorable && wing.IsPersonalStoreSellable, Is.True);
            Assert.That(wing.IsDroppable || wing.IsRepairable || wing.IsSellableToNpc, Is.False);
            Assert.That(wing.BasePowerUpAttributes.Select(a => a.TargetAttribute?.Id), Is.EquivalentTo(new[]
            {
                GameLogic.Attributes.Stats.MovementSpeed.Id, GameLogic.Attributes.Stats.MovementSpeedUnderwater.Id,
            }));
            var options = configuration.ItemOptions.SingleOrDefault(o => o.GetId() == GuidHelper.CreateGuid<ItemOptionDefinition>(ItemOptionDefinitionNumbers.Wing3rd));
            if (options is not null)
            {
                Assert.That(wing.PossibleItemOptions, Is.EquivalentTo(new[] { options }));
                Assert.That(GameLogic.Cosmetics.SkinBonuses.Choices(wing).Count(), Is.EqualTo(4));
            }
        }
    }

    private static void AssertSkins(GameConfiguration configuration)
    {
        foreach (var number in new short[] { 344, 345 })
        {
            var femaleArmor = configuration.Items.Single(d => d.Group == 8 && d.Number == number);
            Assert.That(femaleArmor.QualifiedCharacters.Select(c => c.Number), Is.EquivalentTo(new byte[] { 11, 23 }));
        }

        foreach (var (number, groups) in new[] { (418, new byte[] { 7, 8, 9, 10, 11 }), (303, new byte[] { 8, 9, 10, 11 }) })
        {
            var definitions = configuration.Items.Where(i => i.Number == number && i.Group is >= 7 and <= 11).ToArray();
            Assert.That(definitions.Select(d => d.Group), Is.EquivalentTo(groups));
            var armor = definitions.Single(d => d.Group == 8);
            foreach (var visual in definitions.Where(d => d.Group != 8))
            {
                Assert.That(visual.ItemSlot, Is.Null);
                Assert.That(GameLogic.Cosmetics.CosmeticArmorRules.GetSlot(visual), Is.Null);
                Assert.That(visual.QualifiedCharacters, Is.EquivalentTo(armor.QualifiedCharacters));
                Assert.That(visual.BasePowerUpAttributes, Is.Empty);
                Assert.That(visual.IsDroppable || visual.IsSellableToNpc || visual.IsTradable
                    || visual.IsStorable || visual.IsPersonalStoreSellable || visual.IsRepairable, Is.False);
            }
        }

        AssertReferenceWings(configuration);
        var original = configuration.Items.Single(d => d.Group == 13 && d.Number == 37);
        var mounts = configuration.Items.Where(GameLogic.Cosmetics.FenrirMountRules.IsCustom).ToArray();
        Assert.That(mounts, Has.Length.EqualTo(15));
        foreach (var mount in mounts)
        {
            Assert.That(mount.ItemSlot, Is.SameAs(original.ItemSlot));
            Assert.That(mount.Skill, Is.SameAs(original.Skill));
            Assert.That(mount.Width, Is.EqualTo(2));
            Assert.That(mount.Height, Is.EqualTo(2));
            Assert.That(mount.Durability, Is.EqualTo(255));
            Assert.That(mount.Requirements.Select(q => (q.Attribute, q.MinimumValue)), Is.EquivalentTo(original.Requirements.Select(q => (q.Attribute, q.MinimumValue))));
            Assert.That(mount.BasePowerUpAttributes.Select(q => (q.TargetAttribute, q.BaseValue, q.AggregateType)), Is.EquivalentTo(original.BasePowerUpAttributes.Select(q => (q.TargetAttribute, q.BaseValue, q.AggregateType))));
            Assert.That(mount.BasePowerUpAttributes.Intersect(original.BasePowerUpAttributes), Is.Empty);
            Assert.That(mount.Requirements.Intersect(original.Requirements), Is.Empty);
        }

        var skins = configuration.Items.Where(i => i.Number == 315 && i.Group >= 7 && i.Group <= 11).ToArray();
        Assert.That(skins, Has.Length.EqualTo(5));
        Assert.That(skins.All(i => i.IsTradable && i.IsStorable && i.IsPersonalStoreSellable), Is.True);
        Assert.That(skins.All(i => !i.IsSellableToNpc && !i.IsDroppable && !i.IsRepairable), Is.True);
        Assert.That(skins.All(i => i.BasePowerUpAttributes.Count == 0 && (i.Group == 8 || i.PossibleItemOptions.Count == 0)), Is.True);
        Assert.That(skins.All(i => i.Width == 2 && i.Height == 2 && i.ItemSlot!.ItemSlots.Single() == 240 + i.Group - 7), Is.True);
    }
}
