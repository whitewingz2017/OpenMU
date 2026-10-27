// <copyright file="StoreCurrencyTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using System.Reflection;
using Moq;
using MUnique.OpenMU.DataModel;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.CashShop;
using MUnique.OpenMU.GameLogic.PlayerActions.PlayerStore;
using MUnique.OpenMU.Persistence;
using BasicModel = MUnique.OpenMU.Persistence.BasicModel;

/// <summary>Checks payment conservation, rejection and rollback in personal stores.</summary>
[TestFixture]
public class StoreCurrencyTests
{
    private const int Bless = (14 << 12) | 13;

    /// <summary>Inventory jewels pay exactly once; the bank balance remains untouched.</summary>
    [Test]
    public async Task InventoryJewelsTransferOnceAsync()
    {
        var (buyer, seller, item) = await SetupAsync(Bless, 2).ConfigureAwait(false);
        buyer.Account!.JewelBankBalances = "{\"7181:0\":50}";
        var first = await AddJewelAsync(buyer).ConfigureAwait(false);
        var second = await AddJewelAsync(buyer).ConfigureAwait(false);
        var action = new BuyRequestAction();
        await action.BuyItemAsync(buyer, seller, item.ItemSlot).ConfigureAwait(false);
        await action.BuyItemAsync(buyer, seller, InventoryConstants.FirstStoreItemSlotIndex).ConfigureAwait(false);
        Assert.That(buyer.Inventory!.Items, Does.Contain(item));
        Assert.That(seller.Inventory!.Items, Is.EquivalentTo(new[] { first, second }));
        Assert.That(buyer.Inventory.Items, Does.Not.Contain(first));
        Assert.That(item.StorePrice, Is.Null);
        Assert.That(item.StorePriceItemType, Is.Zero);
        Assert.That(buyer.Account.JewelBankBalances, Is.EqualTo("{\"7181:0\":50}"));
    }

    /// <summary>Bank jewels and upgraded jewels cannot substitute for inventory payment.</summary>
    [Test]
    public async Task IneligibleJewelsCannotPayAsync()
    {
        var (buyer, seller, item) = await SetupAsync(Bless, 2).ConfigureAwait(false);
        buyer.Account!.JewelBankBalances = "{\"7181:0\":100}";
        await AddJewelAsync(buyer).ConfigureAwait(false);
        var upgraded = await AddJewelAsync(buyer).ConfigureAwait(false);
        upgraded.Level = 1;
        await new BuyRequestAction().BuyItemAsync(buyer, seller, item.ItemSlot).ConfigureAwait(false);
        Assert.That(seller.ShopStorage!.Items, Does.Contain(item));
        Assert.That(buyer.Inventory!.Items.Count(), Is.EqualTo(2));
    }

    /// <summary>All three coin balances transfer without charging Zen.</summary>
    [TestCase(0xFFF0)]
    [TestCase(0xFFF1)]
    [TestCase(0xFFF2)]
    public async Task CoinsTransferAsync(int currency)
    {
        var (buyer, seller, item) = await SetupAsync(currency, 100).ConfigureAwait(false);
        var coin = StoreCurrency.CoinType(currency);
        buyer.Account!.SetCashShopCoins(coin, 120);
        seller.Account!.SetCashShopCoins(coin, 7);
        await new BuyRequestAction().BuyItemAsync(buyer, seller, item.ItemSlot).ConfigureAwait(false);
        Assert.That(buyer.Account!.GetCashShopCoins(coin), Is.EqualTo(20));
        Assert.That(seller.Account!.GetCashShopCoins(coin), Is.EqualTo(107));
        Assert.That(buyer.Inventory!.Items, Does.Contain(item));
        Assert.That(buyer.Money, Is.Zero);
    }

    /// <summary>A full seller inventory rolls back previously transferred jewels.</summary>
    [Test]
    public async Task FullSellerRollsBackAsync()
    {
        var (buyer, seller, item) = await SetupAsync(Bless, 2).ConfigureAwait(false);
        var first = await AddJewelAsync(buyer).ConfigureAwait(false);
        var second = await AddJewelAsync(buyer).ConfigureAwait(false);
        while (seller.Inventory!.FreeSlots.Count() > 1)
        {
            await AddJewelAsync(seller).ConfigureAwait(false);
        }

        var count = seller.Inventory.Items.Count();
        await new BuyRequestAction().BuyItemAsync(buyer, seller, item.ItemSlot).ConfigureAwait(false);
        Assert.That(buyer.Inventory!.Items, Is.EquivalentTo(new[] { first, second }));
        Assert.That(seller.Inventory.Items.Count(), Is.EqualTo(count));
        Assert.That(seller.ShopStorage!.Items, Does.Contain(item));
    }

