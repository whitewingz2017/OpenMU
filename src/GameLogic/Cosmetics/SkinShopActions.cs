// <copyright file="SkinShopActions.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Cosmetics;

using System.Runtime.CompilerServices;
using MUnique.OpenMU.GameLogic.CashShop;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.GameLogic.Views.Inventory;

/// <summary>Purchases permanent skins or timed rentals atomically with their configured prices and attributes.</summary>
public sealed class SkinShopActions
{
    /// <summary>The operation which buys a skin with WCoinC.</summary>
    public const byte PurchaseWithWCoinCOperation = 1;

    /// <summary>The operation which buys a skin with Goblin Points.</summary>
    public const byte PurchaseWithGoblinPointsOperation = 2;

    /// <summary>The operation for a one-day Goblin Points rental.</summary>
    public const byte RentOneDayOperation = 3;

    /// <summary>The operation for a three-day Goblin Points rental.</summary>
    public const byte RentThreeDaysOperation = 4;

    /// <summary>The operation for a seven-day Goblin Points rental.</summary>
    public const byte RentSevenDaysOperation = 5;

    /// <summary>The item identifier sent with a catalog request to advertise compact rows.</summary>
    public const int CompactCatalogRequest = ushort.MaxValue;

    private readonly ConditionalWeakTable<Player, Session> _sessions = new();

    /// <summary>Handles catalog (0) and purchase requests (1 with WCoinC, 2 with Goblin Points) under the player's persistence lock.</summary>
    /// <param name="player">The purchasing player.</param>
    /// <param name="requestId">The monotonically increasing session request.</param>
    /// <param name="operation">Zero for catalog, one for a purchase with WCoinC or two for a purchase with Goblin Points.</param>
    /// <param name="itemType">The item identifier, or the compact catalog marker on refresh.</param>
    /// <param name="bonus">Reserved for protocol compatibility; purchases use configured attributes and require zero.</param>
    /// <param name="quotedPrice">The price in the chosen currency which the client displayed.</param>
    /// <param name="level">The level selected in the purchase popup (0 to 15).</param>
    public async ValueTask ExecuteAsync(Player player, uint requestId, byte operation, int itemType, byte bonus, int quotedPrice, byte level = 0)
    {
        var status = await player.RunPersistenceExclusiveAsync(async () =>
        {
            var session = this._sessions.GetValue(player, _ => new());
            if (requestId == 0 || requestId <= session.LastRequest)
            {
                return "Request already processed. Refresh the shop.";
            }

            session.LastRequest = requestId;
            if (player.SelectedCharacter is null || player.Inventory is null || player.Account is null || player.IsTemplatePlayer
                || player.PlayerState.CurrentState != PlayerState.EnteredWorld || player.OpenedNpc is not null
                || player.TemporaryStorage?.Items.Any() == true || player.ShopStorage?.StoreOpen == true || player.IsCashShopOpen)
            {
                return "Close other inventory operations before using the skin shop.";
            }

            if (SkinShopFeaturePlugIn.GetSettings(player.GameContext) is not { } settings)
            {
                return "The skin shop is disabled.";
            }

            await player.ApplyPendingCashShopCoinGrantsAsync().ConfigureAwait(false);
            return operation switch
            {
                0 => "Ready",
                PurchaseWithWCoinCOperation => await PurchaseAsync(player, settings, CashShopCoinType.WCoinC, itemType, bonus, quotedPrice, level).ConfigureAwait(false),
                PurchaseWithGoblinPointsOperation => await PurchaseAsync(player, settings, CashShopCoinType.GoblinPoints, itemType, bonus, quotedPrice, level).ConfigureAwait(false),
                RentOneDayOperation => await PurchaseAsync(player, settings, CashShopCoinType.GoblinPoints, itemType, bonus, quotedPrice, level, 1).ConfigureAwait(false),
                RentThreeDaysOperation => await PurchaseAsync(player, settings, CashShopCoinType.GoblinPoints, itemType, bonus, quotedPrice, level, 3).ConfigureAwait(false),
                RentSevenDaysOperation => await PurchaseAsync(player, settings, CashShopCoinType.GoblinPoints, itemType, bonus, quotedPrice, level, 7).ConfigureAwait(false),
                _ => "Unknown skin shop operation.",
            };
        }).ConfigureAwait(false);

        await player.InvokeViewPlugInAsync<IUpdateInventoryListPlugIn>(view => view.UpdateInventoryListAsync()).ConfigureAwait(false);
        await player.InvokeViewPlugInAsync<ISkinShopView>(view => view.ShowAsync(requestId, status, operation == 0, itemType == CompactCatalogRequest)).ConfigureAwait(false);
    }

