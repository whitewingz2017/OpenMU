// <copyright file="MuunItemsUpdateTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Tests;

using Microsoft.Extensions.Logging.Abstractions;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.Muun;
using MUnique.OpenMU.Persistence.Initialization.Updates;
using MUnique.OpenMU.Persistence.InMemory;
using MUnique.OpenMU.PlugIns;

/// <summary>Exercises saved admin-panel metadata and legacy plain Muun configurations.</summary>
[TestFixture]
internal class MuunItemsUpdateTest
{
    /// <summary>The update preserves customized rules and can be applied twice.</summary>
    /// <param name="format">The previous configuration format: missing, plain, admin metadata or blank.</param>
    [TestCase("missing")]
    [TestCase("plain")]
    [TestCase("admin")]
    [TestCase("blank")]
    public async Task UpdatePreservesSavedSettingsAsync(string format)
    {
        var provider = new InMemoryPersistenceContextProvider();
        await new VersionSeasonSix.DataInitialization(provider, new NullLoggerFactory())
            .CreateInitialDataAsync(1, true).ConfigureAwait(false);
        using var context = provider.CreateNewContext();
        var config = (await context.GetAsync<GameConfiguration>().ConfigureAwait(false)).First();
        var previous = config.PlugInConfigurations.SingleOrDefault(p => p.TypeId == typeof(MuunFeaturePlugIn).GUID);
        if (previous is not null) config.PlugInConfigurations.Remove(previous);
        var settings = new MuunConfiguration { Capacity = 66, LifeLossOnDeath = 7 };
        settings.Pets[0].MaximumLevel = 12;
        settings.Pets[0].SpecialMultiplier = 3;
        settings.Pets[0].Maps = [0, 3];
        if (format != "missing")
        {
            var feature = context.CreateNew<PlugInConfiguration>();
            feature.TypeId = typeof(MuunFeaturePlugIn).GUID;
            config.PlugInConfigurations.Add(feature);
            if (format == "admin")
            {
                using var source = new GameConfigurationDataSource(new NullLogger<GameConfigurationDataSource>(), provider);
                await source.GetOwnerAsync(config.GetId()).ConfigureAwait(false);
                feature.SetConfiguration(settings, new ByDataSourceReferenceHandler(source));
                Assert.That(feature.CustomConfiguration, Does.Contain("$values"));
            }
            else
            {
                feature.SetConfiguration(settings, null);
                if (format == "blank") feature.CustomConfiguration = " ";
            }
        }

        var update = new MuunItemsUpdatePlugIn();
        await update.ApplyUpdateAsync(context, config).ConfigureAwait(false);
        var result = config.PlugInConfigurations.Single(p => p.TypeId == typeof(MuunFeaturePlugIn).GUID);
        var restored = result.GetConfiguration<MuunConfiguration>(null)!;
        Assert.That(result.IsActive, Is.True);
        if (format is "admin" or "plain")
        {
            Assert.That(restored.Capacity, Is.EqualTo(66));
            Assert.That(restored.LifeLossOnDeath, Is.EqualTo(7));
            Assert.That(restored.Pets[0].MaximumLevel, Is.EqualTo(12));
            Assert.That(restored.Pets[0].SpecialMultiplier, Is.EqualTo(3));
            Assert.That(restored.Pets[0].Maps, Is.EqualTo(new short[] { 0, 3 }));
        }

        Assert.That(restored.Pets.Select(p => p.ItemType), Is.Unique);
        var saved = result.CustomConfiguration;
        await update.ApplyUpdateAsync(context, config).ConfigureAwait(false);
        Assert.That(result.CustomConfiguration, Is.EqualTo(saved));
    }
}
