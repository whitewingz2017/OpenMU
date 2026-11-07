// <copyright file="TigrisSocketItemsTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Tests;

using Microsoft.Extensions.Logging.Abstractions;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.Persistence.Initialization.CharacterClasses;
using MUnique.OpenMU.Persistence.Initialization.Updates;
using MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Items;
using MUnique.OpenMU.Persistence.InMemory;

/// <summary>Checks fresh, upgraded and conflicting Tigris equipment configurations.</summary>
[TestFixture]
internal class TigrisSocketItemsTests
{
    [Test]
    public async Task FreshAndUpgradedConfigurationsHaveSocketEquipmentAsync()
    {
        var provider = new InMemoryPersistenceContextProvider();
        await new VersionSeasonSix.DataInitialization(provider, new NullLoggerFactory()).CreateInitialDataAsync(1, true).ConfigureAwait(false);
        using var context = provider.CreateNewContext();
        var configuration = (await context.GetAsync<GameConfiguration>().ConfigureAwait(false)).First();
        AssertEquipment(configuration);
        var originalWeapon = configuration.Items.Single(i => i.Group == 0 && i.Number == 35);
        var originalRequirement = originalWeapon.Requirements.First();
        var previousCount = configuration.Items.Count;
        var newItems = configuration.Items.Where(IsTigris).ToArray();
        foreach (var set in configuration.ItemSetGroups.Where(s => s.Items.Any(m => m.ItemDefinition is { } item && IsTigris(item))).ToArray())
        {
            configuration.ItemSetGroups.Remove(set);
            await context.DeleteAsync(set).ConfigureAwait(false);
        }

        foreach (var item in newItems)
        {
            configuration.Items.Remove(item);
            await context.DeleteAsync(item).ConfigureAwait(false);
        }

        var update = new TigrisSocketItemsUpdatePlugIn();
        await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        AssertEquipment(configuration);
        var addedWeapon = configuration.Items.Single(i => i.Group == 0 && i.Number == 52);
        var setCount = configuration.ItemSetGroups.Count;
        addedWeapon.Durability = 123;
        TigrisSocketItems.Ensure(context, configuration);
        Assert.Multiple(() =>
        {
            Assert.That(configuration.Items.Count, Is.EqualTo(previousCount));
            Assert.That(configuration.ItemSetGroups.Count, Is.EqualTo(setCount));
            Assert.That(addedWeapon.Durability, Is.EqualTo(123), "Preserve owner balance edits.");
            Assert.That(originalWeapon.Requirements.First(), Is.SameAs(originalRequirement));
            Assert.That(addedWeapon.Requirements, Has.None.SameAs(originalRequirement));
        });
    }

    [Test]
    public void OccupiedItemNumberStopsBeforeCreatingAnything()
    {
        using var context = new InMemoryPersistenceContextProvider().CreateNewContext();
        var configuration = context.CreateNew<GameConfiguration>();
        var occupied = context.CreateNew<ItemDefinition>();
        occupied.Group = 7;
        occupied.Number = TigrisSocketItems.SetNumber;
        occupied.Name = "Unrelated custom helm";
        configuration.Items.Add(occupied);
        Assert.Throws<InvalidOperationException>(() => TigrisSocketItems.Ensure(context, configuration));
        Assert.That(configuration.Items.Single(), Is.SameAs(occupied));
    }

    private static bool IsTigris(ItemDefinition item)
        => (item.Group == 0 && item.Number == TigrisSocketItems.WeaponNumber)
            || (item.Group is 7 or 8 or 9 or 11 && item.Number == TigrisSocketItems.SetNumber);

    private static void AssertEquipment(GameConfiguration configuration)
    {
        var items = configuration.Items.Where(IsTigris).ToArray();
        Assert.That(items, Has.Length.EqualTo(5));
        foreach (var item in items)
        {
            Assert.That(item.MaximumSockets, Is.EqualTo(3));
            Assert.That(item.QualifiedCharacters.Select(c => c.Number), Is.EquivalentTo(new byte[] { (byte)CharacterClassNumber.RageFighter, (byte)CharacterClassNumber.FistMaster }));
            Assert.That(item.Requirements.Single(r => r.Attribute == Stats.Level).MinimumValue, Is.EqualTo(380));
            Assert.That(item.PossibleItemOptions.Any(o => o.PossibleOptions.Any(p => p.OptionType == ItemOptionTypes.SocketOption)), Is.True);
            Assert.That(item.PossibleItemOptions.Any(o => o.PossibleOptions.Any(p => p.OptionType == ItemOptionTypes.GuardianOption)), Is.False);
            if (item.Group != 0)
            {
                Assert.That(item.PossibleItemSetGroups, Has.Count.EqualTo(7));
                Assert.That(item.PossibleItemSetGroups.All(s => s.MinimumItemCount == 4 && s.Items.All(m => m.ItemDefinition is { } member && IsTigris(member))), Is.True);
            }
        }

        var weapon = items.Single(i => i.Group == 0);
        Assert.That(weapon.BasePowerUpAttributes.Any(p => p.TargetAttribute == Stats.IsGloveWeaponEquipped), Is.True);
        Assert.That(weapon.ItemSlot!.ItemSlots, Is.EquivalentTo(new[] { 0, 1 }));
    }
}
