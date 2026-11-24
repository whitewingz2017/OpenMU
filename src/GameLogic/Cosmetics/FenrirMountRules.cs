// <copyright file="FenrirMountRules.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Cosmetics;

using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.GameLogic.Attributes;

/// <summary>Recognizes configured Fenrir helpers by their mounted Plasma Storm skill.</summary>
public static class FenrirMountRules
{
    /// <summary>Checks whether the configured item is a Fenrir mount.</summary>
    /// <param name="definition">The item definition.</param>
    public static bool IsFenrir(ItemDefinition? definition)
        => definition?.Group == ItemConstants.Fenrir.Group
            && definition.Skill?.DamageType == DamageType.Fenrir
            && definition.ItemSlot?.ItemSlots.Contains(InventoryConstants.PetSlot) == true;

    /// <summary>Recognizes configured horse mounts by the horse equipment attribute.</summary>
    /// <param name="definition">The item definition.</param>
    public static bool IsDarkHorse(ItemDefinition? definition)
        => definition?.Group == ItemConstants.DarkHorse.Group
            && definition.ItemSlot?.ItemSlots.Contains(InventoryConstants.PetSlot) == true
            && definition.BasePowerUpAttributes.Any(power => power.TargetAttribute?.Id == Stats.IsHorseEquipped.Id && power.BaseValue > 0);

    /// <summary>Checks both supported families of riding mounts.</summary>
    /// <param name="definition">The item definition.</param>
    public static bool IsMount(ItemDefinition? definition) => IsFenrir(definition) || IsDarkHorse(definition);

    /// <summary>Checks whether this is one of the configured custom Fenrir models.</summary>
    /// <param name="definition">The item definition.</param>
    public static bool IsCustom(ItemDefinition? definition)
        => IsMount(definition) && definition!.Number != ItemConstants.Fenrir.Number && definition.Number != ItemConstants.DarkHorse.Number;

    /// <summary>Includes standalone mounts in the skin shop, alongside cosmetic items.</summary>
    /// <param name="definition">The item definition.</param>
    public static bool IsShopItem(ItemDefinition? definition)
        => !IsDuplicateWing(definition) && (CosmeticArmorRules.GetSlot(definition) is not null || IsCustom(definition));

    // Keep definitions for owned copies, but retire the duplicate imported models from sale.
    private static bool IsDuplicateWing(ItemDefinition? definition)
        => definition is { Group: 12, Number: 312 or 319 or 329 or 334 };
}
