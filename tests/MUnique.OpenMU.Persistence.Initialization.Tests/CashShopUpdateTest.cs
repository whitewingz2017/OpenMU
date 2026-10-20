// <copyright file="CashShopUpdateTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Tests;

using Microsoft.Extensions.Logging.Abstractions;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.CashShop;
using MUnique.OpenMU.Persistence.Initialization.Updates;
using MUnique.OpenMU.Persistence.InMemory;

/// <summary>
/// Tests the initialization of the cash shop configuration for new and existing season 6 databases.
/// </summary>
[TestFixture]
internal class CashShopUpdateTest
{
    /// <summary>
    /// The packages of the items which delivery plugins apply: the Summoner and Rage Fighter
    /// Character Cards, the Magic Backpack and the Vault Expansion Certificate, for WCoin (C) and (P).
    /// </summary>
    private static readonly int[] DeliveryPackages = [126, 127, 258, 259, 260, 261, 263, 264];

    /// <summary>
    /// Tests that a freshly initialized database contains the cash shop configuration
    /// which matches the cash shop script 512.2012.084 of the game client.
    /// </summary>
    [Test]
    public async Task FreshDatabaseContainsCashShopAsync()
    {
        var (context, gameConfiguration) = await CreateSeason6ConfigurationAsync().ConfigureAwait(false);
        using var _ = context;

        var configuration = gameConfiguration.CashShopConfiguration;
        Assert.That(configuration, Is.Not.Null);
        Assert.That(configuration!.ScriptSaleZone, Is.EqualTo(512));
        Assert.That(configuration.ScriptYear, Is.EqualTo(2012));
        Assert.That(configuration.ScriptYearId, Is.EqualTo(84));
        Assert.That(configuration.Packages, Has.Count.EqualTo(205));
        Assert.That(configuration.Packages.Select(p => p.PackageSequence), Is.Unique);
        Assert.That(configuration.Packages, Has.All.Matches<CashShopPackage>(p => p.Products.Select(product => product.PriceSequence).Distinct().Count() == p.Products.Count));
        Assert.That(
            configuration.Packages.Where(p => p.IsForSale).SelectMany(p => p.Products),
            Has.All.Matches<CashShopProduct>(product => product.CanBeDelivered()));
        Assert.That(configuration.Packages.Count(p => p.IsForSale), Is.GreaterThan(21));
        Assert.That(configuration.Packages.Where(p => DeliveryPackages.Contains(p.PackageSequence)), Has.All.Matches<CashShopPackage>(p => p.IsForSale));

        var seal = configuration.Packages.Single(p => p.PackageSequence == 122);
        Assert.That(seal.Name, Is.EqualTo("Seal of Wealth"));
        Assert.That(seal.CoinType, Is.EqualTo(CashShopCoinType.WCoinC));
        Assert.That(seal.IsBundle, Is.False);
        Assert.That(seal.Products.Select(p => p.Duration), Is.EquivalentTo(new[] { TimeSpan.FromDays(1), TimeSpan.FromDays(3), TimeSpan.FromDays(7), TimeSpan.FromDays(30) }));
    }

    /// <summary>
    /// Tests that the update adds the cash shop configuration to an existing database
    /// which doesn't have it yet, and that applying it twice doesn't change anything.
    /// </summary>
    [Test]
    public async Task UpdateAddsCashShopToExistingDatabaseAsync()
    {
        var (context, gameConfiguration) = await CreateSeason6ConfigurationAsync().ConfigureAwait(false);
        using var _ = context;
        gameConfiguration.CashShopConfiguration = null;

        var update = new AddCashShopUpdatePlugIn();
        await update.ApplyUpdateAsync(context, gameConfiguration).ConfigureAwait(false);
        var configuration = gameConfiguration.CashShopConfiguration;
        await update.ApplyUpdateAsync(context, gameConfiguration).ConfigureAwait(false);

        Assert.That(configuration, Is.Not.Null);
        Assert.That(gameConfiguration.CashShopConfiguration, Is.SameAs(configuration));
        Assert.That(configuration!.Packages, Has.Count.EqualTo(205));
    }

