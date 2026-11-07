// <copyright file="RankTitleInitializationTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Tests;

using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging.Abstractions;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.RankTitles;
using MUnique.OpenMU.Persistence;
using MUnique.OpenMU.Persistence.Initialization.Updates;
using MUnique.OpenMU.Persistence.InMemory;
using MUnique.OpenMU.PlugIns;

/// <summary>Checks fresh configurations, existing installations and customized title prices.</summary>
[TestFixture]
public class RankTitleInitializationTests
{
    /// <summary>Fresh Season 6 servers include the same eighteen ranks as the custom clients.</summary>
    [Test]
    public async Task FreshConfigurationContainsTitlesAsync()
    {
        var provider = new InMemoryPersistenceContextProvider();
        await new VersionSeasonSix.DataInitialization(provider, new NullLoggerFactory()).CreateInitialDataAsync(1, true).ConfigureAwait(false);
        using var context = provider.CreateNewContext();
        var configuration = (await context.GetAsync<GameConfiguration>().ConfigureAwait(false)).First();
        using var dataSource = new GameConfigurationDataSource(new NullLogger<GameConfigurationDataSource>(), provider);
        await dataSource.GetOwnerAsync(configuration.GetId()).ConfigureAwait(false);
        AssertCatalog(configuration, false, new ByDataSourceReferenceHandler(dataSource));
    }

    /// <summary>Installing twice does not duplicate the feature or replace prices chosen by the owner.</summary>
    [Test]
    public async Task UpdatePreservesCustomPricesAsync()
    {
        var provider = new InMemoryPersistenceContextProvider();
        using var context = provider.CreateNewContext();
        var configuration = context.CreateNew<GameConfiguration>();
        var update = new RankTitlesUpdatePlugIn();
        await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        AssertCatalog(configuration);
        var feature = configuration.PlugInConfigurations.Single();
        var settings = new RankTitleSettings();
        settings.Titles.RemoveAll(title => title.Rank > 9);
        settings.Titles[0].Price = 321;
        settings.Titles[^1].Price = 456;
        feature.IsActive = false;
        using var dataSource = new GameConfigurationDataSource(new NullLogger<GameConfigurationDataSource>(), provider);
        feature.SetConfiguration(settings, new ByDataSourceReferenceHandler(dataSource));
        await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        var updated = feature.CustomConfiguration;
        await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        Assert.That(configuration.PlugInConfigurations, Has.Count.EqualTo(1));
        Assert.That(feature.IsActive, Is.False);
        Assert.That(feature.CustomConfiguration, Is.EqualTo(updated));
        var titles = feature.GetConfiguration<RankTitleSettings>(null)!.Titles;
        Assert.That(titles.Select(title => title.Rank), Is.EqualTo(Enumerable.Range(1, 18)));
        Assert.That(titles[0].Price, Is.EqualTo(321));
        Assert.That(titles[8].Price, Is.EqualTo(456));
        Assert.That(titles.Skip(9).All(title => title.Price == 100), Is.True);
    }

    private static void AssertCatalog(GameConfiguration configuration, bool active = true, ReferenceHandler? referenceHandler = null)
    {
        var feature = configuration.PlugInConfigurations.Single(plugin => plugin.TypeId == typeof(RankTitleFeaturePlugIn).GUID);
        Assert.That(feature.IsActive, Is.EqualTo(active));
        var settings = feature.GetConfiguration<RankTitleSettings>(referenceHandler)!;
        Assert.That(settings.IsValid(), Is.True);
        Assert.That(settings.Titles.Select(title => title.Rank), Is.EqualTo(Enumerable.Range(1, 18)));
        Assert.That(settings.Titles.All(title => title.Price == 100), Is.True);
    }
}
