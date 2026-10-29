// <copyright file="StoreMailTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using System.Text.Json;
using System.Reflection;
using Moq;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.DataModel;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.CashShop;
using MUnique.OpenMU.GameLogic.PlayerActions.PlayerStore;
using MUnique.OpenMU.GameLogic.PlayerActions.Messenger;
using MUnique.OpenMU.Persistence;
using Basic = MUnique.OpenMU.Persistence.BasicModel;

/// <summary>Combined prices charge every component; purchased items remain in mail until claimed.</summary>
[TestFixture]
public class StoreMailTests
{
    private const int Bless = 0xE00D;

    /// <summary>No empty, duplicate, negative or unauthorized currency components are accepted.</summary>
    [Test]
    public void PriceValidation()
    {
        var settings = new StoreCurrencySettings();
        Assert.That(CombinedStorePrice.IsValid([new(Bless, 1), new(0xFFF0, 100)], settings), Is.True);
        Assert.That(CombinedStorePrice.IsValid([], settings), Is.False);
        Assert.That(CombinedStorePrice.IsValid([new(Bless, 1), new(Bless, 2)], settings), Is.False);
        Assert.That(CombinedStorePrice.IsValid([new(Bless, -1)], settings), Is.False);
        Assert.That(CombinedStorePrice.IsValid([new(0xFFF0, 100)], null), Is.False);
        Assert.That(CombinedStorePrice.IsValid([new(0xFFEF, 1)], settings), Is.False);
    }

    /// <summary>The payment is conjunctive; a missing component cannot partially charge the buyer.</summary>
    [TestCase(false)]
    [TestCase(true)]
    public async Task CombinedPurchaseAsync(bool enough)
    {
        var (buyer, seller, item, jewel) = await SetupAsync().ConfigureAwait(false);
        buyer.Account!.WCoinC = enough ? 150 : 99;
        await new BuyRequestAction().BuyItemAsync(buyer, seller, item.ItemSlot).ConfigureAwait(false);
        if (!enough)
        {
            Assert.That(seller.ShopStorage!.Items, Does.Contain(item));
            Assert.That(buyer.Inventory!.Items, Does.Contain(jewel));
            Assert.That(buyer.Account.WCoinC, Is.EqualTo(99));
            Assert.That(seller.Account!.WCoinC, Is.Zero);
            return;
        }
        Assert.That(buyer.Account.WCoinC, Is.EqualTo(50));
        Assert.That(seller.Account!.WCoinC, Is.EqualTo(100));
        Assert.That(buyer.Inventory!.Items, Does.Not.Contain(item).And.Not.Contain(jewel));
        Assert.That(seller.Inventory!.Items, Does.Contain(jewel));
        using var context = buyer.GameContext.PersistenceContextProvider.CreateNewPlayerContext(buyer.GameContext.Configuration);
        var letters = (await context.GetAsync<LetterBody>().ConfigureAwait(false)).ToArray();
        Assert.That(letters, Has.Length.EqualTo(2));
        var delivery = letters.Single(l => l.Attachment is not null);
        Assert.That(delivery.Attachment, Is.SameAs(item));
        Assert.That(delivery.Header!.ReceiverName, Is.EqualTo(buyer.Name));
        Assert.That(delivery.Message, Does.Contain("Jewel of Bless").And.Contain("100 WCoinC"));
        Assert.That(letters.Single(l => l.Attachment is null).Header!.ReceiverName, Is.EqualTo(seller.Name));

        Mock.Get(buyer.SelectedCharacter!).Setup(c => c.Letters).Returns(new List<MUnique.OpenMU.Interfaces.LetterHeader> { delivery.Header });
        await buyer.PlayerState.TryAdvanceToAsync(PlayerState.EnteredWorld).ConfigureAwait(false);
        var claim = new ClaimStoreMailAction();
        await claim.ClaimAsync(buyer, 0).ConfigureAwait(false);
        await claim.ClaimAsync(buyer, 0).ConfigureAwait(false);
        Assert.That(buyer.Inventory.Items.Count(i => ReferenceEquals(i, item)), Is.EqualTo(1));
        Assert.That(delivery.Attachment, Is.Null);
    }

