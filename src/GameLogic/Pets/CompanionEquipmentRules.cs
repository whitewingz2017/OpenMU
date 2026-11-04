// <copyright file="CompanionEquipmentRules.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Pets;

using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.GameLogic.Cosmetics;

/// <summary>Independent equipment for ordinary pets alongside the normal riding helper.</summary>
public static class CompanionEquipmentRules
{
    /// <summary>The former Skin pet position; keeping it preserves existing owned items.</summary>
    public const byte Slot = CosmeticArmorRules.FirstSlot + CosmeticArmorRules.SlotCount - 1;

    /// <summary>Gets stored companion equipment, including expired items for cleanup.</summary>
    /// <param name="player">The equipment owner.</param>
    public static IEnumerable<Item> Stored(Player player)
        => player.SelectedCharacter?.Inventory?.Items.Where(i => i.ItemSlot == Slot && CosmeticArmorRules.FitsSlot(i.Definition, Slot)) ?? [];

    /// <summary>Identifies ordinary pet equipment while excluding all configured riding helpers.</summary>
    /// <param name="definition">The configured item.</param>
    public static bool IsOrdinaryPet(ItemDefinition? definition)
        => definition?.ItemSlot?.ItemSlots.Contains(InventoryConstants.PetSlot) == true
            && !FenrirMountRules.IsFenrir(definition)
            && !(definition.Group == ItemConstants.DarkHorse.Group && definition.Number == ItemConstants.DarkHorse.Number)
            && !(definition.Group == ItemConstants.Uniria.Group && definition.Number == ItemConstants.Uniria.Number)
            && !(definition.Group == ItemConstants.Dinorant.Group && definition.Number == ItemConstants.Dinorant.Number);
}
