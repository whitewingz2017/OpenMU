// <copyright file="CosmeticArmorTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.Cosmetics;
using BasicModel = MUnique.OpenMU.Persistence.BasicModel;

/// <summary>Checks ownership, persistence rollback and separation of skin and combat equipment.</summary>
[TestFixture]
public class CosmeticArmorTests
{
    [Test]
    public async Task EquipUnequipAndReplayConserveItemAsync()
    {
        var (player, item) = await CreateAsync().ConfigureAwait(false);
        var originalSlot = item.ItemSlot;
        var action = new CosmeticArmorActions();
        await action.ExecuteAsync(player, 1, 1, originalSlot, 0).ConfigureAwait(false);
        await action.ExecuteAsync(player, 1, 1, originalSlot, 0).ConfigureAwait(false);
        Assert.That(item.ItemSlot, Is.EqualTo(CosmeticArmorRules.FirstSlot));
        Assert.That(player.Inventory!.Items, Does.Not.Contain(item));
        Assert.That(player.Inventory.EquippedItems, Does.Not.Contain(item));
        Assert.That(player.SelectedCharacter!.Inventory!.Items.Count(i => i == item), Is.EqualTo(1));
        Assert.That(CosmeticArmorRules.VisibleEquipment(player).Single().ItemSlot, Is.EqualTo(2));
        Assert.That(CosmeticArmorRules.VisibleEquipment(player).Single().Definition, Is.SameAs(item.Definition));
        await action.ExecuteAsync(player, 2, 2, 0, 0).ConfigureAwait(false);
        Assert.That(player.Inventory.Items.Single(), Is.SameAs(item));
        Assert.That(CosmeticArmorRules.Equipped(player), Is.Empty);
    }

    [Test]
    public async Task WrongSlotAndUnownedItemAreRejectedAsync()
    {
        var (player, item) = await CreateAsync().ConfigureAwait(false);
        var originalSlot = item.ItemSlot;
        var action = new CosmeticArmorActions();
        await action.ExecuteAsync(player, 1, 1, originalSlot, 1).ConfigureAwait(false);
        await action.ExecuteAsync(player, 2, 1, byte.MaxValue, 0).ConfigureAwait(false);
        Assert.That(item.ItemSlot, Is.EqualTo(originalSlot));
        Assert.That(player.Inventory!.Items.Single(), Is.SameAs(item));
    }

    [Test]
    public async Task TradeStateCannotChangeSkinOwnershipAsync()
    {
        var (player, item) = await CreateAsync().ConfigureAwait(false);
        await player.PlayerState.TryAdvanceToAsync(PlayerState.TradeRequested).ConfigureAwait(false);
        await new CosmeticArmorActions().ExecuteAsync(player, 1, 1, item.ItemSlot, 0).ConfigureAwait(false);
        Assert.That(player.Inventory!.Items.Single(), Is.SameAs(item));
    }

    [Test]
    public async Task FailedSaveRestoresOriginalInventoryAsync()
    {
        var (player, item) = await CreateAsync().ConfigureAwait(false);
        var slot = item.ItemSlot;
        CashShopActionsTest.SaveFailingContext.Install(player, () => true);
        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await new CosmeticArmorActions().ExecuteAsync(player, 1, 1, slot, 0).ConfigureAwait(false));
        Assert.That(player.Inventory!.GetItem(slot), Is.SameAs(item));
        Assert.That(CosmeticArmorRules.Equipped(player), Is.Empty);
    }

    /// <summary>A failed unequip save keeps the same item in the same cosmetic slot.</summary>
    [Test]
    public async Task FailedUnequipSaveRestoresCosmeticSlotAsync()
    {
        var (player, item) = await CreateAsync().ConfigureAwait(false);
        var actions = new CosmeticArmorActions();
        await actions.ExecuteAsync(player, 1, 1, item.ItemSlot, 0).ConfigureAwait(false);
        CashShopActionsTest.SaveFailingContext.Install(player, () => true);
        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await actions.ExecuteAsync(player, 2, 2, 0, 0).ConfigureAwait(false));
        Assert.That(CosmeticArmorRules.Equipped(player).Single(), Is.SameAs(item));
        Assert.That(player.Inventory!.Items, Does.Not.Contain(item));
        Assert.That(player.SelectedCharacter!.Inventory!.Items.Count(i => i == item), Is.EqualTo(1));
    }

    [Test]
    public async Task FullInventoryKeepsSkinEquippedAsync()
    {
        var (player, item) = await CreateAsync().ConfigureAwait(false);
        var action = new CosmeticArmorActions();
        await action.ExecuteAsync(player, 1, 1, item.ItemSlot, 0).ConfigureAwait(false);
        var ordinary = new BasicModel.ItemDefinition { Width = 1, Height = 1, Name = "Bag item" };
        foreach (var slot in player.Inventory!.FreeSlots.ToArray())
        {
            var filler = player.PersistenceContext.CreateNew<Item>();
            filler.Definition = ordinary;
            Assert.That(await player.Inventory.AddItemAsync(slot, filler).ConfigureAwait(false), Is.True);
        }

        await action.ExecuteAsync(player, 2, 2, 0, 0).ConfigureAwait(false);
        Assert.That(CosmeticArmorRules.Equipped(player).Single(), Is.SameAs(item));
        Assert.That(player.SelectedCharacter!.Inventory!.Items.Count(i => i == item), Is.EqualTo(1));
    }

    [Test]
    public async Task CosmeticSlotsSurviveInventoryReconstructionAsync()
    {
        var (player, item) = await CreateAsync().ConfigureAwait(false);
        await new CosmeticArmorActions().ExecuteAsync(player, 1, 1, item.ItemSlot, 0).ConfigureAwait(false);
        var reloaded = new InventoryStorage(player, player.GameContext);
        Assert.That(reloaded.Items, Does.Not.Contain(item));
        Assert.That(player.SelectedCharacter!.Inventory!.Items, Does.Contain(item));
        Assert.That(CosmeticArmorRules.Equipped(player).Single(), Is.SameAs(item));
    }

    private static async Task<(Player Player, Item Item)> CreateAsync()
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        await player.PlayerState.TryAdvanceToAsync(PlayerState.EnteredWorld).ConfigureAwait(false);
        var slot = new BasicModel.ItemSlotType();
        slot.ItemSlots.Add(CosmeticArmorRules.FirstSlot);
        var definition = new BasicModel.ItemDefinition { Group = 7, Number = 315, Width = 2, Height = 2, ItemSlot = slot, Name = "Set 315 Skin Helm" };
        var item = player.PersistenceContext.CreateNew<Item>();
        item.Definition = definition;
        item.Durability = 1;
        Assert.That(await player.Inventory!.AddItemAsync(item).ConfigureAwait(false), Is.True);
        return (player, item);
    }
}
