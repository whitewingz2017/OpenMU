// <copyright file="GemstoneBankUpdateTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Tests;

using Microsoft.Extensions.Logging.Abstractions;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.JewelBank;
using MUnique.OpenMU.Persistence.Initialization.Updates;
using MUnique.OpenMU.Persistence.InMemory;
using MUnique.OpenMU.PlugIns;

/// <summary>Checks fresh defaults and repair of saved bank settings.</summary>
[TestFixture]
internal class GemstoneBankUpdateTest
{
    /// <summary>The update adds Gemstone once and retains custom settings.</summary>
    [Test]
    public async Task FreshDefaultsAndSavedConfigurationSupportGemstoneAsync()
    {
        var provider = new InMemoryPersistenceContextProvider();
        await new VersionSeasonSix.DataInitialization(provider, new NullLoggerFactory()).CreateInitialDataAsync(1, true).ConfigureAwait(false);
        using var context = provider.CreateNewContext();
        var config = (await context.GetAsync<GameConfiguration>().ConfigureAwait(false)).First();
        var gemstone = config.Items.Single(i => i.Group == 14 && i.Number == 41);
        var itemId = (gemstone.Group * 512) + gemstone.Number;
        Assert.That(new JewelBankConfiguration().Items, Does.Contain(itemId));
        var handlerId = new Guid("3a2e4ae2-6965-4598-a1d1-17d10331184f");
        var feature = config.PlugInConfigurations.Single(p => p.TypeId == handlerId);
        Assert.That(feature.GetConfiguration<JewelBankConfiguration>(null)!.Items, Does.Contain(itemId));
        feature.IsActive = false;
        feature.SetConfiguration(new JewelBankConfiguration { Enabled = false, MaximumBalance = 123, Items = [7181] }, null);
        var update = new GemstoneBankUpdatePlugIn();
        await update.ApplyUpdateAsync(context, config).ConfigureAwait(false);
        var saved = feature.CustomConfiguration;
        await update.ApplyUpdateAsync(context, config).ConfigureAwait(false);
        var settings = feature.GetConfiguration<JewelBankConfiguration>(null)!;
        Assert.That(settings.Items, Is.EqualTo(new[] { 7181, itemId }));
        Assert.That(settings.Enabled, Is.False);
        Assert.That(settings.MaximumBalance, Is.EqualTo(123));
        Assert.That(feature.IsActive, Is.False);
        Assert.That(feature.CustomConfiguration, Is.EqualTo(saved));
        Assert.That(update.IsMandatory, Is.False);
        Assert.That(update.Name, Is.Not.Empty);
    }
}