    private static async ValueTask<string> PurchaseAsync(Player player, SkinShopSettings settings, CashShopCoinType currency, int itemType, byte bonus, int quotedPrice, byte level, int rentalDays = 0)
    {
        const byte MaximumLevel = 15;
        if (level > MaximumLevel)
        {
            return "Choose a level from +0 to +15.";
        }

        if (rentalDays > 0 && !player.GameContext.PlugInManager.IsPlugInActive(typeof(ItemExpirationPlugIn)))
        {
            return "Skin rentals require the Item expiration plugin.";
        }

        var definition = player.GameContext.Configuration.Items.SingleOrDefault(d => (d.Group * 512) + d.Number == itemType);
        if (definition is null || !FenrirMountRules.IsShopItem(definition) || !CosmeticArmorRules.CanPurchase(player, definition))
        {
            return "This skin is unavailable for your character class.";
        }

        if (bonus != 0)
        {
            return player.GetLocalizedMessage("SkinShopFixedAttributesOnly");
        }

        var currencyName = currency == CashShopCoinType.GoblinPoints ? "Goblin Points" : "WCoinC";
        var price = rentalDays > 0 ? settings.RentalPrice(definition, rentalDays)
            : currency == CashShopCoinType.GoblinPoints ? settings.GoblinPrice(definition) : settings.Price(definition);
        if (price <= 0 || quotedPrice != price)
        {
            return $"This skin is not for sale with {currencyName}, or its price changed. Refresh the shop.";
        }

        var account = player.Account!;
        var balance = account.GetCashShopCoins(currency);
        if (balance < price)
        {
            return $"Not enough {currencyName}.";
        }

        var item = player.PersistenceContext.CreateNew<Item>();
        item.Definition = definition;
        item.Durability = definition.Durability;
        item.Level = level;
        item.ExpiresAtUtc = rentalDays > 0 ? DateTime.UtcNow.AddDays(rentalDays) : null;
        item.HasSkill = FenrirMountRules.IsCustom(definition) && definition.Skill is not null;
        var committed = false;
        try
        {
            if (!await player.Inventory!.AddItemAsync(item).ConfigureAwait(false))
            {
                return "Make room in your inventory first.";
            }

            account.SetCashShopCoins(currency, balance - price);
            if (!await player.SaveProgressAsync().ConfigureAwait(false))
            {
                return $"Saving failed. No {currencyName} was spent.";
            }

            committed = true;
            return rentalDays > 0 ? $"Skin rented for {rentalDays} day(s). Its timer continues offline." : "Skin purchased. Equip it on the Skin page.";
        }
        catch (Exception exception)
        {
            player.Logger.LogError(exception, "Skin purchase failed for {Player}.", player);
            return $"Saving failed. No {currencyName} was spent.";
        }
        finally
        {
            if (!committed)
            {
                await RollbackPurchaseAsync(player, item, currency, balance).ConfigureAwait(false);
            }
        }
    }

    private static async ValueTask RollbackPurchaseAsync(Player player, Item item, CashShopCoinType currency, int balance)
    {
        player.Account!.SetCashShopCoins(currency, balance);
        if (player.Inventory!.Items.Contains(item))
        {
            await player.Inventory.RemoveItemAsync(item).ConfigureAwait(false);
        }

        await player.PersistenceContext.DeleteAsync(item).ConfigureAwait(false);
    }

    private sealed class Session
    {
        public uint LastRequest { get; set; }
    }
}
