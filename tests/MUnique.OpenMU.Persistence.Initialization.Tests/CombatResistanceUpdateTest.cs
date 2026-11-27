// <copyright file="CombatResistanceUpdateTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Tests;

using Microsoft.Extensions.Logging.Abstractions;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.Persistence.Initialization.Updates;
using MUnique.OpenMU.Persistence.InMemory;

/// <summary>Verifies the resistance attributes on fresh and existing Season 6 games.</summary>
[TestFixture]
internal class CombatResistanceUpdateTest
{
    [Test]
    public async Task FreshAndExistingGamesHaveBothAttributesAsync()
    {
        var provider = new InMemoryPersistenceContextProvider();
        var initialization = new VersionSeasonSix.DataInitialization(provider, new NullLoggerFactory());
        await initialization.CreateInitialDataAsync(1, true).ConfigureAwait(false);
        using var context = provider.CreateNewContext();
        var configuration = (await context.GetAsync<GameConfiguration>().ConfigureAwait(false)).First();
        var definitions = new[] { Stats.ExcellentDamageResistance, Stats.IgnoreDefenseResistance };
        foreach (var definition in definitions)
        {
            Assert.That(configuration.Attributes.Remove(configuration.Attributes.Single(item => item.Id == definition.Id)), Is.True);
        }

        var update = new AddCombatResistanceAttributesUpdatePlugIn();
        await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        foreach (var definition in definitions)
        {
            Assert.That(configuration.Attributes.Count(item => item.Id == definition.Id), Is.EqualTo(1));
        }
    }
}