    /// <summary>A database failure restores items and coins before releasing the player locks.</summary>
    [TestCase(Bless)]
    [TestCase(0xFFF0)]
    public async Task FailedSaveRollsBackAsync(int currency)
    {
        var (buyer, seller, item) = await SetupAsync(currency, 2).ConfigureAwait(false);
        var jewel = await AddJewelAsync(buyer).ConfigureAwait(false);
        await AddJewelAsync(buyer).ConfigureAwait(false);
        buyer.Account!.SetCashShopCoins(CashShopCoinType.WCoinC, 10);
        var provider = new Mock<IPersistenceContextProvider>();
        var transaction = new Mock<IPlayerContext>();
        transaction.Setup(c => c.SaveChangesAsync(It.IsAny<System.Threading.CancellationToken>())).Throws(new InvalidOperationException("Save failed"));
        provider.Setup(p => p.CreateNewPlayerContext(buyer.GameContext.Configuration)).Returns(transaction.Object);
        typeof(GameContext).GetField("<PersistenceContextProvider>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(buyer.GameContext, provider.Object);
        Assert.ThrowsAsync<InvalidOperationException>(async () => await new BuyRequestAction().BuyItemAsync(buyer, seller, item.ItemSlot).ConfigureAwait(false));
        Assert.That(seller.ShopStorage!.Items, Does.Contain(item));
        Assert.That(item.StorePrice, Is.EqualTo(2));
        Assert.That(item.StorePriceItemType, Is.EqualTo(currency));
        Assert.That(buyer.Inventory!.Items.Count(), Is.EqualTo(2));
        Assert.That(buyer.Inventory.Items, Does.Contain(jewel));
        Assert.That(seller.Inventory!.Items, Is.Empty);
        Assert.That(buyer.Account!.GetCashShopCoins(CashShopCoinType.WCoinC), Is.EqualTo(10));
        Assert.That(seller.Account!.GetCashShopCoins(CashShopCoinType.WCoinC), Is.Zero);
    }

    /// <summary>Protocol and operator limits reject unsupported or oversized payments.</summary>
    [Test]
    public void InvalidPricesAreRejected()
    {
        var settings = new StoreCurrencySettings();
        Assert.That(StoreCurrency.IsValid(Bless, 65536, settings), Is.False);
        Assert.That(StoreCurrency.IsValid(Bless, 0, settings), Is.False);
        Assert.That(StoreCurrency.IsValid(0x1234, 1, settings), Is.False);
        Assert.That(StoreCurrency.IsValid(Bless, 1, null), Is.False);
        Assert.That(StoreCurrency.IsValid(0xFFF0, int.MaxValue, settings), Is.True);
    }

    /// <summary>Only extended clients receive the configured wire currency types.</summary>
    [TestCase(false)]
    [TestCase(true)]
    public async Task AdvertisesCurrenciesOnlyToExtendedClientsAsync(bool extended)
    {
        var (player, output) = CastleSiegeRemoteViewTestHelper.CreatePlayer();
        player.ClientVersion = new MUnique.OpenMU.Network.PlugIns.ClientVersion(extended ? (byte)106 : (byte)6, 3, MUnique.OpenMU.Network.PlugIns.ClientLanguage.Invariant);
        player.GameContext.FeaturePlugIns.AddPlugIn(new StoreCurrencyFeaturePlugIn(), true);
        await MUnique.OpenMU.GameServer.RemoteView.PlayerShop.StorePriceTypes.SendAsync(player).ConfigureAwait(false);
        var data = output.ToArray();
        if (!extended)
        {
            Assert.That(data, Is.Empty);
            return;
        }

        MUnique.OpenMU.Network.Packets.ServerToClient.PlayerShopPriceTypesExtendedRef packet = data.AsSpan();
        Assert.That(data[..4], Is.EqualTo(new byte[] { 0xC1, 25, 0x3F, 0x0A }));
        Assert.That(packet.Count, Is.EqualTo(10));
        Assert.That(Enumerable.Range(0, packet.Count).Select(i => BitConverter.ToUInt16(data, 5 + (2 * i))),
            Does.Contain((ushort)Bless).And.Contain((ushort)0xFFF0).And.Contain((ushort)0xFFF1).And.Contain((ushort)0xFFF2));
    }

    /// <summary>Insufficient funds, seller overflow and a full buyer cannot charge coins.</summary>
    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    public async Task RejectedCoinPaymentDoesNotChangeBalancesAsync(int rejection)
    {
        var (buyer, seller, item) = await SetupAsync(0xFFF0, 100).ConfigureAwait(false);
        buyer.Account!.WCoinC = rejection == 0 ? 99 : 120;
        seller.Account!.WCoinC = rejection == 1 ? int.MaxValue : 7;
        if (rejection == 2)
        {
            while (buyer.Inventory!.FreeSlots.Any())
            {
                await AddJewelAsync(buyer).ConfigureAwait(false);
            }
        }

        var before = buyer.Account.WCoinC;
        var sellerBefore = seller.Account.WCoinC;
        await new BuyRequestAction().BuyItemAsync(buyer, seller, item.ItemSlot).ConfigureAwait(false);
        Assert.That(seller.ShopStorage!.Items, Does.Contain(item));
        Assert.That(buyer.Account.WCoinC, Is.EqualTo(before));
        Assert.That(seller.Account.WCoinC, Is.EqualTo(sellerBefore));
    }

    private static async Task<(Player Buyer, Player Seller, Item Item)> SetupAsync(int currency, int amount)
    {
        var buyer = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        var seller = await PlayerTestHelper.CreatePlayerAsync(buyer.GameContext).ConfigureAwait(false);
        buyer.Account = new BasicModel.Account();
        seller.Account = new BasicModel.Account();
        buyer.GameContext.FeaturePlugIns.AddPlugIn(new StoreCurrencyFeaturePlugIn(), true);
        var item = seller.PersistenceContext.CreateNew<Item>();
        item.Definition = new BasicModel.ItemDefinition { Width = 1, Height = 1, IsPersonalStoreSellable = true };
        item.StorePrice = amount;
        item.StorePriceItemType = currency;
        await seller.ShopStorage!.AddItemAsync(InventoryConstants.FirstStoreItemSlotIndex, item).ConfigureAwait(false);
        seller.ShopStorage.StoreOpen = true;
        return (buyer, seller, item);
    }

    private static async Task<Item> AddJewelAsync(Player player)
    {
        var item = player.PersistenceContext.CreateNew<Item>();
        item.Definition = new BasicModel.ItemDefinition { Group = 14, Number = 13, Width = 1, Height = 1, Durability = 1, IsTradable = true };
        item.Durability = 1;
        Assert.That(await player.Inventory!.AddItemAsync(item).ConfigureAwait(false), Is.True);
        return item;
    }
}
