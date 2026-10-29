// <copyright file="CosmeticArmorItems.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Items;

using System.Text.Json;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.Interfaces;

/// <summary>Initializes permanent tradable cosmetic items for the custom client.</summary>
public static class CosmeticArmorItems
{
    /// <summary>Ensures skin definitions exist without overwriting unrelated items.</summary>
    /// <param name="context">The configuration context.</param>
    /// <param name="configuration">The target game configuration.</param>
    public static void Ensure(IContext context, GameConfiguration configuration)
    {
        string[] parts = ["Helm", "Armor", "Pants", "Gloves", "Boots"];
        for (var part = 0; part < parts.Length; part++)
        {
            EnsureItem(context, configuration, (byte)(7 + part), 315, $"Set 315 Skin {parts[part]}", part, true);
            EnsureItem(context, configuration, (byte)(7 + part), 100, $"Set 100 {parts[part]} Skin", part);
        }

        EnsureItem(context, configuration, 0, 100, "Sword Skin", 5);
        EnsureItem(context, configuration, 2, 100, "Mace Skin", 5);
        EnsureItem(context, configuration, 4, 100, "Bow Skin", 6);
        EnsureItem(context, configuration, 5, 100, "Staff Skin", 5);
        EnsureItem(context, configuration, 6, 100, "Shield Skin", 6);
        EnsureItem(context, configuration, 12, 180, "Cape Skin", 7);
        EnsureItem(context, configuration, 12, 200, "Wing Skin", 7);
        EnsureItem(context, configuration, 12, 210, "Wing 210 Skin", 7);
        EnsureItem(context, configuration, 12, 213, "Cape 213 Skin", 7);
        EnsureItem(context, configuration, 13, 200, "Pet 208 Skin", 8);
        using var catalog = typeof(CosmeticArmorItems).Assembly.GetManifestResourceStream("FinalMuCosmeticSkins.json")
            ?? throw new InvalidOperationException("The FinalMU cosmetic catalog is missing.");
        var imports = JsonSerializer.Deserialize<FinalMuSkin[]>(catalog)
            ?? throw new InvalidOperationException("The FinalMU cosmetic catalog is invalid.");
        foreach (var skin in imports)
        {
            EnsureItem(context, configuration, skin.Group, skin.Number, skin.Name, skin.Part);
        }
    }

    private static void EnsureItem(
        IContext context,
        GameConfiguration configuration,
        byte group,
        short number,
        string name,
        int part,
        bool female = false)
    {
        var definition = configuration.Items.SingleOrDefault(i => i.Group == group && i.Number == number);
        if (definition is not null && definition.Name.ValueInNeutralLanguage != name
            && !(part == 1 && definition.Name.ValueInNeutralLanguage == $"Set {number} Skin")
            && !(definition.Name.ValueInNeutralLanguage == $"FinalMU {name}"
                && definition.ItemSlot?.ItemSlots is { Count: 1 } legacySlots && legacySlots.Single() == 240 + part))
        {
            throw new InvalidOperationException($"Item {group}/{number} is already used. Cosmetic items cannot replace it.");
        }

        var slotNumber = 240 + part;
        var slot = configuration.ItemSlotTypes.SingleOrDefault(s => s.ItemSlots.Count == 1 && s.ItemSlots.Single() == slotNumber);
        if (slot is null)
        {
            slot = context.CreateNew<ItemSlotType>();
            slot.SetGuid((short)slotNumber);
            slot.Description = $"Cosmetic slot {part}";
            slot.ItemSlots.Add(slotNumber);
            configuration.ItemSlotTypes.Add(slot);
        }

        if (definition is null)
        {
            definition = context.CreateNew<ItemDefinition>();
            definition.SetGuid(group, number);
            definition.Group = group;
            definition.Number = number;
            definition.Name = name;
            definition.ItemSlot = slot;
            definition.Durability = 1;
            definition.IsTradable = true;
            definition.IsStorable = true;
            definition.IsPersonalStoreSellable = true;
            definition.IsSellableToNpc = false;
            definition.IsDroppable = false;
            definition.IsRepairable = false;
            configuration.Items.Add(definition);
        }

        (definition.Width, definition.Height) = CosmeticItemDimensions.Get(group, name);
        definition.Name = part == 1 ? $"Set {number} Skin" : name;

        EnsureBonuses(configuration, definition, part);

        if (female)
        {
            definition.QualifiedCharacters.Clear();
            foreach (var characterClass in configuration.CharacterClasses.Where(c => c.Number is 8 or 10 or 11 or 20 or 22 or 23))
            {
                definition.QualifiedCharacters.Add(characterClass);
            }
        }
    }

    private static void EnsureBonuses(GameConfiguration configuration, ItemDefinition definition, int part)
    {
        if (part is 0 or 2 or 3 or 4 or 8)
        {
            return;
        }

        var number = part == 7 ? ItemOptionDefinitionNumbers.Wing3rd
            : definition.Group is 6 or 8 ? ItemOptionDefinitionNumbers.ExcellentDefense
            : definition.Group == 5 ? ItemOptionDefinitionNumbers.ExcellentWizardry : ItemOptionDefinitionNumbers.ExcellentPhysical;
        var options = configuration.ItemOptions.SingleOrDefault(option => option.GetId() == GuidHelper.CreateGuid<ItemOptionDefinition>((short)number));
        if (options is not null && !definition.PossibleItemOptions.Contains(options))
        {
            definition.PossibleItemOptions.Add(options);
        }
    }

    private sealed record FinalMuSkin(byte Group, short Number, string Name, int Part);
}
