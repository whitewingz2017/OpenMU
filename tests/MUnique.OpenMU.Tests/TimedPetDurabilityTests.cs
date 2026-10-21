// <copyright file="TimedPetDurabilityTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using System.Reflection;
using MUnique.OpenMU.DataModel;
using MUnique.OpenMU.GameLogic;
using Item = MUnique.OpenMU.Persistence.BasicModel.Item;
using ItemDefinition = MUnique.OpenMU.Persistence.BasicModel.ItemDefinition;
using ItemSlotType = MUnique.OpenMU.Persistence.BasicModel.ItemSlotType;

/// <summary>
/// Verifies that period pets expire by time rather than dying from combat wear.
/// </summary>
[TestFixture]
public class TimedPetDurabilityTests
{
    /// <summary>
    /// Protects timed pets of different kinds, including a pet with almost no HP.
    /// </summary>
    /// <param name="number">The pet number.</param>
    /// <param name="durability">The remaining pet HP.</param>
    [TestCase(80, 1f)]
    [TestCase(80, 255f)]
    [TestCase(64, 1f)]
    [TestCase(123, 1f)]
    public async Task TimedPetSurvivesCombatAsync(short number, float durability)
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        player.GameContext.Configuration.DamagePerOnePetDurability = 1;
        var pet = CreatePet(number, durability);
        pet.ExpiresAtUtc = DateTime.UtcNow.AddDays(1);
        await player.Inventory!.AddItemAsync(InventoryConstants.PetSlot, pet).ConfigureAwait(false);

        await ApplyPetDamageAsync(player, pet, 100000).ConfigureAwait(false);

        Assert.That(pet.Durability, Is.EqualTo(durability));
        Assert.That(player.Inventory.GetItem(InventoryConstants.PetSlot), Is.SameAs(pet));
    }

    /// <summary>
    /// Ordinary pets still take combat wear when no expiration was purchased.
    /// </summary>
    [Test]
    public async Task PermanentPetStillTakesDamageAsync()
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        player.GameContext.Configuration.DamagePerOnePetDurability = 1;
        var pet = CreatePet(80, 100);
        await player.Inventory!.AddItemAsync(InventoryConstants.PetSlot, pet).ConfigureAwait(false);

        await ApplyPetDamageAsync(player, pet, 10).ConfigureAwait(false);

        Assert.That(pet.Durability, Is.LessThan(100));
        Assert.That(player.Inventory.GetItem(InventoryConstants.PetSlot), Is.SameAs(pet));
    }

    /// <summary>
    /// Immunity to combat wear must not prevent the normal expiration cleanup.
    /// </summary>
    [Test]
    public async Task TimedPetStillExpiresAsync()
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        var deadline = DateTime.UtcNow.AddHours(1);
        var pet = CreatePet(80, 1);
        pet.ExpiresAtUtc = deadline;
        await player.Inventory!.AddItemAsync(InventoryConstants.PetSlot, pet).ConfigureAwait(false);
        await ApplyPetDamageAsync(player, pet, 100000).ConfigureAwait(false);

        await ItemExpiration.RemoveExpiredAsync(player, deadline.AddSeconds(1)).ConfigureAwait(false);

        Assert.That(player.Inventory.Items, Does.Not.Contain(pet));
    }

    private static Item CreatePet(short number, float durability) => new()
    {
        Definition = new ItemDefinition
        {
            Group = 13,
            Number = number,
            ItemSlot = new ItemSlotType { ItemSlots = { InventoryConstants.PetSlot } },
            Width = 1,
            Height = 1,
            Durability = 255,
        },
        Durability = durability,
    };

    private static async ValueTask ApplyPetDamageAsync(Player player, Item pet, float damage)
    {
        var method = typeof(Player).GetMethod("DecreasePetDurabilityAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
        await ((ValueTask)method.Invoke(player, new object[] { pet, damage })!).ConfigureAwait(false);
    }
}