    /// <summary>
    /// Tests that the update assigns the items of the delivery plugins to the products of a configuration
    /// which was created without these items, and puts their
    /// packages on sale. Applying it twice doesn't add the items twice.
    /// </summary>
    [Test]
    public async Task UpdateAssignsDeliveryItemsToExistingCatalogAsync()
    {
        var (context, gameConfiguration) = await CreateSeason6ConfigurationAsync().ConfigureAwait(false);
        using var _ = context;
        var configuration = gameConfiguration.CashShopConfiguration!;
        var deliveryItems = gameConfiguration.Items.Where(IsDeliveryItem).ToList();
        foreach (var package in configuration.Packages.Where(p => DeliveryPackages.Contains(p.PackageSequence)))
        {
            package.IsForSale = false;
            package.Products.ToList().ForEach(product => product.ItemDefinition = null);
        }

        deliveryItems.ForEach(item => gameConfiguration.Items.Remove(item));

        var update = new AddCashShopUpdatePlugIn();
        await update.ApplyUpdateAsync(context, gameConfiguration).ConfigureAwait(false);
        await update.ApplyUpdateAsync(context, gameConfiguration).ConfigureAwait(false);

        Assert.That(gameConfiguration.Items.Count(IsDeliveryItem), Is.EqualTo(4));
        Assert.That(configuration.Packages.Count(p => p.IsForSale), Is.GreaterThan(21));
        Assert.That(
            configuration.Packages.Where(p => DeliveryPackages.Contains(p.PackageSequence)).SelectMany(p => p.Products),
            Has.All.Matches<CashShopProduct>(product => product.ItemDefinition is { } item && IsDeliveryItem(item)));
    }

    /// <summary>
    /// Version two upgrades the old disabled timed catalog without duplicating plugins.
    /// </summary>
    [Test]
    public async Task UpdateEnablesTimedWearablesAsync()
    {
        var (context, gameConfiguration) = await CreateSeason6ConfigurationAsync().ConfigureAwait(false);
        using var _ = context;
        var packages = gameConfiguration.CashShopConfiguration!.Packages
            .Where(p => p.Products.Any(product => product.Duration > TimeSpan.Zero)
                && p.Products.All(product => product.CanBeDelivered())).ToList();
        Assert.That(packages, Is.Not.Empty);
        packages.ForEach(p => p.IsForSale = false);
        var update = new AddCashShopUpdatePlugIn();
        await update.ApplyUpdateAsync(context, gameConfiguration).ConfigureAwait(false);
        var pluginCount = gameConfiguration.PlugInConfigurations.Count;
        await update.ApplyUpdateAsync(context, gameConfiguration).ConfigureAwait(false);
        Assert.That(packages, Has.All.Matches<CashShopPackage>(p => p.IsForSale));
        Assert.That(gameConfiguration.PlugInConfigurations.Count, Is.EqualTo(pluginCount));
        Assert.That(update.Version, Is.EqualTo(6));
    }

    /// <summary>
    /// Repairs stale pet and non-pet links without replacing owner pricing or duplicating products.
    /// </summary>
    [Test]
    public async Task UpdateRepairsMissingSupportedProductItemsAsync()
    {
        var (context, gameConfiguration) = await CreateSeason6ConfigurationAsync().ConfigureAwait(false);
        using var _ = context;
        var shop = gameConfiguration.CashShopConfiguration!;
        var packages = shop.Packages.Where(p => new[] { 10, 28, 30, 100, 102, 385 }.Contains(p.PackageSequence)).ToList();
        var products = packages.SelectMany(p => p.Products).ToList();
        var expectedItems = products.Select(p => p.ItemDefinition).ToArray();
        var productCount = shop.Packages.Sum(p => p.Products.Count);
        packages.ForEach(p => p.IsForSale = false);
        products.ForEach(p => { p.ItemDefinition = null; p.Price = 987; });

        var update = new AddCashShopUpdatePlugIn();
        await update.ApplyUpdateAsync(context, gameConfiguration).ConfigureAwait(false);
        await update.ApplyUpdateAsync(context, gameConfiguration).ConfigureAwait(false);

        Assert.That(packages, Has.Count.EqualTo(6));
        Assert.That(packages, Has.All.Matches<CashShopPackage>(p => p.IsForSale));
        Assert.That(products.Select(p => p.ItemDefinition), Is.EqualTo(expectedItems));
        Assert.That(products, Has.All.Matches<CashShopProduct>(p => p.Price == 987));
        Assert.That(shop.Packages.Sum(p => p.Products.Count), Is.EqualTo(productCount));
        var unsupported = shop.Packages.Single(p => p.PackageSequence == 122);
        Assert.That(unsupported.IsForSale, Is.True);
        Assert.That(unsupported.Products, Has.All.Matches<CashShopProduct>(p => p.ItemDefinition?.ConsumeEffect?.PowerUpDefinitions.Count > 0));
    }

