// <copyright file="AddCashShopUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.CashShop;
using MUnique.OpenMU.GameLogic.PlayerActions.ItemConsumeActions;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.GameServer.RemoteView.Inventory;
using MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix;
using MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Items;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Adds the cash shop configuration with the packages of the cash shop script
/// which comes with the game client, and the items which delivery plugins apply
/// when a player uses them from the cash shop storage.
/// </summary>
/// <remarks>
/// An existing configuration is kept; the items of the delivery plugins are assigned to its
/// products which don't have an item yet.
/// </remarks>
[PlugIn]
[Display(Name = nameof(MUnique.OpenMU.Persistence.Initialization.Properties.PlugInResources.AddCashShopUpdatePlugIn_Name), Description = nameof(MUnique.OpenMU.Persistence.Initialization.Properties.PlugInResources.AddCashShopUpdatePlugIn_Description), ResourceType = typeof(MUnique.OpenMU.Persistence.Initialization.Properties.PlugInResources))]
[Guid("C6B9A9DB-FFA6-4C66-B9E7-43166A06B2D5")]
public class AddCashShopUpdatePlugIn : UpdatePlugInBase
{
    /// <summary>
    /// The plug-in name.
    /// </summary>
    internal const string PlugInName = "Add cash shop";

    /// <summary>
    /// The plug-in description.
    /// </summary>
    internal const string PlugInDescription = "This update adds the cash shop configuration with the packages of the cash shop script 512.2012.084 of the game client, and the items which are applied when they're used from the cash shop storage (character cards, vault and inventory extension). Version 4 adds missing catalog definitions and repairs item links, configures seals and scrolls, and enables timed effect persistence, expiration, elite potions and reset fruits. Gold Channel and Premium Service remain unavailable until their access rules or benefits are configured. Existing prices, durations and explicit item assignments are preserved.";

    /// <summary>
    /// The item numbers (of group 14) of the items of the delivery plugins, by the product sequence numbers of the script.
    /// </summary>
    private static readonly IReadOnlyDictionary<int, short> DeliveryItemNumbers = new Dictionary<int, short>
    {
        { 31, 91 }, // Summoner Character Card
        { 281, 162 }, // Magic Backpack
        { 282, 163 }, // Vault Expansion Certificate
        { 284, 169 }, // Rage Fighter Character Card
    };

    /// <inheritdoc />
    public override int Version => 4;

    /// <inheritdoc />
    public override DateTime UpdatedAt => new(2026, 10, 20, 0, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    /// <remarks>
    /// Not every server wants to offer a cash shop, so the update can be deselected.
    /// </remarks>
    public override bool IsMandatory => false;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 10, 07, 12, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        new CashShopItems(context, gameConfiguration).Initialize();
        var initializer = new CashShopInitializer(context, gameConfiguration);
        initializer.Initialize();
        var repairedPackages = initializer.RepairExistingItemReferences();
        AssignDeliveryItems(gameConfiguration);
        foreach (var type in new[] { typeof(ItemExpirationPlugIn), typeof(ItemExpirationViewPlugIn), typeof(EliteHealthPotionConsumeHandlerPlugIn), typeof(EliteManaPotionConsumeHandlerPlugIn), typeof(EliteShieldPotionConsumeHandlerPlugIn), typeof(ResetStrengthFruitConsumeHandlerPlugIn), typeof(ResetAgilityFruitConsumeHandlerPlugIn), typeof(ResetVitalityFruitConsumeHandlerPlugIn), typeof(ResetEnergyFruitConsumeHandlerPlugIn), typeof(ResetLeadershipFruitConsumeHandlerPlugIn) })
        {
            if (gameConfiguration.PlugInConfigurations.All(p => p.TypeId != type.GUID))
            {
                var plugIn = context.CreateNew<PlugInConfiguration>();
                plugIn.TypeId = type.GUID;
                plugIn.IsActive = true;
                gameConfiguration.PlugInConfigurations.Add(plugIn);
            }
        }

        if (gameConfiguration.CashShopConfiguration is { } shop)
        {
            foreach (var package in shop.Packages.Where(p => (p.Products.Any(product => product.Duration > TimeSpan.Zero) || repairedPackages.Contains(p))
                && p.Products.All(product => product.CanBeDelivered())))
            {
                package.IsForSale = true;
            }
        }

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Assigns the items of the delivery plugins to the products of a configuration which was created
    /// without them. Their packages were initialized as not for sale then, so they're put on sale.
    /// </summary>
    private static void AssignDeliveryItems(GameConfiguration gameConfiguration)
    {
        if (gameConfiguration.CashShopConfiguration is not { } configuration)
        {
            return;
        }

        foreach (var package in configuration.Packages)
        {
            var assigned = false;
            foreach (var product in package.Products.Where(p => p.ItemDefinition is null))
            {
                if (DeliveryItemNumbers.TryGetValue(product.ProductSequence, out var number)
                    && gameConfiguration.Items.FirstOrDefault(item => item.Group == 14 && item.Number == number) is { } item)
                {
                    product.ItemDefinition = item;
                    assigned = true;
                }
            }

            if (assigned && package.Products.All(product => product.CanBeDelivered()))
            {
                package.IsForSale = true;
            }
        }
    }
}