    /// <summary>Legacy listings also go to mail when the updated client requests it.</summary>
    [Test]
    public async Task ExistingListingUsesMailAsync()
    {
        var (buyer, seller, item, _) = await SetupAsync().ConfigureAwait(false);
        item.StorePricesJson = string.Empty;
        item.StorePriceItemType = 0xFFF0;
        item.StorePrice = 100;
        buyer.Account!.WCoinC = 150;
        await new BuyRequestAction().BuyItemAsync(buyer, seller, item.ItemSlot, true).ConfigureAwait(false);
        Assert.That(buyer.Inventory!.Items, Does.Not.Contain(item));
        using var context = buyer.GameContext.PersistenceContextProvider.CreateNewPlayerContext(buyer.GameContext.Configuration);
        Assert.That((await context.GetAsync<LetterBody>().ConfigureAwait(false)).Count(l => l.Attachment == item), Is.EqualTo(1));
    }

    /// <summary>A full recipient retains the attachment; another character cannot claim it.</summary>
    [Test]
    public async Task FullInventoryAndRecipientChecksAsync()
    {
        var (buyer, seller, item, jewel) = await SetupAsync().ConfigureAwait(false);
        buyer.Account!.WCoinC = 150;
        await new BuyRequestAction().BuyItemAsync(buyer, seller, item.ItemSlot).ConfigureAwait(false);
        using var context = buyer.GameContext.PersistenceContextProvider.CreateNewPlayerContext(buyer.GameContext.Configuration);
        var delivery = (await context.GetAsync<LetterBody>().ConfigureAwait(false)).Single(l => l.Attachment is not null);
        Mock.Get(buyer.SelectedCharacter!).Setup(c => c.Letters).Returns(new List<MUnique.OpenMU.Interfaces.LetterHeader> { delivery.Header! });
        Mock.Get(seller.SelectedCharacter!).Setup(c => c.Letters).Returns(new List<MUnique.OpenMU.Interfaces.LetterHeader>());
        await buyer.PlayerState.TryAdvanceToAsync(PlayerState.EnteredWorld).ConfigureAwait(false);
        await seller.PlayerState.TryAdvanceToAsync(PlayerState.EnteredWorld).ConfigureAwait(false);
        await new ClaimStoreMailAction().ClaimAsync(seller, 0).ConfigureAwait(false);
        Assert.That(delivery.Attachment, Is.SameAs(item));
        while (buyer.Inventory!.FreeSlots.Any())
        {
            var filler = buyer.PersistenceContext.CreateNew<Item>(); filler.Definition = jewel.Definition; filler.Durability = 1;
            await buyer.Inventory.AddItemAsync(filler).ConfigureAwait(false);
        }
        await new ClaimStoreMailAction().ClaimAsync(buyer, 0).ConfigureAwait(false);
        await new LetterDeleteAction().DeleteLetterAsync(buyer, delivery.Header!).ConfigureAwait(false);
        Assert.That(delivery.Attachment, Is.SameAs(item));
        Assert.That(buyer.SelectedCharacter!.Letters, Has.Count.EqualTo(1));
    }

    /// <summary>The persisted attachment has optimistic concurrency protection and a unique item relationship.</summary>
    [Test]
    public void AttachmentPersistenceModel()
    {
        using var context = new MUnique.OpenMU.Persistence.EntityFramework.EntityDataContext();
        var type = context.Model.GetEntityTypes().Single(t => t.ClrType.BaseType == typeof(LetterBody));
        Assert.That(type.FindProperty(nameof(LetterBody.ClaimRevision))!.IsConcurrencyToken, Is.True);
        Assert.That(type.GetForeignKeys().Single(f => f.Properties.Any(p => p.Name == "AttachmentId")).IsUnique, Is.True);
    }

