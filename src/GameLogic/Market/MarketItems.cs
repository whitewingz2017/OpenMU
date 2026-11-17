// <copyright file="MarketItems.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Market;

using System.Globalization;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.GameLogic.Cosmetics;
using MUnique.OpenMU.GameLogic.Muun;
using MUnique.OpenMU.GameLogic.Pets;

/// <summary>Which items may be sold on the Marketplace, how they're named and categorized.</summary>
public static class MarketItems
{
    private const int LastWeaponGroup = 5;
    private const int LastArmorGroup = 11;
    private const string JewelWord = "Jewel";

    /// <summary>
    /// Determines whether the item may be listed: a tradable, personal-store-sellable item of the inventory,
    /// not worn, without a time limit and not priced for the personal store.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns><c>true</c> if the item may be listed.</returns>
    public static bool CanSell(Item item)
        => item.Definition is { IsPersonalStoreSellable: true, IsTradable: true }
           && item.ItemSlot > InventoryConstants.LastEquippableItemSlotIndex
           && item.ExpiresAtUtc is null
           && !item.StorePrice.HasValue;

    /// <summary>Gets the name of the item as the lists show it, e.g. "Sword of Destruction +13".</summary>
    /// <param name="item">The item.</param>
    /// <returns>The name.</returns>
    public static string Name(Item item)
    {
        var name = item.Definition?.GetNameForLevel(item.Level) ?? string.Empty;
        return item.Level > 0 ? string.Create(CultureInfo.InvariantCulture, $"{name} +{item.Level}") : name;
    }

    /// <summary>
    /// Determines whether the item is a pet, a mount, a Muun or a skin: an item of the pet slot, a companion pet,
    /// a mount, a configured Muun species, or an item of a cosmetic slot.
    /// </summary>
    /// <param name="definition">The item definition.</param>
    /// <param name="muun">The Muun configuration, or <c>null</c>.</param>
    /// <returns><c>true</c> for the Other category.</returns>
    public static bool IsOther(ItemDefinition definition, MuunConfiguration? muun)
        => CosmeticArmorRules.GetSlot(definition) is not null
           || definition.ItemSlot?.ItemSlots.Contains(InventoryConstants.PetSlot) == true
           || CompanionEquipmentRules.IsOrdinaryPet(definition)
           || MuunRules.IsMount(definition)
           || MuunRules.Find(muun, (definition.Group * 512) + definition.Number) is not null;

    /// <summary>Gets the category of the item.</summary>
    /// <param name="item">The item.</param>
    /// <param name="settings">The settings, for the jewel currencies.</param>
    /// <param name="muun">The Muun configuration, for the Muun species; <c>null</c> without Muuns.</param>
    /// <returns>The category.</returns>
    public static MarketCategory CategoryOf(Item? item, MarketSettings settings, MuunConfiguration? muun)
    {
        if (item?.Definition is not { } definition)
        {
            return MarketCategory.Materials;
        }

        if (IsOther(definition, muun))
        {
            return MarketCategory.Other;
        }

        if (definition.ItemSlot?.ItemSlots.Contains(InventoryConstants.WingsSlot) == true)
        {
            return MarketCategory.Wings;
        }

        if (definition.Group <= LastWeaponGroup)
        {
            return MarketCategory.Weapons;
        }

        if (definition.Group <= LastArmorGroup)
        {
            return MarketCategory.Armor;
        }

        var type = (definition.Group << 12) | (ushort)definition.Number;
        return MarketCurrency.Allowed(settings).Contains(type)
               || definition.Name.ToString()?.Contains(JewelWord, StringComparison.OrdinalIgnoreCase) == true
            ? MarketCategory.Jewels
            : MarketCategory.Materials;
    }
}
