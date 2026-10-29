// <copyright file="SkinShopActions.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Cosmetics;

using System.Runtime.CompilerServices;
using MUnique.OpenMU.GameLogic.CashShop;
using MUnique.OpenMU.GameLogic.Views.Inventory;

/// <summary>Purchases one permanent skin and its chosen bonus atomically with WCoinC.</summary>
public sealed class SkinShopActions
{
    private readonly ConditionalWeakTable<Player, Session> _sessions = new();

    /// <summary>Handles catalog (0) and purchase (1) requests under the player's persistence lock.</summary>
    /// <param name="player">The purchasing player.</param>
    /// <param name="requestId">The monotonically increasing session request.</param>
    /// <param name="operation">Zero for catalog or one for purchase.</param>
    /// <param name="itemType">The item group times 512 plus its number.</param>
    /// <param name="bonus">The configured option number, or zero for pets.</param>
    /// <param name="quotedPrice">The WCoinC price displayed by the client.</param>
    public async ValueTask ExecuteAsync(Player player, uint requestId, byte operation, int itemType, byte bonus, int quotedPrice)
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
            return operation == 0 ? "Ready" : operation == 1
                ? await PurchaseAsync(player, settings, itemType, bonus, quotedPrice).ConfigureAwait(false)
                : "Unknown skin shop operation.";
        }).ConfigureAwait(false);

        await player.InvokeViewPlugInAsync<IUpdateInventoryListPlugIn>(view => view.UpdateInventoryListAsync()).ConfigureAwait(false);
        await player.InvokeViewPlugInAsync<ISkinShopView>(view => view.ShowAsync(requestId, status, operation == 0)).ConfigureAwait(false);
    }

    private static async ValueTask<string> PurchaseAsync(Player player, SkinShopSettings settings, int itemType, byte bonus, int quotedPrice)
    {
        var definition = player.GameContext.Configuration.Items.SingleOrDefault(d => (d.Group * 512) + d.Number == itemType);
        if (definition is null || !FenrirMountRules.IsShopItem(definition) || !CosmeticArmorRules.CanEquip(player, definition))
        {
            return "This skin is unavailable for your character class.";
        }

        var choices = SkinBonuses.Choices(definition).ToArray();
        var option = choices.SingleOrDefault(o => o.Number == bonus);
        if (choices.Length == 0 ? bonus != 0 : option is null)
        {
            return "Choose one of this skin's available bonuses.";
        }

        var price = settings.Price(definition);
        if (price <= 0 || quotedPrice != price)
        {
            return "This category is not for sale, or its price changed. Refresh the shop.";
        }

        var account = player.Account!;
        var balance = account.WCoinC;
        if (balance < price)
        {
            return "Not enough WCoinC.";
        }

        var item = player.PersistenceContext.CreateNew<Item>();
        item.Definition = definition;
        item.Durability = definition.Durability;
        item.HasSkill = FenrirMountRules.IsCustom(definition) && definition.Skill is not null;
        ItemOptionLink? link = null;
        var committed = false;
        try
        {
            if (option is not null)
            {
                link = player.PersistenceContext.CreateNew<ItemOptionLink>();
                link.ItemOption = option;
                link.Level = 1;
                item.ItemOptions.Add(link);
            }

            if (!await player.Inventory!.AddItemAsync(item).ConfigureAwait(false))
            {
                return "Make room in your inventory first.";
            }

            account.WCoinC = balance - price;
            if (!await player.SaveProgressAsync().ConfigureAwait(false))
            {
                return "Saving failed. No WCoinC was spent.";
            }

            committed = true;
            return "Skin purchased. Equip it on the Skin page.";
        }
        catch (Exception exception)
        {
            player.Logger.LogError(exception, "Skin purchase failed for {Player}.", player);
            return "Saving failed. No WCoinC was spent.";
        }
        finally
        {
            if (!committed)
            {
                account.WCoinC = balance;
                if (player.Inventory!.Items.Contains(item))
                {
                    await player.Inventory.RemoveItemAsync(item).ConfigureAwait(false);
                }

                if (link is not null)
                {
                    item.ItemOptions.Remove(link);
                    await player.PersistenceContext.DeleteAsync(link).ConfigureAwait(false);
                }

                await player.PersistenceContext.DeleteAsync(item).ConfigureAwait(false);
            }
        }
    }

    private sealed class Session
    {
        public uint LastRequest { get; set; }
    }
}
