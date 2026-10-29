// <copyright file="StoreMailPurchase.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions.PlayerStore;

using MUnique.OpenMU.GameLogic.CashShop;
using MUnique.OpenMU.GameLogic.Views;
using MUnique.OpenMU.GameLogic.Views.Inventory;
using MUnique.OpenMU.GameLogic.Views.PlayerShop;
using MUnique.OpenMU.Persistence;

/// <summary>Atomically exchanges every required payment for a claimable item in buyer mail.</summary>
internal sealed class StoreMailPurchase
{
    /// <summary>Both player persistence locks are held by BuyRequestAction before the store lock.</summary>
    /// <param name="buyer">The buyer.</param>
    /// <param name="seller">The seller.</param>
    /// <param name="slot">The slot.</param>
    public async ValueTask BuyAsync(Player buyer, Player seller, byte slot)
    {
        if (buyer.Account is null || seller.Account is null || ReferenceEquals(buyer.Account, seller.Account) || (buyer.Account.GetId() != Guid.Empty && buyer.Account.GetId() == seller.Account.GetId())
            || buyer.Inventory is null || seller.Inventory is null || seller.ShopStorage is null)
        {
            return;
        }

        LetterBody buyerMail;
        LetterBody sellerMail;
        using (await seller.ShopStorage.StoreLock.LockAsync().ConfigureAwait(false))
        {
            var item = seller.ShopStorage.GetItem(slot);
            if (!seller.ShopStorage.StoreOpen || item?.Definition is not { IsPersonalStoreSellable: true })
            {
                return;
            }

            var price = string.IsNullOrEmpty(item.StorePricesJson)
                ? new[] { new StorePricePart(item.StorePriceItemType, item.StorePrice ?? 0) }
                : CombinedStorePrice.Read(item);
            if (!CombinedStorePrice.IsValid(price, StoreCurrencyFeaturePlugIn.GetSettings(buyer.GameContext)))
            {
                return;
            }

            var jewels = new List<Item>();
            foreach (var part in price)
            {
                if ((part.Type == 0 && buyer.Money < part.Amount)
                    || (StoreCurrency.IsCoin(part.Type) && buyer.Account.GetCashShopCoins(StoreCurrency.CoinType(part.Type)) < part.Amount))
                {
                    await FailAsync(buyer, seller, ItemBuyResult.LackOfMoney).ConfigureAwait(false);
                    return;
                }
                if (StoreCurrency.IsCoin(part.Type) && (long)seller.Account.GetCashShopCoins(StoreCurrency.CoinType(part.Type)) + part.Amount > int.MaxValue)
                {
                    await FailAsync(buyer, seller, ItemBuyResult.MoneyOverflowOrNotEnoughSpace).ConfigureAwait(false);
                    return;
                }
                if (part.Type == 0 || StoreCurrency.IsCoin(part.Type))
                {
                    continue;
                }

                var payment = buyer.Inventory.Items.Where(i => StoreCurrency.IsPaymentJewel(i, part.Type)).Take(part.Amount).ToArray();
                if (payment.Length != part.Amount)
                {
                    await FailAsync(buyer, seller, ItemBuyResult.LackOfMoney).ConfigureAwait(false);
                    return;
                }
                jewels.AddRange(payment);
            }

            // Jewel proceeds still go to seller inventory. Reserve all cells before taking payment.
            if (seller.Inventory.FreeSlots.Count() < jewels.Count)
            {
                await FailAsync(buyer, seller, ItemBuyResult.MoneyOverflowOrNotEnoughSpace).ConfigureAwait(false);
                return;
            }
            var slots = jewels.Select(i => i.ItemSlot).ToArray();
            var oldPrice = item.StorePricesJson;
            var oldAmount = item.StorePrice;
            var oldCurrency = item.StorePriceItemType;
            var buyerCoins = Enumerable.Range(0, 3).Select(i => buyer.Account.GetCashShopCoins((MUnique.OpenMU.DataModel.Configuration.CashShopCoinType)i)).ToArray();
            var sellerCoins = Enumerable.Range(0, 3).Select(i => seller.Account.GetCashShopCoins((MUnique.OpenMU.DataModel.Configuration.CashShopCoinType)i)).ToArray();
            var zen = price.FirstOrDefault(p => p.Type == 0)?.Amount ?? 0;
            using var transaction = buyer.GameContext.PersistenceContextProvider.CreateNewPlayerContext(buyer.GameContext.Configuration);
            transaction.Attach(buyer.Account);
            transaction.Attach(seller.Account);
            transaction.Attach(buyer.SelectedCharacter!);
            transaction.Attach(seller.SelectedCharacter!);
            var transferred = new List<Item>();
            var removed = false;
            var chargedZen = false;
            var creditedZen = false;
            var committed = false;
            try
            {
                // Create both receipts first. Failure leaves the listing and balances untouched.
                buyerMail = await StoreSaleMail.CreateAsync(transaction, buyer, seller, item, price, true).ConfigureAwait(false);
                sellerMail = await StoreSaleMail.CreateAsync(transaction, seller, buyer, item, price, false).ConfigureAwait(false);
                foreach (var jewel in jewels)
                {
                    await buyer.Inventory.RemoveItemAsync(jewel).ConfigureAwait(false);
                    transferred.Add(jewel);
                    if (!await seller.Inventory.AddItemAsync(jewel).ConfigureAwait(false))
                    {
                        throw new InvalidOperationException("Seller jewel space changed.");
                    }
                }

                if (zen > 0)
                {
                    if (!buyer.TryRemoveMoney(zen))
                    {
                        throw new InvalidOperationException("Buyer Zen changed.");
                    }

                    chargedZen = true;
                    if (!seller.TryAddMoney(zen))
                    {
                        throw new InvalidOperationException("Seller Zen overflow.");
                    }

                    creditedZen = true;
                }

                foreach (var part in price.Where(p => StoreCurrency.IsCoin(p.Type)))
                {
                    var coin = StoreCurrency.CoinType(part.Type);
                    buyer.Account.SetCashShopCoins(coin, buyer.Account.GetCashShopCoins(coin) - part.Amount);
                    seller.Account.SetCashShopCoins(coin, seller.Account.GetCashShopCoins(coin) + part.Amount);
                }

                await seller.ShopStorage.RemoveItemAsync(item).ConfigureAwait(false);
                removed = true;
                item.StorePrice = null;
                item.StorePriceItemType = 0;
                item.StorePricesJson = string.Empty;
                if (!await transaction.SaveChangesAsync().ConfigureAwait(false))
                {
                    throw new InvalidOperationException("Store sale was not saved.");
                }

                committed = true;
            }
            finally
            {
                if (!committed)
                {
                    for (var i = 0; i < 3; ++i)
                    {
                        buyer.Account.SetCashShopCoins((MUnique.OpenMU.DataModel.Configuration.CashShopCoinType)i, buyerCoins[i]);
                        seller.Account.SetCashShopCoins((MUnique.OpenMU.DataModel.Configuration.CashShopCoinType)i, sellerCoins[i]);
                    }

                    if (chargedZen)
                    {
                        buyer.TryAddMoney(zen);
                    }

                    if (creditedZen)
                    {
                        seller.TryRemoveMoney(zen);
                    }

                    for (var i = 0; i < transferred.Count; ++i)
                    {
                        await seller.Inventory.RemoveItemAsync(transferred[i]).ConfigureAwait(false);
                        await buyer.Inventory.AddItemAsync(slots[i], transferred[i]).ConfigureAwait(false);
                    }

                    if (removed)
                    {
                        item.StorePrice = oldAmount;
                        item.StorePriceItemType = oldCurrency;
                        item.StorePricesJson = oldPrice;
                        seller.ShopStorage.StoreOpen = false;
                        try
                        {
                            await seller.ShopStorage.AddItemAsync(slot, item).ConfigureAwait(false);
                        }
                        finally
                        {
                            seller.ShopStorage.StoreOpen = true;
                        }
                    }
                }
            }

            seller.PersistenceContext.Detach(item);
            foreach (var jewel in jewels)
            {
                buyer.PersistenceContext.Detach(jewel);
                seller.PersistenceContext.Attach(jewel);
            }
            await seller.InvokeViewPlugInAsync<IItemSoldByPlayerShopPlugIn>(p => p.ItemSoldByPlayerShopAsync(slot, buyer)).ConfigureAwait(false);
        }

        await StoreSaleMail.ForwardAsync(buyer, buyerMail).ConfigureAwait(false);
        await StoreSaleMail.ForwardAsync(seller, sellerMail).ConfigureAwait(false);
        await buyer.ShowLocalizedBlueMessageAsync("StoreItemDeliveredToMail").ConfigureAwait(false);
        await buyer.InvokeViewPlugInAsync<IUpdateInventoryListPlugIn>(p => p.UpdateInventoryListAsync()).ConfigureAwait(false);
        await seller.InvokeViewPlugInAsync<IUpdateInventoryListPlugIn>(p => p.UpdateInventoryListAsync()).ConfigureAwait(false);
        await buyer.InvokeViewPlugInAsync<IUpdateMoneyPlugIn>(p => p.UpdateMoneyAsync()).ConfigureAwait(false);
        await seller.InvokeViewPlugInAsync<IUpdateMoneyPlugIn>(p => p.UpdateMoneyAsync()).ConfigureAwait(false);
        if (seller.ShopStorage.Items.Any())
        {
            await buyer.InvokeViewPlugInAsync<IShowShopItemListPlugIn>(p => p.ShowShopItemListAsync(seller, true)).ConfigureAwait(false);
        }
        else
        {
            await new CloseStoreAction().CloseStoreAsync(seller).ConfigureAwait(false);
        }
    }

    private static ValueTask FailAsync(Player buyer, Player seller, ItemBuyResult result)
        => buyer.InvokeViewPlugInAsync<IPlayerShopBuyRequestResultPlugIn>(p => p.ShowResultAsync(seller, result, null));
}
