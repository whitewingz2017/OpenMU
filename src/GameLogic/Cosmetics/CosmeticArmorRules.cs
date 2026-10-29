// <copyright file="CosmeticArmorRules.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Cosmetics;

using MUnique.OpenMU.DataModel.Configuration.Items;

/// <summary>Reserved Season 6 inventory positions for cosmetic armor, outside all gear, bag and shop slots.</summary>
public static class CosmeticArmorRules
{
    /// <summary>The custom client reserves nine positions after the last personal-store position (235).</summary>
    public const byte FirstSlot = 240;

    /// <summary>The protocol slot count, including retired body-piece positions.</summary>
    public const byte SlotCount = 9;

    /// <summary>The normal helmet position, followed by armor, pants, gloves and boots.</summary>
    public const byte FirstGearSlot = 2;

    /// <summary>Maps a cosmetic part to its visible gear slot.</summary>
    public static byte GearSlot(int part) => part < 5 ? (byte)(part + FirstGearSlot)
        : part == 5 ? (byte)0 : part == 6 ? (byte)1 : part == 7 ? (byte)7 : (byte)8;

    /// <summary>Checks the configured class restrictions; Set 315 always requires a female class.</summary>
    public static bool CanEquip(Player player, ItemDefinition? definition)
    {
        if (definition is null || player.SelectedCharacter?.CharacterClass is not { } characterClass)
        {
            return false;
        }
        if (definition.Group is >= 7 and <= 11 && definition.Number == 315)
        {
            return characterClass.Number is 8 or 10 or 11 or 20 or 22 or 23;
        }

        return definition.QualifiedCharacters.Count == 0 || definition.QualifiedCharacters.Contains(characterClass);
    }

    /// <summary>Gets the configured cosmetic slot, or null for ordinary items.</summary>
    public static byte? GetSlot(ItemDefinition? definition)
    {
        if (definition?.ItemSlot?.ItemSlots is not { Count: 1 } slots)
        {
            return null;
        }

        var slot = slots.Single();
        return slot is 241 or >= 245 and <= 248 ? (byte)slot : null;
    }

    /// <summary>Gets valid equipped skins from the complete persisted inventory.</summary>
    public static IEnumerable<Item> Equipped(Player player)
        => player.SelectedCharacter?.Inventory?.Items.Where(i => GetSlot(i.Definition) == i.ItemSlot
            && i.ItemSlot >= FirstSlot && i.ItemSlot < FirstSlot + SlotCount
            && (i.ExpiresAtUtc is null || i.ExpiresAtUtc > DateTime.UtcNow)) ?? [];

    /// <summary>Gets visible equipment without changing the items supplying combat attributes.</summary>
    public static IEnumerable<Item> VisibleEquipment(Player player)
    {
        var skins = new Dictionary<byte, ItemDefinition>();
        foreach (var item in Equipped(player).Where(i => CanEquip(player, i.Definition)))
        {
            if (item.Definition!.Group == 8)
            {
                foreach (var definition in player.GameContext.Configuration.Items.Where(d => d.Group is >= 7 and <= 11 && d.Number == item.Definition.Number))
                {
                    skins[(byte)(definition.Group - 5)] = definition;
                }
            }
            else
            {
                skins[GearSlot(item.ItemSlot - FirstSlot)] = item.Definition;
            }
        }

        foreach (var item in player.Inventory?.EquippedItems ?? [])
        {
            if (!skins.ContainsKey(item.ItemSlot))
            {
                yield return item;
            }
        }

        foreach (var entry in skins)
        {
            yield return new TemporaryItem { Definition = entry.Value, ItemSlot = entry.Key };
        }
    }
}
