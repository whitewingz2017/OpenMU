// <copyright file="CosmeticArmorItems.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Items;

using System.Text.Json;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.Interfaces;
using MUnique.OpenMU.Persistence.Initialization.CharacterClasses;

/// <summary>Initializes permanent tradable cosmetic items for the custom client.</summary>
public static class CosmeticArmorItems
{
    /// <summary>The cosmetic part of wing and cape skins.</summary>
    private const int WingPart = 7;
    private const int PetPart = 8;

    /// <summary>The first cosmetic item slot; a skin of part N is worn in this slot plus N.</summary>
    private const int FirstCosmeticSlot = 240;

    /// <summary>Gives every wing and cape skin the movement speed of normal wings.</summary>
    /// <param name="context">The configuration context.</param>
    /// <param name="configuration">The target game configuration.</param>
    public static void EnsureWingSkinMovementSpeed(IContext context, GameConfiguration configuration)
    {
        var wingSkins = configuration.Items.Where(i => i.ItemSlot?.ItemSlots is { Count: 1 } slots && slots.Single() == FirstCosmeticSlot + WingPart);
        foreach (var definition in wingSkins)
        {
            EnsureMovementSpeed(context, configuration, definition);
        }
    }

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
        EnsureItem(context, configuration, 0, 101, "Sacred Glove Skin", 5);
        EnsureItem(context, configuration, 2, 101, "Battle Scepter Skin", 5);
        EnsureItem(context, configuration, 5, 101, "Summoner Book Skin", 6);
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
            if (skin.Mount)
            {
                FenrirMountItems.Ensure(context, configuration, skin.Group, skin.Number);
            }
        }

        RestrictWeaponClasses(configuration, imports);
        RestrictImportedClasses(configuration, imports);
        EnsureSetVisuals(context, configuration, imports);
    }

    private static void EnsureSetVisuals(IContext context, GameConfiguration configuration, FinalMuSkin[] imports)
    {
        foreach (var skin in imports.Where(s => s.Group == 8 && s.Part == 1))
        {
            var armor = configuration.Items.Single(d => d.Group == skin.Group && d.Number == skin.Number);
            string[] parts = ["Helm", "Armor", "Pants", "Gloves", "Boots"];
            for (var part = 0; part < parts.Length; ++part)
            {
                var group = (byte)(7 + part);
                if ((skin.ArmorMask & (1 << part)) == 0 || configuration.Items.Any(d => d.Group == group && d.Number == skin.Number))
                {
                    continue;
                }

                // Observers need each model defined, but only the armor is an equippable skin.
                var visual = context.CreateNew<ItemDefinition>();
                visual.SetGuid(group, skin.Number);
                visual.Group = group;
                visual.Number = skin.Number;
                visual.Name = $"{skin.Name} ({parts[part]} visual)";
                visual.Width = armor.Width;
                visual.Height = armor.Height;
                visual.IsTradable = false;
                visual.IsDroppable = false;
                visual.IsSellableToNpc = false;
                visual.IsStorable = false;
                visual.IsPersonalStoreSellable = false;
                visual.IsRepairable = false;
                foreach (var characterClass in armor.QualifiedCharacters)
                {
                    visual.QualifiedCharacters.Add(characterClass);
                }

                configuration.Items.Add(visual);
            }
        }
    }

    private static void RestrictWeaponClasses(GameConfiguration configuration, FinalMuSkin[] imports)
    {
        var importedIds = imports.Select(s => (s.Group, s.Number)).ToHashSet();
        var weapons = configuration.Items.Where(d => d.Group <= 5
            && (importedIds.Contains((d.Group, d.Number))
                || (d.Number == 100 && d.Group is 0 or 2 or 4 or 5)
                || (d.Number == 101 && d.Group is 0 or 2 or 5)));
        foreach (var definition in weapons)
        {
            var classes = (definition.Group, definition.Number) switch
            {
                (0, 101) => CharacterClasses.AllFighters,
                (2, 101) => CharacterClasses.AllLords,
                (5, 101 or 302 or 304) => CharacterClasses.AllSummoners,
                (0 or 3, _) => CharacterClasses.AllKnights | CharacterClasses.AllMGs,
                (2, _) => CharacterClasses.AllKnightsLordsAndMGs | CharacterClasses.AllFighters,
                (4, _) => CharacterClasses.AllElfs,
                _ => CharacterClasses.AllMagicians,
            };
            definition.QualifiedCharacters.Clear();
            foreach (var characterClass in configuration.DetermineCharacterClasses(classes))
            {
                definition.QualifiedCharacters.Add(characterClass);
            }
        }
    }

    private static void RestrictImportedClasses(GameConfiguration configuration, FinalMuSkin[] imports)
    {
        foreach (var skin in imports.Where(s => s.Classes is not null))
        {
            var definition = configuration.Items.Single(d => d.Group == skin.Group && d.Number == skin.Number);
            var allowed = CharacterClasses.None;
            foreach (var (name, requirement) in skin.Classes!)
            {
                var family = name switch
                {
                    "darkWizard" => CharacterClasses.AllWizards,
                    "darkKnight" => CharacterClasses.AllKnights,
                    "fairyElf" => CharacterClasses.AllElfs,
                    "magicGladiator" => CharacterClasses.AllMGs,
                    "darkLord" => CharacterClasses.AllLords,
                    "summoner" => CharacterClasses.AllSummoners,
                    "rageFighter" => CharacterClasses.AllFighters,
                    _ => throw new InvalidOperationException($"Unknown cosmetic class {name}."),
                };
                allowed |= requirement switch
                {
                    1 => family,
                    2 => family & CharacterClasses.AllMastersAndSecondClass,
                    3 => family & CharacterClasses.AllMasters,
                    _ => throw new InvalidOperationException($"Invalid cosmetic class requirement {requirement}."),
                };
            }

            definition.QualifiedCharacters.Clear();
            foreach (var characterClass in configuration.DetermineCharacterClasses(allowed))
            {
                definition.QualifiedCharacters.Add(characterClass);
            }
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

        // An item which already occupies this cosmetic slot is one of these skins, and so is an imported
        // mount (a copy of the normal Fenrir, see FenrirMountItems); either may be renamed, e.g. to the
        // FinalMU name of an imported skin. Any other item with this number is not replaced.
        var fenrirSkill = configuration.Items
            .SingleOrDefault(i => i.Group == ItemConstants.Fenrir.Group && i.Number == ItemConstants.Fenrir.Number)?.Skill;
        var isThisSkin = (definition?.ItemSlot?.ItemSlots is { Count: 1 } ownSlots && ownSlots.Single() == FirstCosmeticSlot + part)
            || (definition is not null && part == PetPart && definition.Group == ItemConstants.Fenrir.Group
                && definition.Number != ItemConstants.Fenrir.Number && fenrirSkill is not null && definition.Skill == fenrirSkill);
        if (definition is not null && definition.Name.ValueInNeutralLanguage != name && !isThisSkin
            && !(part == 1 && definition.Name.ValueInNeutralLanguage == $"Set {number} Skin")
            && !(name.EndsWith(" Mount", StringComparison.Ordinal)
                && definition.Name.ValueInNeutralLanguage is { } previousName
                && (previousName == name.Replace(" Mount", " Skin", StringComparison.Ordinal)
                    || previousName == $"FinalMU {name.Replace(" Mount", " Skin", StringComparison.Ordinal)}")
                && definition.ItemSlot?.ItemSlots.SingleOrDefault() == 248)
            && !(definition.Name.ValueInNeutralLanguage == $"FinalMU {name}"
                && definition.ItemSlot?.ItemSlots is { Count: 1 } legacySlots && legacySlots.Single() == 240 + part))
        {
            throw new InvalidOperationException($"Item {group}/{number} is already used. Cosmetic items cannot replace it.");
        }

        var slotNumber = FirstCosmeticSlot + part;
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
            definition.Durability = part == PetPart ? MuunItems.DefaultLife : (byte)1;
            definition.IsTradable = true;
            definition.IsStorable = true;
            definition.IsPersonalStoreSellable = true;
            definition.IsSellableToNpc = false;
            definition.IsDroppable = false;
            definition.IsRepairable = false;
            configuration.Items.Add(definition);
        }

        (definition.Width, definition.Height) = CosmeticItemDimensions.Get(group, name);
        definition.Name = part == 1 && (number is 100 or 315) ? $"Set {number} Skin" : name;

        EnsureBonuses(configuration, definition, part);
        if (part == WingPart)
        {
            EnsureMovementSpeed(context, configuration, definition);
        }

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

        var number = part == WingPart ? ItemOptionDefinitionNumbers.Wing3rd
            : definition.Group is 6 or 8 ? ItemOptionDefinitionNumbers.ExcellentDefense
            : definition.Group == 5 ? ItemOptionDefinitionNumbers.ExcellentWizardry : ItemOptionDefinitionNumbers.ExcellentPhysical;
        var options = configuration.ItemOptions.SingleOrDefault(option => option.GetId() == GuidHelper.CreateGuid<ItemOptionDefinition>((short)number));
        if (options is not null && !definition.PossibleItemOptions.Contains(options))
        {
            definition.PossibleItemOptions.Add(options);
        }
    }

    private static void EnsureMovementSpeed(IContext context, GameConfiguration configuration, ItemDefinition definition)
    {
        foreach (var attribute in new[] { Stats.MovementSpeed, Stats.MovementSpeedUnderwater })
        {
            if (definition.BasePowerUpAttributes.Any(p => p.TargetAttribute?.Id == attribute.Id))
            {
                continue;
            }

            var powerUp = context.CreateNew<ItemBasePowerUpDefinition>();
            powerUp.TargetAttribute = attribute.GetPersistent(configuration);
            powerUp.BaseValue = MovementSpeedConstants.DefaultWingMovementSpeed;
            powerUp.AggregateType = AggregateType.Maximum;
            definition.BasePowerUpAttributes.Add(powerUp);
        }
    }

    private sealed record FinalMuSkin(byte Group, short Number, string Name, int Part, bool Mount = false, Dictionary<string, int>? Classes = null, int ArmorMask = 0);
}
