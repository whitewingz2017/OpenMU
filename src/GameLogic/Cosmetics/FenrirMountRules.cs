// <copyright file="FenrirMountRules.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Cosmetics;

using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.DataModel.Configuration;

/// <summary>Recognizes configured Fenrir helpers by their mounted Plasma Storm skill.</summary>
public static class FenrirMountRules
{
    /// <summary>Checks whether the configured item is a Fenrir mount.</summary>
    /// <param name="definition">The item definition.</param>
    public static bool IsFenrir(ItemDefinition? definition)
        => definition?.Group == ItemConstants.Fenrir.Group
            && definition.Skill?.DamageType == DamageType.Fenrir
            && definition.ItemSlot?.ItemSlots.Contains(InventoryConstants.PetSlot) == true;

    /// <summary>Checks whether this is one of the configured custom Fenrir models.</summary>
    /// <param name="definition">The item definition.</param>
    public static bool IsCustom(ItemDefinition? definition)
        => IsFenrir(definition) && definition!.Number != ItemConstants.Fenrir.Number;

    /// <summary>Includes standalone mounts in the skin shop, alongside cosmetic items.</summary>
    /// <param name="definition">The item definition.</param>
    public static bool IsShopItem(ItemDefinition? definition)
        => CosmeticArmorRules.GetSlot(definition) is not null || IsCustom(definition);
}
