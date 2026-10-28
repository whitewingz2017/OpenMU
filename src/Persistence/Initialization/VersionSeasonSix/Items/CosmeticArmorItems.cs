// <copyright file="CosmeticArmorItems.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Items;

using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.Interfaces;

/// <summary>Initializes tradable, zero-stat Set 315 skins for the custom client.</summary>
public static class CosmeticArmorItems
{
    /// <summary>Ensures the five item definitions exist; refuses to overwrite unrelated items.</summary>
    public static void Ensure(IContext context, GameConfiguration configuration)
    {
        string[] parts = ["Helm", "Armor", "Pants", "Gloves", "Boots"];
        const short setNumber = 315;
        const byte firstArmorGroup = 7;
        const int firstSkinSlot = 240;
        for (var part = 0; part < parts.Length; part++)
        {
            var group = (byte)(firstArmorGroup + part);
            var name = $"Set 315 Skin {parts[part]}";
            var definition = configuration.Items.SingleOrDefault(i => i.Group == group && i.Number == setNumber);
            if (definition is not null)
            {
                if (definition.Name.ValueInNeutralLanguage != name)
                {
                    throw new InvalidOperationException($"Item {group}/{setNumber} is already used. Cosmetic armor cannot replace it.");
                }

                continue;
            }

            var slot = context.CreateNew<ItemSlotType>();
            slot.SetGuid((short)(firstSkinSlot + part));
            slot.Description = name;
            slot.ItemSlots.Add(firstSkinSlot + part);
            configuration.ItemSlotTypes.Add(slot);
            definition = context.CreateNew<ItemDefinition>();
            definition.SetGuid(group, setNumber);
            definition.Group = group;
            definition.Number = setNumber;
            definition.Name = name;
            definition.ItemSlot = slot;
            definition.Width = 1;
            definition.Height = 1;
            definition.Durability = 1;
            definition.IsTradable = true;
            definition.IsStorable = true;
            definition.IsPersonalStoreSellable = true;
            definition.IsSellableToNpc = false;
            definition.IsDroppable = false;
            definition.IsRepairable = false;
            configuration.Items.Add(definition);
        }
    }
}