    /// <summary>
    /// The upgrade creates missing buff definitions and repairs links in an old catalog.
    /// </summary>
    [Test]
    public async Task UpdateRepairsMissingBuffDefinitionsAsync()
    {
        var (context, gameConfiguration) = await CreateSeason6ConfigurationAsync().ConfigureAwait(false);
        using var _ = context;
        var shop = gameConfiguration.CashShopConfiguration!;
        var products = shop.Packages.SelectMany(package => package.Products)
            .Where(product => product.Duration > TimeSpan.Zero && product.ItemDefinition?.ConsumeEffect?.PowerUpDefinitions.Count > 0).ToList();
        Assert.That(products, Is.Not.Empty);
        var missing = products.Select(product => product.ItemDefinition!).Distinct().ToList();
        var dropRate = gameConfiguration.Attributes.Single(attribute => attribute.Id == Stats.ItemDropRate.Id);
        gameConfiguration.Attributes.Remove(dropRate);
        foreach (var characterClass in gameConfiguration.CharacterClasses)
        {
            foreach (var value in characterClass.BaseAttributeValues.Where(value => value.Definition == dropRate).ToList())
            {
                characterClass.BaseAttributeValues.Remove(value);
            }
        }

        foreach (var item in missing)
        {
            gameConfiguration.MagicEffects.Remove(item.ConsumeEffect!);
            gameConfiguration.Items.Remove(item);
        }

        foreach (var product in products)
        {
            product.ItemDefinition = null;
        }

        var update = new AddCashShopUpdatePlugIn();
        await update.ApplyUpdateAsync(context, gameConfiguration).ConfigureAwait(false);
        var itemCount = gameConfiguration.Items.Count;
        await update.ApplyUpdateAsync(context, gameConfiguration).ConfigureAwait(false);

        Assert.That(products, Has.All.Matches<CashShopProduct>(product => product.CanBeDelivered()
            && product.ItemDefinition?.ConsumeEffect?.PowerUpDefinitions.Count > 0));
        Assert.That(shop.Packages.Single(package => package.PackageSequence == 122).Products,
            Has.All.Matches<CashShopProduct>(product => product.ItemDefinition is { Group: 13, Number: 44 }));
        Assert.That(gameConfiguration.Items.Count, Is.EqualTo(itemCount));
        var persistentDropRate = gameConfiguration.Attributes.Single(attribute => attribute.Id == Stats.ItemDropRate.Id);
        Assert.That(gameConfiguration.CharacterClasses, Has.All.Matches<CharacterClass>(characterClass =>
            characterClass.BaseAttributeValues.Count(value => value.Definition == persistentDropRate && value.Value == 1) == 1));
        Assert.That(shop.Packages.Single(package => package.PackageSequence == 122).Products,
            Has.All.Matches<CashShopProduct>(product => product.ItemDefinition!.ConsumeEffect!.PowerUpDefinitions
                .Any(bonus => ReferenceEquals(bonus.TargetAttribute, persistentDropRate))));
    }

    /// <summary>
    /// An existing item link must not hide an unconfigured purchased effect.
    /// </summary>
    [Test]
    public async Task UpdateRepairsExistingEmptyBuffEffectAsync()
    {
        var (context, gameConfiguration) = await CreateSeason6ConfigurationAsync().ConfigureAwait(false);
        using var _ = context;
        var seal = gameConfiguration.Items.Single(item => item.Group == 13 && item.Number == 44);
        var effect = seal.ConsumeEffect!;
        effect.PowerUpDefinitions.Clear();
        var update = new AddCashShopUpdatePlugIn();

        await update.ApplyUpdateAsync(context, gameConfiguration).ConfigureAwait(false);
        await update.ApplyUpdateAsync(context, gameConfiguration).ConfigureAwait(false);

        Assert.That(seal.ConsumeEffect, Is.SameAs(effect));
        Assert.That(effect.PowerUpDefinitions, Has.Count.EqualTo(2));
    }

    /// <summary>
    /// Every catalog option must have a real definition and delivery path.
    /// </summary>
    [Test]
    public async Task EveryCatalogProductHasDeliveryAsync()
    {
        var (context, gameConfiguration) = await CreateSeason6ConfigurationAsync().ConfigureAwait(false);
        using var _ = context;
        // These script products are Gold Channel access and the undefined Premium Service.
        int[] deferred = [528, 149, 1352, 526, 527, 98, 912, 913, 914, 915];
        var unavailable = gameConfiguration.CashShopConfiguration!.Packages
            .SelectMany(package => package.Products.Where(product => !deferred.Contains(product.ProductSequence) && !product.CanBeDelivered())
                .Select(product => $"{package.Name}: product {product.ProductSequence}, price {product.PriceSequence}, item {product.ItemDefinition}"))
            .Distinct().ToArray();
        Assert.That(unavailable, Is.Empty, string.Join(Environment.NewLine, unavailable));
    }

    private static bool IsDeliveryItem(DataModel.Configuration.Items.ItemDefinition item)
    {
        return item.Group == 14 && item.Number is 91 or 162 or 163 or 169;
    }

    private static async Task<(IContext Context, GameConfiguration GameConfiguration)> CreateSeason6ConfigurationAsync()
    {
        var contextProvider = new InMemoryPersistenceContextProvider();
        var dataInitialization = new VersionSeasonSix.DataInitialization(contextProvider, new NullLoggerFactory());
        await dataInitialization.CreateInitialDataAsync(1, true).ConfigureAwait(false);

        var context = contextProvider.CreateNewContext();
        var gameConfiguration = (await context.GetAsync<GameConfiguration>().ConfigureAwait(false)).First();
        return (context, gameConfiguration);
    }
}
