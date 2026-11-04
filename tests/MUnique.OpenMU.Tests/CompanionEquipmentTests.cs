// <copyright file="CompanionEquipmentTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.Cosmetics;
using MUnique.OpenMU.GameLogic.Pets;
using MUnique.OpenMU.GameLogic.PlayerActions.Items;
using Basic = MUnique.OpenMU.Persistence.BasicModel;

/// <summary>Checks independent mount and ordinary-pet equipment, bonuses and ownership.</summary>
[TestFixture]
internal class CompanionEquipmentTests
{
    [TestCase(0)]
    [TestCase(1)]
    [TestCase(64)]
    [TestCase(65)]
    [TestCase(67)]
    [TestCase(80)]
    [TestCase(106)]
    [TestCase(123)]
    public async Task OrdinaryPetKeepsBonusesAlongsideMountAsync(short number)
    {
        var (player, pet) = await CreateAsync(number).ConfigureAwait(false);
        var mount = await AddAsync(player, 37).ConfigureAwait(false);
        var moves = new MoveItemAction();
        await moves.MoveItemAsync(player, mount.ItemSlot, Storages.Inventory, (byte)InventoryConstants.PetSlot, Storages.Inventory).ConfigureAwait(false);
        Assert.That(mount.ItemSlot, Is.EqualTo(InventoryConstants.PetSlot));

        var before = player.Attributes![Stats.MaximumHealth];
        pet.Definition!.BasePowerUpAttributes.Add(new Basic.ItemBasePowerUpDefinition
        {
            TargetAttribute = Stats.MaximumHealth,
            BaseValue = 40,
        });
        pet.ItemOptions.Add(new Basic.ItemOptionLink
        {
            ItemOption = new Basic.IncreasableItemOption
            {
                OptionType = ItemOptionTypes.Excellent,
                PowerUpDefinition = new Basic.PowerUpDefinition
                {
                    TargetAttribute = Stats.MaximumHealth,
                    Boost = new Basic.PowerUpDefinitionValue { ConstantValue = { Value = 10, AggregateType = AggregateType.AddRaw } },
                },
            },
            Level = 1,
        });
        var actions = new CosmeticArmorActions();
        await actions.ExecuteAsync(player, 1, 1, pet.ItemSlot, 8).ConfigureAwait(false);
        Assert.That(pet.ItemSlot, Is.EqualTo(CompanionEquipmentRules.Slot));
        Assert.That(player.Inventory!.EquippedItems, Is.EquivalentTo(new[] { mount, pet }));
        Assert.That(GameServer.RemoteView.Inventory.UpdateInventoryListPlugIn.GetInventoryItems(player), Is.EquivalentTo(new[] { mount, pet }));
        Assert.That(CosmeticArmorRules.VisibleEquipment(player).Select(i => i.ItemSlot), Is.EquivalentTo(new[] { (byte)InventoryConstants.PetSlot, CompanionEquipmentRules.Slot }));
        Assert.That(player.Attributes[Stats.MaximumHealth], Is.EqualTo(before + 50).Within(0.001));
        Assert.That(pet.DegradesWithDurability(), Is.False);
        var reloaded = new InventoryStorage(player, player.GameContext);
        Assert.That(reloaded.EquippedItems, Is.EquivalentTo(new[] { mount, pet }));
        Assert.That(player.Attributes[Stats.MaximumHealth], Is.EqualTo(before + 50).Within(0.001));
        await player.DecreaseItemDurabilityAsync(pet, 99).ConfigureAwait(false);
        Assert.That(player.Attributes[Stats.MaximumHealth], Is.EqualTo(before + 50).Within(0.001));
        await player.DecreaseItemDurabilityAsync(pet, 1).ConfigureAwait(false);
        Assert.That(player.Attributes[Stats.MaximumHealth], Is.EqualTo(before).Within(0.001));
        Assert.That(CosmeticArmorRules.VisibleEquipment(player).Any(i => i.ItemSlot == CompanionEquipmentRules.Slot), Is.False);
        await actions.ExecuteAsync(player, 2, 2, 0, 8).ConfigureAwait(false);
        Assert.That(player.Inventory.Items, Does.Contain(pet));
        Assert.That(player.Inventory.GetItem((byte)InventoryConstants.PetSlot), Is.SameAs(mount));
    }

