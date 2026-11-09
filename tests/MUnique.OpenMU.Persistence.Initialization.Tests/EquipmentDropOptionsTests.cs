// <copyright file="EquipmentDropOptionsTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Tests;

using MUnique.OpenMU.DataModel.Attributes;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.Persistence.InMemory;
using MUnique.OpenMU.Persistence.Initialization.Updates;

/// <summary>Checks the upgrade changes drop rules without changing crafting or unrelated luck.</summary>
[TestFixture]
public class EquipmentDropOptionsTests
{
    /// <summary>An existing configuration gets one dedicated luck definition and an idempotent socket drop cap.</summary>
    [Test]
    public async Task UpdateReplacesLuckOnlyFor380AndSocketEquipmentAsync()
    {
        var provider = new InMemoryPersistenceContextProvider();
        using var context = provider.CreateNewContext();
        var configuration = context.CreateNew<GameConfiguration>();
        var luck = context.CreateNew<ItemOptionDefinition>();
        luck.AddChance = .25f;
        var option = context.CreateNew<IncreasableItemOption>();
        option.OptionType = ItemOptionTypes.Luck;
        option.PowerUpDefinition = context.CreateNew<PowerUpDefinition>();
        option.PowerUpDefinition.Boost = context.CreateNew<PowerUpDefinitionValue>();
        option.PowerUpDefinition.Boost.ConstantValue.Value = .05f;
        luck.PossibleOptions.Add(option);
        configuration.ItemOptions.Add(luck);
        var guardian = context.CreateNew<ItemOptionDefinition>();
        var guardianOption = context.CreateNew<IncreasableItemOption>();
        guardianOption.OptionType = ItemOptionTypes.GuardianOption;
        guardian.PossibleOptions.Add(guardianOption);
        var normal = context.CreateNew<ItemDefinition>();
        normal.PossibleItemOptions.Add(luck);
        var socket = context.CreateNew<ItemDefinition>();
        socket.MaximumSockets = 5;
        socket.PossibleItemOptions.Add(luck);
        var level380 = context.CreateNew<ItemDefinition>();
        level380.PossibleItemOptions.Add(guardian);
        level380.PossibleItemOptions.Add(luck);
        foreach (var item in new[] { normal, socket, level380 })
        {
            configuration.Items.Add(item);
        }

        var update = new SetEquipmentDropOptionsUpdatePlugIn();
        await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        Assert.That(configuration.MaximumSocketCountDrop, Is.EqualTo(2));
        Assert.That(socket.MaximumSockets, Is.EqualTo(5));
        Assert.That(configuration.ItemOptions, Has.Count.EqualTo(2));
        Assert.That(luck.AddChance, Is.EqualTo(.25f));
        Assert.That(normal.PossibleItemOptions.Single(), Is.SameAs(luck));
        foreach (var item in new[] { socket, level380 })
        {
            var updated = item.PossibleItemOptions.Single(definition => definition.PossibleOptions.Any(possible => possible.OptionType == ItemOptionTypes.Luck));
            Assert.That(updated.AddChance, Is.EqualTo(.30f));
            Assert.That(updated.MaximumOptionsPerItem, Is.EqualTo(1));
            var copiedOption = updated.PossibleOptions.Single();
            Assert.That(copiedOption, Is.Not.SameAs(option));
            Assert.That(copiedOption.PowerUpDefinition, Is.Not.SameAs(option.PowerUpDefinition));
            Assert.That(copiedOption.PowerUpDefinition!.Boost, Is.Not.SameAs(option.PowerUpDefinition.Boost));
            Assert.That(copiedOption.PowerUpDefinition.Boost!.ConstantValue.Value, Is.EqualTo(.05f));
        }

        Assert.That(luck.PossibleOptions.Single(), Is.SameAs(option));
    }
}
