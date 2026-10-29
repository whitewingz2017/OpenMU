// <copyright file="CosmeticArmorInitializationTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Tests;

using Microsoft.Extensions.Logging.Abstractions;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
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
            Assert.That(skin.Width, Is.EqualTo(1));
            Assert.That(skin.Height, Is.EqualTo(group == 2 ? 3 : 2));
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
        Assert.That(configuration.Items.Single(d => d.Group == 5 && d.Number == 302).QualifiedCharacters.Select(c => c.Number), Is.EquivalentTo(new byte[] { 20, 22, 23 }));
        Assert.That(configuration.Items.Single(d => d.Group == 0 && d.Number == 101).QualifiedCharacters.Select(c => c.Number), Is.EquivalentTo(new byte[] { 24, 25 }));
        Assert.That(configuration.Items.Single(d => d.Group == 2 && d.Number == 101).QualifiedCharacters.Select(c => c.Number), Is.EquivalentTo(new byte[] { 16, 17 }));

        var book = configuration.Items.Single(d => d.Group == 5 && d.Number == 101);
        Assert.That(book.PossibleItemOptions.Single().GetId(), Is.EqualTo(GuidHelper.CreateGuid<ItemOptionDefinition>(ItemOptionDefinitionNumbers.ExcellentWizardry)));

        var wing = configuration.Items.Single(d => d.Group == 12 && d.Number == 210);
        Assert.That(GameLogic.Cosmetics.SkinBonuses.Choices(wing).Count(), Is.EqualTo(4));
        Assert.That(GameLogic.Cosmetics.SkinBonuses.Choices(wing).All(o => o.OptionType == ItemOptionTypes.Wing), Is.True);
        Assert.That(GameLogic.Cosmetics.SkinBonuses.Choices(configuration.Items.Single(d => d.Group == 13 && d.Number == 200)), Is.Empty);
        Assert.That(configuration.Items.Count(i => i.ItemSlot?.ItemSlots.Any(s => s >= 240 && s <= 248) == true), Is.EqualTo(186));
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
            Assert.That(skin.Width, Is.EqualTo(1));
            Assert.That(skin.Height, Is.EqualTo(group == 2 ? 3 : 2));
            Assert.That(skin.ItemSlot!.ItemSlots.Single(), Is.EqualTo(group == 5 ? 246 : 245));
        }
        Assert.That(configuration.Items.Count(i => i.ItemSlot?.ItemSlots.Any(s => s >= 240 && s <= 248) == true), Is.EqualTo(186));
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

    private static void AssertSkins(GameConfiguration configuration)
    {
        var original = configuration.Items.Single(d => d.Group == 13 && d.Number == 37);
        var mounts = configuration.Items.Where(GameLogic.Cosmetics.FenrirMountRules.IsCustom).ToArray();
        Assert.That(mounts, Has.Length.EqualTo(13));
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
        Assert.That(skins.All(i => i.Width == 2 && i.Height == (i.Group == 8 ? 3 : 2) && i.ItemSlot!.ItemSlots.Single() == 240 + i.Group - 7), Is.True);
    }
}
