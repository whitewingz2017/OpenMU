// <copyright file="CosmeticArmorRules.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Cosmetics;

using MUnique.OpenMU.DataModel.Configuration.Items;

/// <summary>Reserved Season 6 inventory positions for cosmetic armor, outside all gear, bag and shop slots.</summary>
public static class CosmeticArmorRules
{
    /// <summary>The custom client reserves five positions after the last personal-store position (235).</summary>
    public const byte FirstSlot = 240;

    /// <summary>The five armor parts supported by the custom client.</summary>
    public const byte SlotCount = 5;

    /// <summary>The normal helmet position, followed by armor, pants, gloves and boots.</summary>
    public const byte FirstGearSlot = 2;

    /// <summary>Gets the configured cosmetic slot, or null for ordinary items.</summary>
    public static byte? GetSlot(ItemDefinition? definition)
    {
        if (definition?.ItemSlot?.ItemSlots is not { Count: 1 } slots)
        {
            return null;
        }

        var slot = slots.Single();
        return slot >= FirstSlot && slot < FirstSlot + SlotCount ? (byte)slot : null;
    }

    /// <summary>Gets valid equipped skins from the complete persisted inventory.</summary>
    public static IEnumerable<Item> Equipped(Player player)
        => player.SelectedCharacter?.Inventory?.Items.Where(i => GetSlot(i.Definition) == i.ItemSlot
            && i.ItemSlot >= FirstSlot && i.ItemSlot < FirstSlot + SlotCount
            && (i.ExpiresAtUtc is null || i.ExpiresAtUtc > DateTime.UtcNow)) ?? [];

    /// <summary>Gets visible equipment without changing the items supplying combat attributes.</summary>
    public static IEnumerable<Item> VisibleEquipment(Player player)
    {
        var skins = Equipped(player).ToDictionary(i => (byte)(i.ItemSlot - FirstSlot + FirstGearSlot));
        foreach (var item in player.Inventory?.EquippedItems ?? [])
        {
            if (!skins.ContainsKey(item.ItemSlot))
            {
                yield return item;
            }
        }

        foreach (var entry in skins)
        {
            yield return new TemporaryItem { Definition = entry.Value.Definition, ItemSlot = entry.Key };
        }
    }
}
