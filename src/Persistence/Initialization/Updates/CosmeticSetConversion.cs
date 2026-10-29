// <copyright file="CosmeticSetConversion.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic.Cosmetics;

/// <summary>Converts every old armor piece into a full-set item without deleting owned items.</summary>
public static class CosmeticSetConversion
{
    /// <summary>Converts definitions and preflights all storage pages; failure restores every changed item.</summary>
    /// <param name="context">The update persistence context.</param>
    /// <param name="configuration">The target configuration.</param>
    public static async ValueTask ApplyAsync(IContext context, GameConfiguration configuration)
    {
        var changes = new List<(Item Item, ItemDefinition Definition, byte Slot, double Durability, bool HasSkill)>();
        var addedOptions = new List<(Item Item, ItemOptionLink Link)>();
        var definitions = configuration.Items.Where(d => (d.ItemSlot?.ItemSlots.Any(slot => slot is >= 240 and <= 248) == true || FenrirMountRules.IsCustom(d))).ToDictionary(d => d.GetId());
        try
        {
            foreach (var item in await context.GetAsync<Item>().ConfigureAwait(false))
            {
                if (item.Definition is not { } definition || !definitions.TryGetValue(definition.GetId(), out var configuredDefinition))
                {
                    continue;
                }

                changes.Add((item, definition, item.ItemSlot, item.Durability, item.HasSkill));
                item.Definition = definition.Group is >= 7 and <= 11 && definition.Number is 100 or 315
                    ? configuration.Items.Single(d => d.Group == 8 && d.Number == definition.Number)
                    : configuredDefinition;

                if (FenrirMountRules.IsCustom(item.Definition))
                {
                    if (!item.HasSkill || item.ItemSlot == 248 || definition.ItemSlot?.ItemSlots.Contains(248) == true)
                    {
                        item.Durability = item.Definition.Durability;
                    }

                    item.HasSkill = true;
                }

                if (item.ItemOptions.Count == 0 && SkinBonuses.Choices(item.Definition).OrderBy(o => o.Number).FirstOrDefault() is { } option)
                {
                    var link = context.CreateNew<ItemOptionLink>();
                    link.ItemOption = option;
                    link.Level = 1;
                    item.ItemOptions.Add(link);
                    addedOptions.Add((item, link));
                }
            }

            foreach (var character in await context.GetAsync<Character>().ConfigureAwait(false))
            {
                var inventory = character.Inventory?.Items;
                if (inventory is null)
                {
                    continue;
                }

                foreach (var mount in inventory.Where(i => i.ItemSlot == 248 && FenrirMountRules.IsCustom(i.Definition)).ToArray())
                {
                    mount.ItemSlot = DataModel.InventoryConstants.EquippableSlotsCount;
                }

                foreach (var weapon in inventory.Where(i => i.ItemSlot is 245 or 246
                    && i.Definition is { Group: <= 5 } definition && definition.QualifiedCharacters.Count > 0
                    && (character.CharacterClass is not { } ownerClass || !definition.QualifiedCharacters.Contains(ownerClass))).ToArray())
                {
                    weapon.ItemSlot = DataModel.InventoryConstants.EquippableSlotsCount;
                }

                // Retired body slots return to the bag. Existing armor keeps the full-set slot.
                foreach (var item in inventory.Where(i => i.ItemSlot is 240 or 242 or 243 or 244
                    && i.Definition?.Group == 8 && i.Definition.Number is 100 or 315).ToArray())
                {
                    item.ItemSlot = DataModel.InventoryConstants.EquippableSlotsCount;
                }
            }

            await CosmeticStorageResize.ApplyAsync(context, configuration).ConfigureAwait(false);
        }
        catch
        {
            foreach (var change in changes)
            {
                change.Item.Definition = change.Definition;
                change.Item.ItemSlot = change.Slot;
                change.Item.Durability = change.Durability;
                change.Item.HasSkill = change.HasSkill;
            }

            foreach (var added in addedOptions)
            {
                added.Item.ItemOptions.Remove(added.Link);
                await context.DeleteAsync(added.Link).ConfigureAwait(false);
            }

            throw;
        }
    }
}