    [TestCase(80)]
    [TestCase(65)]
    public async Task ExtraPetSupplementsOccupiedOriginalSlotAsync(short extraNumber)
    {
        var (player, original) = await CreateAsync(80).ConfigureAwait(false);
        var extra = await AddAsync(player, extraNumber).ConfigureAwait(false);
        if (extraNumber == 80) extra.Definition = original.Definition;
        foreach (var definition in new[] { original.Definition!, extra.Definition! }.Distinct())
        {
            definition.BasePowerUpAttributes.Add(new Basic.ItemBasePowerUpDefinition { TargetAttribute = Stats.MaximumHealth, BaseValue = 40 });
        }

        var before = player.Attributes![Stats.MaximumHealth];
        var moves = new MoveItemAction();
        await moves.MoveItemAsync(player, original.ItemSlot, Storages.Inventory, (byte)InventoryConstants.PetSlot, Storages.Inventory).ConfigureAwait(false);
        Assert.That(original.ItemSlot, Is.EqualTo(InventoryConstants.PetSlot));
        var actions = new CosmeticArmorActions();
        await actions.ExecuteAsync(player, 1, 1, extra.ItemSlot, 8).ConfigureAwait(false);
        Assert.That(extra.ItemSlot, Is.EqualTo(CompanionEquipmentRules.Slot));
        Assert.That(player.Inventory!.EquippedItems, Is.EquivalentTo(new[] { original, extra }));
        Assert.That(player.Attributes[Stats.MaximumHealth], Is.EqualTo(before + 80));
        _ = new InventoryStorage(player, player.GameContext);
        Assert.That(player.Attributes[Stats.MaximumHealth], Is.EqualTo(before + 80));

        var spare = await AddAsync(player, 1).ConfigureAwait(false);
        var spareSlot = spare.ItemSlot;
        await actions.ExecuteAsync(player, 2, 1, spareSlot, 8).ConfigureAwait(false);
        Assert.That(spare.ItemSlot, Is.EqualTo(spareSlot));
        Assert.That(extra.ItemSlot, Is.EqualTo(CompanionEquipmentRules.Slot));
        await actions.ExecuteAsync(player, 3, 2, 0, 8).ConfigureAwait(false);
        Assert.That(player.Inventory.GetItem((byte)InventoryConstants.PetSlot), Is.SameAs(original));
        Assert.That(player.Attributes[Stats.MaximumHealth], Is.EqualTo(before + 40));
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task OriginalPetCanMoveToExtraSlotAsync(bool failSave)
    {
        var (player, pet) = await CreateAsync(80).ConfigureAwait(false);
        await new MoveItemAction().MoveItemAsync(player, pet.ItemSlot, Storages.Inventory, (byte)InventoryConstants.PetSlot, Storages.Inventory).ConfigureAwait(false);
        var actions = new CosmeticArmorActions();
        if (failSave)
        {
            CashShopActionsTest.SaveFailingContext.Install(player, () => true);
            Assert.ThrowsAsync<InvalidOperationException>(async () => await actions.ExecuteAsync(player, 1, 1, (byte)InventoryConstants.PetSlot, 8).ConfigureAwait(false));
            Assert.That(pet.ItemSlot, Is.EqualTo(InventoryConstants.PetSlot));
        }
        else
        {
            await actions.ExecuteAsync(player, 1, 1, (byte)InventoryConstants.PetSlot, 8).ConfigureAwait(false);
            Assert.That(pet.ItemSlot, Is.EqualTo(CompanionEquipmentRules.Slot));
            Assert.That(player.Inventory!.GetItem((byte)InventoryConstants.PetSlot), Is.Null);
        }

        Assert.That(player.SelectedCharacter!.Inventory!.Items.Count(i => i == pet), Is.EqualTo(1));
    }

    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(37)]
    [TestCase(300)]
    [TestCase(312)]
    [TestCase(309)]
    [TestCase(369)]
    public async Task SmallPetSlotRejectsMountsAsync(short number)
    {
        var (player, mount) = await CreateAsync(number).ConfigureAwait(false);
        var original = mount.ItemSlot;
        await new CosmeticArmorActions().ExecuteAsync(player, 1, 1, original, 8).ConfigureAwait(false);
        Assert.That(mount.ItemSlot, Is.EqualTo(original));
        Assert.That(player.Inventory!.Items.Single(), Is.SameAs(mount));
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task FailedSaveKeepsSamePetAsync(bool unequip)
    {
        var (player, pet) = await CreateAsync(80).ConfigureAwait(false);
        var actions = new CosmeticArmorActions();
        if (unequip)
        {
            await actions.ExecuteAsync(player, 1, 1, pet.ItemSlot, 8).ConfigureAwait(false);
        }

        var original = pet.ItemSlot;
        CashShopActionsTest.SaveFailingContext.Install(player, () => true);
        Assert.ThrowsAsync<InvalidOperationException>(async () => await actions.ExecuteAsync(player, 2, unequip ? (byte)2 : (byte)1, unequip ? (byte)0 : original, 8).ConfigureAwait(false));
        Assert.That(pet.ItemSlot, Is.EqualTo(original));
        Assert.That(player.SelectedCharacter!.Inventory!.Items.Count(i => i == pet), Is.EqualTo(1));
        Assert.That(player.Inventory!.Items.Contains(pet), Is.EqualTo(!unequip));
    }

    [Test]
    public async Task FullBagKeepsPetEquippedAsync()
    {
        var (player, pet) = await CreateAsync(80).ConfigureAwait(false);
        var actions = new CosmeticArmorActions();
        await actions.ExecuteAsync(player, 1, 1, pet.ItemSlot, 8).ConfigureAwait(false);
        foreach (var slot in player.Inventory!.FreeSlots.ToArray())
        {
            var filler = player.PersistenceContext.CreateNew<Item>();
            filler.Definition = new Basic.ItemDefinition { Width = 1, Height = 1 };
            Assert.That(await player.Inventory.AddItemAsync(slot, filler).ConfigureAwait(false), Is.True);
        }

        await actions.ExecuteAsync(player, 2, 2, 0, 8).ConfigureAwait(false);
        Assert.That(pet.ItemSlot, Is.EqualTo(CompanionEquipmentRules.Slot));
        Assert.That(player.SelectedCharacter!.Inventory!.Items.Count(i => i == pet), Is.EqualTo(1));
    }

    [Test]
    public async Task ExpiredPetAndClassRestrictedPetAreRejectedAsync()
    {
        var (player, pet) = await CreateAsync(80).ConfigureAwait(false);
        var original = pet.ItemSlot;
        pet.ExpiresAtUtc = DateTime.UtcNow.AddDays(-1);
        var actions = new CosmeticArmorActions();
        await actions.ExecuteAsync(player, 1, 1, original, 8).ConfigureAwait(false);
        Assert.That(pet.ItemSlot, Is.EqualTo(original));
        pet.ExpiresAtUtc = null;
        pet.Definition!.QualifiedCharacters.Clear();
        pet.Definition.QualifiedCharacters.Add(new Basic.CharacterClass { Id = Guid.NewGuid(), Number = 16 });
        await actions.ExecuteAsync(player, 2, 1, original, 8).ConfigureAwait(false);
        Assert.That(pet.ItemSlot, Is.EqualTo(original));
    }

    [Test]
    public async Task RentalExpirationRemovesPetAndBonusesAsync()
    {
        var (player, pet) = await CreateAsync(80).ConfigureAwait(false);
        pet.ExpiresAtUtc = DateTime.UtcNow.AddDays(1);
        pet.Definition!.BasePowerUpAttributes.Add(new Basic.ItemBasePowerUpDefinition { TargetAttribute = Stats.MaximumHealth, BaseValue = 40 });
        var before = player.Attributes![Stats.MaximumHealth];
        await new CosmeticArmorActions().ExecuteAsync(player, 1, 1, pet.ItemSlot, 8).ConfigureAwait(false);
        Assert.That(player.Attributes[Stats.MaximumHealth], Is.EqualTo(before + 40));
        pet.ExpiresAtUtc = DateTime.UtcNow.AddDays(-1);
        await ItemExpiration.RemoveExpiredAsync(player, DateTime.UtcNow).ConfigureAwait(false);
        Assert.That(player.SelectedCharacter!.Inventory!.Items, Does.Not.Contain(pet));
        Assert.That(CompanionEquipmentRules.Stored(player), Is.Empty);
        Assert.That(player.Attributes[Stats.MaximumHealth], Is.EqualTo(before));
    }

    private static async Task<(Player Player, Item Pet)> CreateAsync(short number)
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        await player.PlayerState.TryAdvanceToAsync(PlayerState.EnteredWorld).ConfigureAwait(false);
        player.SelectedCharacter!.CharacterClass = new Basic.CharacterClass { Number = 0 };
        return (player, await AddAsync(player, number).ConfigureAwait(false));
    }

    private static async Task<Item> AddAsync(Player player, short number)
    {
        var slot = new Basic.ItemSlotType();
        slot.ItemSlots.Add(InventoryConstants.PetSlot);
        var definition = new Basic.ItemDefinition { Id = Guid.NewGuid(), Group = 13, Number = number, Width = 1, Height = 1, Durability = 100, ItemSlot = slot, Name = "Pet" };
        if (number is 37 or 300 or 312 or 309 or 369)
        {
            definition.Skill = new Basic.Skill { DamageType = DataModel.Configuration.DamageType.Fenrir };
        }
        definition.QualifiedCharacters.Add(player.SelectedCharacter!.CharacterClass!);
        player.GameContext.Configuration.Items.Add(definition);
        var pet = player.PersistenceContext.CreateNew<Item>();
        pet.Definition = definition;
        pet.Durability = 100;
        Assert.That(await player.Inventory!.AddItemAsync(pet).ConfigureAwait(false), Is.True);
        return pet;
    }
}
