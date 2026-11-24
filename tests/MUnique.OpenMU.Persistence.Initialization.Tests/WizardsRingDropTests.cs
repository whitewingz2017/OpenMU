// <copyright file="WizardsRingDropTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Tests;

using Microsoft.Extensions.Logging.Abstractions;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.Persistence.InMemory;
using MUnique.OpenMU.Persistence.Initialization.Updates;

/// <summary>Checks the Wizard's Ring drop policy on new and existing configurations.</summary>
[TestFixture]
internal class WizardsRingDropTests
{
    /// <summary>Fresh Season Six initialization allows the Wizard's Ring to be dropped.</summary>
    [Test]
    public async Task FreshRingIsDroppableAsync()
    {
        var provider = new InMemoryPersistenceContextProvider();
        var initialization = new VersionSeasonSix.DataInitialization(provider, new NullLoggerFactory());
        await initialization.CreateInitialDataAsync(1, true).ConfigureAwait(false);
        using var context = provider.CreateNewContext();
        var configuration = (await context.GetAsync<GameConfiguration>().ConfigureAwait(false)).First();
        Assert.That(configuration.Items.Single(item => item.Group == 13 && item.Number == 20).IsDroppable, Is.True);
    }

    /// <summary>The existing ring is corrected in place, and replaying the update changes no other limits.</summary>
    [Test]
    public async Task ExistingRingIsDroppableAfterUpdateAsync()
    {
        var provider = new InMemoryPersistenceContextProvider();
        using var context = provider.CreateNewContext();
        var configuration = context.CreateNew<GameConfiguration>();
        var ring = context.CreateNew<ItemDefinition>();
        ring.Group = 13;
        ring.Number = 20;
        ring.IsDroppable = false;
        configuration.Items.Add(ring);
        var update = new LimitWhiteWizardDropsUpdatePlugIn();
        await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        Assert.That(configuration.Items.Single(), Is.SameAs(ring));
        Assert.That(ring.IsDroppable, Is.True);
        Assert.That(ring.StorageLimitPerCharacter, Is.EqualTo(1));
    }
}
