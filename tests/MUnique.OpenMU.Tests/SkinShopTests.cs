// <copyright file="SkinShopTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.Cosmetics;
using BasicModel = MUnique.OpenMU.Persistence.BasicModel;

/// <summary>Verifies skin purchases, saved bonuses and coin rollback.</summary>
[TestFixture]
public class SkinShopTests
{
    /// <summary>A replay or concurrent duplicate never spends WCoinC twice.</summary>
    [Test]
    public async Task PurchaseAndReplayAreAtomicAsync()
    {
        var player = await CreateAsync().ConfigureAwait(false);
        var actions = new SkinShopActions();
        await Task.WhenAll(actions.ExecuteAsync(player, 1, 1, 4196, 4, 100).AsTask(), actions.ExecuteAsync(player, 1, 1, 4196, 4, 100).AsTask()).ConfigureAwait(false);
        Assert.That(player.Account!.WCoinC, Is.EqualTo(400));
        var item = player.Inventory!.Items.Single();
        Assert.That(item.ItemOptions.Single().ItemOption!.Number, Is.EqualTo(4));
        Assert.That(SkinBonuses.IsValid(item), Is.True);
        await new CosmeticArmorActions().ExecuteAsync(player, 1, 1, item.ItemSlot, 1).ConfigureAwait(false);
        Assert.That(player.Attributes![Stats.ArmorDamageDecrease], Is.EqualTo(0.04f).Within(0.0001));
        _ = new InventoryStorage(player, player.GameContext);
        Assert.That(player.Attributes[Stats.ArmorDamageDecrease], Is.EqualTo(0.04f).Within(0.0001));
        await new CosmeticArmorActions().ExecuteAsync(player, 1, 2, 0, 1).ConfigureAwait(false);
        Assert.That(player.Attributes[Stats.ArmorDamageDecrease], Is.Zero);
        Assert.That(item.ItemOptions.Single().ItemOption!.Number, Is.EqualTo(4));
    }

    /// <summary>Untrusted price, bonus, class and item identifiers cannot purchase items.</summary>
    [TestCase(4196, 4, 99)]
    [TestCase(4196, 4, 0)]
    [TestCase(4196, 6, 100)]
    [TestCase(4196, 0, 100)]
    [TestCase(7777, 4, 100)]
    public async Task InvalidPurchaseDoesNotSpendAsync(int type, byte bonus, int price)
    {
        var player = await CreateAsync().ConfigureAwait(false);
        await new SkinShopActions().ExecuteAsync(player, 1, 1, type, bonus, price).ConfigureAwait(false);
        Assert.That(player.Account!.WCoinC, Is.EqualTo(500));
        Assert.That(player.Inventory!.Items, Is.Empty);
    }

    /// <summary>A failed save restores both coins and inventory.</summary>
    [Test]
    public async Task FailedSaveRollsBackAsync()
    {
        var player = await CreateAsync().ConfigureAwait(false);
        CashShopActionsTest.SaveFailingContext.Install(player, () => true);
        await new SkinShopActions().ExecuteAsync(player, 1, 1, 4196, 4, 100).ConfigureAwait(false);
        Assert.That(player.Account!.WCoinC, Is.EqualTo(500));
        Assert.That(player.Inventory!.Items, Is.Empty);
        Assert.That(player.SelectedCharacter!.Inventory!.Items, Is.Empty);
    }

    /// <summary>Full bags, insufficient funds and disabled prices leave ownership unchanged.</summary>
    [TestCase(true, 500, 100)]
    [TestCase(false, 99, 100)]
    [TestCase(false, 500, 0)]
    public async Task UnavailablePurchaseDoesNotSpendAsync(bool full, int coins, int price)
    {
        var player = await CreateAsync(price).ConfigureAwait(false);
        player.Account!.WCoinC = coins;
        if (full)
        {
            foreach (var slot in player.Inventory!.FreeSlots.ToArray())
            {
                var filler = player.PersistenceContext.CreateNew<Item>();
                filler.Definition = new BasicModel.ItemDefinition { Width = 1, Height = 1 };
                await player.Inventory.AddItemAsync(slot, filler).ConfigureAwait(false);
            }
        }

        var count = player.SelectedCharacter!.Inventory!.Items.Count;
        await new SkinShopActions().ExecuteAsync(player, 1, 1, 4196, 4, price).ConfigureAwait(false);
        Assert.That(player.Account.WCoinC, Is.EqualTo(coins));
        Assert.That(player.SelectedCharacter.Inventory.Items.Count, Is.EqualTo(count));
    }

    /// <summary>GM creation uses exactly one ordinary flag and rejects combined or unsupported flags.</summary>
    [TestCase(0, true)]
    [TestCase(8, true)]
    [TestCase(9, false)]
    [TestCase(64, false)]
    public async Task GmBonusSelectionUsesOneNormalFlagAsync(int flags, bool accepted)
    {
        var player = await CreateAsync().ConfigureAwait(false);
        var definition = player.GameContext.Configuration.Items.Single();
        Assert.That(SkinBonuses.FromFlags(definition, flags) is not null, Is.EqualTo(accepted));
    }

    private static async Task<Player> CreateAsync(int price = 100)
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        await player.PlayerState.TryAdvanceToAsync(PlayerState.EnteredWorld).ConfigureAwait(false);
        player.GameContext.FeaturePlugIns.AddPlugIn(new SkinShopFeaturePlugIn { Configuration = new SkinShopSettings { ArmorPrice = price } }, true);
        player.Account!.WCoinC = 500;
        player.SelectedCharacter!.CharacterClass = new BasicModel.CharacterClass { Number = 0 };
        var slot = new BasicModel.ItemSlotType();
        slot.ItemSlots.Add(241);
        var definition = new BasicModel.ItemDefinition { Group = 8, Number = 100, Width = 2, Height = 3, Durability = 1, ItemSlot = slot, Name = "Set 100 Skin" };
        var options = new BasicModel.ItemOptionDefinition();
        options.PossibleOptions.Add(new BasicModel.IncreasableItemOption
        {
            Number = 4,
            OptionType = ItemOptionTypes.Excellent,
            PowerUpDefinition = new BasicModel.PowerUpDefinition
            {
                TargetAttribute = Stats.ArmorDamageDecrease,
                Boost = new BasicModel.PowerUpDefinitionValue { ConstantValue = { Value = 0.04f, AggregateType = AggregateType.AddRaw } },
            },
        });
        definition.PossibleItemOptions.Add(options);
        player.GameContext.Configuration.Items.Add(definition);
        return player;
    }
}
