// <copyright file="ExcellentCombinationUpdateTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Tests;

using Microsoft.Extensions.Logging.Abstractions;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.PlayerActions.Items;
using MUnique.OpenMU.Persistence.Initialization.Updates;
using MUnique.OpenMU.Persistence.InMemory;
using MUnique.OpenMU.PlugIns;

/// <summary>Checks fresh material initialization, optional update and preservation of admin settings.</summary>
[TestFixture]
internal class ExcellentCombinationUpdateTest
{
    /// <summary>The update is idempotent and keeps customized settings and activation.</summary>
    [Test]
    public async Task UpdatePreservesSavedConfigurationAsync()
    {
        var provider = new InMemoryPersistenceContextProvider();
        await new VersionSeasonSix.DataInitialization(provider, new NullLoggerFactory()).CreateInitialDataAsync(1, true).ConfigureAwait(false);
        using var context = provider.CreateNewContext();
        var config = (await context.GetAsync<GameConfiguration>().ConfigureAwait(false)).First();
        var item = config.Items.Single(i => i.Group == 14 && i.Number == 252);
        Assert.That(item.Name.ValueInNeutralLanguage, Is.EqualTo("Resonance Core"));
        Assert.That(item.Durability, Is.EqualTo(255));
        var feature = config.PlugInConfigurations.Single(p => p.TypeId == typeof(ExcellentCombinationFeaturePlugIn).GUID);
        Assert.That(feature.IsActive, Is.False);
        feature.SetConfiguration(new ExcellentCombinationConfiguration { BaseRate = 40, MaximumRate = 90, ZenCost = 123 }, null);
        feature.IsActive = false;
        var saved = feature.CustomConfiguration;
        var update = new ExcellentCombinationUpdatePlugIn();
        await update.ApplyUpdateAsync(context, config).ConfigureAwait(false);
        await update.ApplyUpdateAsync(context, config).ConfigureAwait(false);
        Assert.That(feature.CustomConfiguration, Is.EqualTo(saved));
        Assert.That(feature.IsActive, Is.False);
        Assert.That(config.Items.Count(i => i.Group == 14 && i.Number == 252), Is.EqualTo(1));
        config.Items.Remove(item);
        await update.ApplyUpdateAsync(context, config).ConfigureAwait(false);
        Assert.That(config.Items.Count(i => i.Group == 14 && i.Number == 252), Is.EqualTo(1));
        config.PlugInConfigurations.Remove(feature);
        await update.ApplyUpdateAsync(context, config).ConfigureAwait(false);
        var registered = config.PlugInConfigurations.Single(p => p.TypeId == typeof(ExcellentCombinationFeaturePlugIn).GUID);
        Assert.That(registered.IsActive, Is.True);
        Assert.That(registered.GetConfiguration<ExcellentCombinationConfiguration>(null)!.BaseRate, Is.EqualTo(35));
        config.Items.Single(i => i.Group == 14 && i.Number == 252).Name = "Another custom item";
        Assert.ThrowsAsync<InvalidOperationException>(async () => await update.ApplyUpdateAsync(context, config).ConfigureAwait(false));
    }
}