    /// <summary>A failed database commit restores every currency, jewel, listing and original price.</summary>
    [Test]
    public async Task FailedSaleRestoresPaymentAsync()
    {
        var (buyer, seller, item, jewel) = await SetupAsync().ConfigureAwait(false);
        buyer.Account!.WCoinC = 150;
        var original = item.StorePricesJson;
        var provider = buyer.GameContext.PersistenceContextProvider;
        var transaction = FailingContext.Wrap(provider.CreateNewPlayerContext(buyer.GameContext.Configuration));
        var replacement = new Mock<IPersistenceContextProvider>();
        replacement.Setup(p => p.CreateNewPlayerContext(buyer.GameContext.Configuration)).Returns(transaction);
        typeof(GameContext).GetField("<PersistenceContextProvider>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(buyer.GameContext, replacement.Object);
        Assert.ThrowsAsync<InvalidOperationException>(async () => await new BuyRequestAction().BuyItemAsync(buyer, seller, item.ItemSlot).ConfigureAwait(false));
        Assert.That(buyer.Account.WCoinC, Is.EqualTo(150));
        Assert.That(seller.Account!.WCoinC, Is.Zero);
        Assert.That(buyer.Inventory!.Items, Does.Contain(jewel));
        Assert.That(seller.Inventory!.Items, Does.Not.Contain(jewel));
        Assert.That(seller.ShopStorage!.Items, Does.Contain(item));
        Assert.That(item.StorePricesJson, Is.EqualTo(original));
    }

    /// <summary>A failed claim leaves the original item attached and removes its tentative inventory entry.</summary>
    [Test]
    public async Task FailedClaimPreservesAttachmentAsync()
    {
        var (buyer, seller, item, _) = await SetupAsync().ConfigureAwait(false);
        buyer.Account!.WCoinC = 150;
        await new BuyRequestAction().BuyItemAsync(buyer, seller, item.ItemSlot).ConfigureAwait(false);
        using var context = buyer.GameContext.PersistenceContextProvider.CreateNewPlayerContext(buyer.GameContext.Configuration);
        var mail = (await context.GetAsync<LetterBody>().ConfigureAwait(false)).Single(l => l.Attachment is not null);
        Mock.Get(buyer.SelectedCharacter!).Setup(c => c.Letters).Returns(new List<MUnique.OpenMU.Interfaces.LetterHeader> { mail.Header! });
        await buyer.PlayerState.TryAdvanceToAsync(PlayerState.EnteredWorld).ConfigureAwait(false);
        typeof(Player).GetField("<PersistenceContext>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(buyer, FailingContext.Wrap(buyer.PersistenceContext));
        var revision = mail.ClaimRevision;
        await new ClaimStoreMailAction().ClaimAsync(buyer, 0).ConfigureAwait(false);
        Assert.That(mail.Attachment, Is.SameAs(item));
        Assert.That(mail.ClaimRevision, Is.EqualTo(revision));
        Assert.That(buyer.Inventory!.Items, Does.Not.Contain(item));
    }

    /// <summary>Injects a rejected commit without changing storage or network behavior.</summary>
    public class FailingContext : DispatchProxy
    {
        private IPlayerContext _inner = null!;

        /// <summary>Wraps an existing context.</summary>
        public static IPlayerContext Wrap(IPlayerContext inner)
        {
            var proxy = DispatchProxy.Create<IPlayerContext, FailingContext>();
            ((FailingContext)(object)proxy)._inner = inner;
            return proxy;
        }

        /// <inheritdoc />
        protected override object? Invoke(MethodInfo? method, object?[]? args)
            => method!.Name == nameof(IContext.SaveChangesAsync) ? ValueTask.FromResult(false) : method.Invoke(this._inner, args);
    }

    private static async Task<(Player Buyer, Player Seller, Item Item, Item Jewel)> SetupAsync()
    {
        var buyer = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        var seller = await PlayerTestHelper.CreatePlayerAsync(buyer.GameContext).ConfigureAwait(false);
        buyer.Account = new Basic.Account(); seller.Account = new Basic.Account();
        buyer.SelectedCharacter!.Name = "Buyer"; seller.SelectedCharacter!.Name = "Seller";
        var definition = new Basic.ItemDefinition { Group = 14, Number = 13, Name = "Jewel of Bless", Width = 1, Height = 1, Durability = 1, IsTradable = true, IsPersonalStoreSellable = true };
        Mock.Get(buyer.GameContext.Configuration).Setup(c => c.Items).Returns(new List<ItemDefinition> { definition });
        buyer.GameContext.FeaturePlugIns.AddPlugIn(new StoreCurrencyFeaturePlugIn(), true);
        var item = seller.PersistenceContext.CreateNew<Item>();
        item.Definition = definition; item.Durability = 1;
        item.StorePrice = 1; item.StorePriceItemType = CombinedStorePrice.Marker;
        item.StorePricesJson = JsonSerializer.Serialize(new StorePricePart[] { new(Bless, 1), new(0xFFF0, 100) });
        await seller.ShopStorage!.AddItemAsync(InventoryConstants.FirstStoreItemSlotIndex, item).ConfigureAwait(false);
        seller.ShopStorage.StoreOpen = true;
        var jewel = buyer.PersistenceContext.CreateNew<Item>(); jewel.Definition = definition; jewel.Durability = 1;
        await buyer.Inventory!.AddItemAsync(jewel).ConfigureAwait(false);
        return (buyer, seller, item, jewel);
    }
}



