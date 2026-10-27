// <copyright file="StoreCurrencyPurchase.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions.PlayerStore;

using MUnique.OpenMU.GameLogic.CashShop;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.GameLogic.Views;
using MUnique.OpenMU.GameLogic.Views.Inventory;
using MUnique.OpenMU.GameLogic.Views.PlayerShop;

/// <summary>Transfers a sold item and its inventory-jewel or account-coin payment in one persistence save.</summary>
internal sealed class StoreCurrencyPurchase
{
    /// <summary>Buys an extended-price item. Both player persistence locks must be held by the caller.</summary>
    public async ValueTask BuyAsync(Player buyer, Player seller, byte slot)
    {
        if (buyer.Account is null || seller.Account is null || buyer.Inventory is null || seller.Inventory is null
            || seller.ShopStorage is null || ReferenceEquals(buyer.Account, seller.Account))
        {
            return;
        }

        var sold = false;
        using (await seller.ShopStorage.StoreLock.LockAsync().ConfigureAwait(false))
        {
            var item = seller.ShopStorage.GetItem(slot);
            if (!seller.ShopStorage.StoreOpen || item?.StorePrice is not { } amount
                || item.Definition is not { IsPersonalStoreSellable: true }
                || !StoreCurrency.IsValid(item.StorePriceItemType, amount, StoreCurrencyFeaturePlugIn.GetSettings(buyer.GameContext)))
            {
                await ResultAsync(buyer, seller, ItemBuyResult.NameMismatchOrPriceMissing).ConfigureAwait(false);
                return;
            }

            var currency = item.StorePriceItemType;
            var coins = StoreCurrency.IsCoin(currency);
            var payment = coins ? [] : buyer.Inventory.Items.Where(jewel => StoreCurrency.IsPaymentJewel(jewel, currency)).Take(amount).ToList();
            var coinType = coins ? StoreCurrency.CoinType(currency) : default;
            var buyerBalance = coins ? buyer.Account.GetCashShopCoins(coinType) : 0;
            var sellerBalance = coins ? seller.Account.GetCashShopCoins(coinType) : 0;
            if ((coins && buyerBalance < amount) || (!coins && payment.Count != amount))
            {
                await ResultAsync(buyer, seller, ItemBuyResult.LackOfMoney).ConfigureAwait(false);
                return;
            }

            if (coins && (long)sellerBalance + amount > int.MaxValue)
            {
                await ResultAsync(buyer, seller, ItemBuyResult.MoneyOverflowOrNotEnoughSpace).ConfigureAwait(false);
                return;
            }

            var originalSlots = payment.Select(jewel => jewel.ItemSlot).ToArray();
            using var transaction = buyer.GameContext.PersistenceContextProvider.CreateNewPlayerContext(buyer.GameContext.Configuration);
            transaction.Attach(buyer.Account);
            transaction.Attach(seller.Account);
            // Test players and offline projections may not expose their characters through Account.Characters.
            transaction.Attach(buyer.SelectedCharacter!);
            transaction.Attach(seller.SelectedCharacter!);
            var committed = false;
            var itemMoved = false;
            var transferred = new List<Item>();
            try
            {
                foreach (var jewel in payment)
                {
                    await buyer.Inventory.RemoveItemAsync(jewel).ConfigureAwait(false);
                    if (!await seller.Inventory.AddItemAsync(jewel).ConfigureAwait(false))
                    {
                        await buyer.Inventory.AddItemAsync(originalSlots[payment.IndexOf(jewel)], jewel).ConfigureAwait(false);
                        await ResultAsync(buyer, seller, ItemBuyResult.MoneyOverflowOrNotEnoughSpace).ConfigureAwait(false);
                        return;
                    }

                    transferred.Add(jewel);
                }

                var free = buyer.Inventory.CheckInvSpace(item);
                if (free is null)
                {
                    await ResultAsync(buyer, seller, ItemBuyResult.MoneyOverflowOrNotEnoughSpace).ConfigureAwait(false);
                    return;
                }

                await seller.ShopStorage.RemoveItemAsync(item).ConfigureAwait(false);
                itemMoved = true;
                if (!await buyer.Inventory.AddItemAsync((byte)free, item).ConfigureAwait(false))
                {
                    throw new InvalidOperationException("Reserved buyer slot unavailable.");
                }

                item.StorePrice = null;
                item.StorePriceItemType = 0;
                if (coins)
                {
                    buyer.Account.SetCashShopCoins(coinType, buyerBalance - amount);
                    seller.Account.SetCashShopCoins(coinType, sellerBalance + amount);
                }

                if (!await transaction.SaveChangesAsync().ConfigureAwait(false))
                {
                    throw new InvalidOperationException("Store transaction was not saved.");
                }

                committed = true;
                sold = true;
                foreach (var jewel in payment)
                {
                    buyer.PersistenceContext.Detach(jewel);
                    seller.PersistenceContext.Attach(jewel);
                }

                seller.PersistenceContext.Detach(item);
                buyer.PersistenceContext.Attach(item);
                await seller.InvokeViewPlugInAsync<IItemSoldByPlayerShopPlugIn>(p => p.ItemSoldByPlayerShopAsync(slot, buyer)).ConfigureAwait(false);
                await buyer.InvokeViewPlugInAsync<IPlayerShopBuyRequestResultPlugIn>(p => p.ShowResultAsync(seller, ItemBuyResult.Success, item)).ConfigureAwait(false);
                buyer.GameContext.PlugInManager.GetPlugInPoint<IItemSoldToOtherPlayerPlugIn>()?.ItemSold(seller, item, buyer);
            }
            finally
            {
                if (!committed)
                {
                    if (coins)
                    {
                        buyer.Account.SetCashShopCoins(coinType, buyerBalance);
                        seller.Account.SetCashShopCoins(coinType, sellerBalance);
                    }

                    if (itemMoved)
                    {
                        await buyer.Inventory.RemoveItemAsync(item).ConfigureAwait(false);
                        item.StorePrice = amount;
                        item.StorePriceItemType = currency;
                        // The public add operation rejects an open store, including rollback.
                        seller.ShopStorage.StoreOpen = false;
                        try
                        {
                            if (!await seller.ShopStorage.AddItemAsync(slot, item).ConfigureAwait(false))
                            {
                                throw new InvalidOperationException("Could not restore sold item after failed payment.");
                            }
                        }
                        finally
                        {
                            seller.ShopStorage.StoreOpen = true;
                        }
                    }

                    foreach (var jewel in transferred)
                    {
                        await seller.Inventory.RemoveItemAsync(jewel).ConfigureAwait(false);
                        await buyer.Inventory.AddItemAsync(originalSlots[payment.IndexOf(jewel)], jewel).ConfigureAwait(false);
                    }
                }
            }
        }

        if (!sold)
        {
            return;
        }

        await buyer.InvokeViewPlugInAsync<IUpdateInventoryListPlugIn>(p => p.UpdateInventoryListAsync()).ConfigureAwait(false);
        await seller.InvokeViewPlugInAsync<IUpdateInventoryListPlugIn>(p => p.UpdateInventoryListAsync()).ConfigureAwait(false);
        if (seller.ShopStorage.Items.Any())
        {
            await buyer.InvokeViewPlugInAsync<IShowShopItemListPlugIn>(p => p.ShowShopItemListAsync(seller, true)).ConfigureAwait(false);
        }
        else
        {
            await new CloseStoreAction().CloseStoreAsync(seller).ConfigureAwait(false);
        }
    }

    private static ValueTask ResultAsync(Player buyer, Player seller, ItemBuyResult result)
        => buyer.InvokeViewPlugInAsync<IPlayerShopBuyRequestResultPlugIn>(p => p.ShowResultAsync(seller, result, null));
}
