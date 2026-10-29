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
        await action.ExecuteAsync(player, 1, 1, originalSlot, 1).ConfigureAwait(false);
        await action.ExecuteAsync(player, 1, 1, originalSlot, 1).ConfigureAwait(false);
        Assert.That(item.ItemSlot, Is.EqualTo(CosmeticArmorRules.FirstSlot + 1));
        Assert.That(player.Inventory!.Items, Does.Not.Contain(item));
        Assert.That(player.Inventory.EquippedItems, Does.Not.Contain(item));
        Assert.That(player.SelectedCharacter!.Inventory!.Items.Count(i => i == item), Is.EqualTo(1));
        Assert.That(CosmeticArmorRules.VisibleEquipment(player).Select(i => i.ItemSlot), Is.EquivalentTo(new byte[] { 2, 3, 4, 5, 6 }));
        Assert.That(CosmeticArmorRules.VisibleEquipment(player).Single(i => i.ItemSlot == 3).Definition, Is.SameAs(item.Definition));
        await action.ExecuteAsync(player, 2, 2, 0, 1).ConfigureAwait(false);
        Assert.That(player.Inventory.Items.Single(), Is.SameAs(item));
        Assert.That(CosmeticArmorRules.Equipped(player), Is.Empty);
    }

    [Test]
    public async Task WrongSlotAndUnownedItemAreRejectedAsync()
    {
        var (player, item) = await CreateAsync().ConfigureAwait(false);
        var originalSlot = item.ItemSlot;
        var action = new CosmeticArmorActions();
        await action.ExecuteAsync(player, 1, 1, originalSlot, 0).ConfigureAwait(false);
        await action.ExecuteAsync(player, 2, 1, byte.MaxValue, 1).ConfigureAwait(false);
        Assert.That(item.ItemSlot, Is.EqualTo(originalSlot));
        Assert.That(player.Inventory!.Items.Single(), Is.SameAs(item));
    }

    [Test]
    public async Task TradeStateCannotChangeSkinOwnershipAsync()
    {
        var (player, item) = await CreateAsync().ConfigureAwait(false);
        await player.PlayerState.TryAdvanceToAsync(PlayerState.TradeRequested).ConfigureAwait(false);
        await new CosmeticArmorActions().ExecuteAsync(player, 1, 1, item.ItemSlot, 1).ConfigureAwait(false);
        Assert.That(player.Inventory!.Items.Single(), Is.SameAs(item));
    }

    [Test]
    public async Task FailedSaveRestoresOriginalInventoryAsync()
    {
        var (player, item) = await CreateAsync().ConfigureAwait(false);
        var slot = item.ItemSlot;
        CashShopActionsTest.SaveFailingContext.Install(player, () => true);
        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await new CosmeticArmorActions().ExecuteAsync(player, 1, 1, slot, 1).ConfigureAwait(false));
        Assert.That(player.Inventory!.GetItem(slot), Is.SameAs(item));
        Assert.That(CosmeticArmorRules.Equipped(player), Is.Empty);
    }

    /// <summary>A failed unequip save keeps the same item in the same cosmetic slot.</summary>
    [Test]
    public async Task FailedUnequipSaveRestoresCosmeticSlotAsync()
    {
        var (player, item) = await CreateAsync().ConfigureAwait(false);
        var actions = new CosmeticArmorActions();
        await actions.ExecuteAsync(player, 1, 1, item.ItemSlot, 1).ConfigureAwait(false);
        CashShopActionsTest.SaveFailingContext.Install(player, () => true);
        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await actions.ExecuteAsync(player, 2, 2, 0, 1).ConfigureAwait(false));
        Assert.That(CosmeticArmorRules.Equipped(player).Single(), Is.SameAs(item));
        Assert.That(player.Inventory!.Items, Does.Not.Contain(item));
        Assert.That(player.SelectedCharacter!.Inventory!.Items.Count(i => i == item), Is.EqualTo(1));
    }

    [Test]
    public async Task FullInventoryKeepsSkinEquippedAsync()
    {
        var (player, item) = await CreateAsync().ConfigureAwait(false);
        var action = new CosmeticArmorActions();
        await action.ExecuteAsync(player, 1, 1, item.ItemSlot, 1).ConfigureAwait(false);
        var ordinary = new BasicModel.ItemDefinition { Width = 1, Height = 1, Name = "Bag item" };
        foreach (var slot in player.Inventory!.FreeSlots.ToArray())
        {
            var filler = player.PersistenceContext.CreateNew<Item>();
            filler.Definition = ordinary;
            Assert.That(await player.Inventory.AddItemAsync(slot, filler).ConfigureAwait(false), Is.True);
        }

        await action.ExecuteAsync(player, 2, 2, 0, 1).ConfigureAwait(false);
        Assert.That(CosmeticArmorRules.Equipped(player).Single(), Is.SameAs(item));
        Assert.That(player.SelectedCharacter!.Inventory!.Items.Count(i => i == item), Is.EqualTo(1));
    }

    [Test]
    public async Task CosmeticSlotsSurviveInventoryReconstructionAsync()
    {
        var (player, item) = await CreateAsync().ConfigureAwait(false);
        await new CosmeticArmorActions().ExecuteAsync(player, 1, 1, item.ItemSlot, 1).ConfigureAwait(false);
        var reloaded = new InventoryStorage(player, player.GameContext);
        Assert.That(reloaded.Items, Does.Not.Contain(item));
        Assert.That(player.SelectedCharacter!.Inventory!.Items, Does.Contain(item));
        Assert.That(CosmeticArmorRules.Equipped(player).Single(), Is.SameAs(item));
    }

    [TestCase(0, 100, 5)]
    [TestCase(6, 100, 6)]
    [TestCase(12, 210, 7)]
    [TestCase(13, 200, 8)]
    public async Task ExtraSkinSlotsPersistAndUnequipAsync(byte group, short number, byte part)
    {
        var (player, item) = await CreateAsync().ConfigureAwait(false);
        item.Definition!.Group = group;
        item.Definition.Number = number;
        item.Definition.ItemSlot!.ItemSlots.Clear();
        item.Definition.ItemSlot.ItemSlots.Add(CosmeticArmorRules.FirstSlot + part);
        var actions = new CosmeticArmorActions();
        await actions.ExecuteAsync(player, 1, 1, item.ItemSlot, part).ConfigureAwait(false);
        Assert.That(item.ItemSlot, Is.EqualTo(CosmeticArmorRules.FirstSlot + part));
        Assert.That(CosmeticArmorRules.VisibleEquipment(player).Single().ItemSlot, Is.EqualTo(CosmeticArmorRules.GearSlot(part)));
        await actions.ExecuteAsync(player, 2, 2, 0, part).ConfigureAwait(false);
        Assert.That(CosmeticArmorRules.Equipped(player), Is.Empty);
        Assert.That(player.Inventory!.Items, Does.Contain(item));
    }

    [Test]
    public async Task IncompatibleEquippedSkinRemainsRemovableAsync()
    {
        var (player, item) = await CreateAsync().ConfigureAwait(false);
        item.Definition!.Number = 315;
        player.SelectedCharacter!.CharacterClass = new BasicModel.CharacterClass { Number = 8 };
        var actions = new CosmeticArmorActions();
        await actions.ExecuteAsync(player, 1, 1, item.ItemSlot, 1).ConfigureAwait(false);
        player.SelectedCharacter.CharacterClass = new BasicModel.CharacterClass { Number = 4 };
        Assert.That(CosmeticArmorRules.Equipped(player), Does.Contain(item));
        Assert.That(CosmeticArmorRules.VisibleEquipment(player), Is.Empty);
        await actions.ExecuteAsync(player, 2, 2, 0, 1).ConfigureAwait(false);
        Assert.That(player.Inventory!.Items, Does.Contain(item));
    }

    [TestCase(0, false)]
    [TestCase(4, false)]
    [TestCase(12, false)]
    [TestCase(16, false)]
    [TestCase(24, false)]
    [TestCase(8, true)]
    [TestCase(11, true)]
    [TestCase(20, true)]
    [TestCase(23, true)]
    public async Task FemaleArmorRequiresElfOrSummonerAsync(byte classNumber, bool allowed)
    {
        var (player, item) = await CreateAsync().ConfigureAwait(false);
        item.Definition!.Number = 315;
        player.SelectedCharacter!.CharacterClass = new BasicModel.CharacterClass { Number = classNumber };
        await new CosmeticArmorActions().ExecuteAsync(player, 1, 1, item.ItemSlot, 1).ConfigureAwait(false);
        Assert.That(CosmeticArmorRules.Equipped(player).Any(), Is.EqualTo(allowed));
        Assert.That(player.Inventory!.Items.Contains(item), Is.EqualTo(!allowed));
    }

    [TestCase(0, 2)]
    [TestCase(4, 6)]
    [TestCase(5, 0)]
    [TestCase(6, 1)]
    [TestCase(7, 7)]
    [TestCase(8, 8)]
    public void CosmeticPartsMapToGearSlots(int part, byte expected)
        => Assert.That(CosmeticArmorRules.GearSlot(part), Is.EqualTo(expected));

    [TestCase(5)]
    [TestCase(6)]
    public async Task WeaponSkinFitsEitherHandAndSurvivesReloadAsync(byte part)
    {
        var (player, item) = await CreateAsync().ConfigureAwait(false);
        item.Definition!.Group = 0;
        item.Definition.ItemSlot!.ItemSlots.Clear();
        item.Definition.ItemSlot.ItemSlots.Add(245);
        var actions = new CosmeticArmorActions();
        await actions.ExecuteAsync(player, 1, 1, item.ItemSlot, part).ConfigureAwait(false);
        Assert.That(item.ItemSlot, Is.EqualTo(CosmeticArmorRules.FirstSlot + part));
        Assert.That(CosmeticArmorRules.Equipped(player).Single(), Is.SameAs(item));
        var visible = CosmeticArmorRules.VisibleEquipment(player).Single();
        Assert.That(visible.ItemSlot, Is.EqualTo(CosmeticArmorRules.GearSlot(part)));
        Assert.That(visible.Definition, Is.SameAs(item.Definition));
        _ = new InventoryStorage(player, player.GameContext);
        Assert.That(CosmeticArmorRules.Equipped(player).Single(), Is.SameAs(item));
        await actions.ExecuteAsync(player, 2, 2, 0, part).ConfigureAwait(false);
        Assert.That(CosmeticArmorRules.Equipped(player), Is.Empty);
        Assert.That(player.Inventory!.Items, Does.Contain(item));
    }

    [TestCase(245, 246, true)]
    [TestCase(246, 245, false)]
    [TestCase(248, 246, false)]
    [TestCase(241, 246, false)]
    [TestCase(0, 246, false)]
    [TestCase(245, 12, false)]
    public void OnlyWeaponSkinsGainAnAlternativeSlot(int configured, byte destination, bool accepted)
    {
        var slots = new BasicModel.ItemSlotType();
        slots.ItemSlots.Add(configured);
        var definition = new BasicModel.ItemDefinition { ItemSlot = slots };
        Assert.That(CosmeticArmorRules.FitsSlot(definition, destination), Is.EqualTo(accepted));
        Assert.That(CosmeticArmorRules.FitsSlot(null, destination), Is.False);
    }

    private static async Task<(Player Player, Item Item)> CreateAsync()
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        await player.PlayerState.TryAdvanceToAsync(PlayerState.EnteredWorld).ConfigureAwait(false);
        var slot = new BasicModel.ItemSlotType();
        slot.ItemSlots.Add(CosmeticArmorRules.FirstSlot + 1);
        var definition = new BasicModel.ItemDefinition { Group = 8, Number = 100, Width = 2, Height = 3, ItemSlot = slot, Name = "Set 100 Helm Skin" };
        player.GameContext.Configuration.Items.Add(definition);
        foreach (var group in new byte[] { 7, 9, 10, 11 })
        {
            player.GameContext.Configuration.Items.Add(new BasicModel.ItemDefinition { Group = group, Number = 100 });
        }

        var item = player.PersistenceContext.CreateNew<Item>();
        item.Definition = definition;
        item.Durability = 1;
        Assert.That(await player.Inventory!.AddItemAsync(item).ConfigureAwait(false), Is.True);
        return (player, item);
    }
}
