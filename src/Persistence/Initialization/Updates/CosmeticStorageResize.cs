// <copyright file="CosmeticStorageResize.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using MUnique.OpenMU.DataModel;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic;
using BasicModel = MUnique.OpenMU.Persistence.BasicModel;

/// <summary>Preflights enlarged skin items on detached copies before changing persisted positions.</summary>
public static class CosmeticStorageResize
{
    /// <summary>Plans one storage page without changing any original items.</summary>
    /// <param name="storage">The persisted storage.</param>
    /// <param name="size">The page size.</param>
    /// <param name="offset">The page's first slot.</param>
    /// <param name="skins">The resized definitions by identifier.</param>
    /// <param name="location">The character/account and storage page shown if resizing fails.</param>
    /// <returns>The item positions to commit after every page passes.</returns>
    public static async ValueTask<IReadOnlyList<(Item Item, byte Slot)>> PlanAsync(ItemStorage storage, int size, int offset, IReadOnlyDictionary<Guid, ItemDefinition> skins, string? location = null)
    {
        var originals = storage.Items.Where(i => i.ItemSlot >= offset && i.ItemSlot < offset + size && i.Definition is not null).ToArray();
        if (!originals.Any(i => skins.ContainsKey(i.Definition!.GetId())))
        {
            return [];
        }

        var copies = originals.OrderBy(i => skins.ContainsKey(i.Definition!.GetId())).ThenBy(i => i.ItemSlot)
            .Select(i => (Original: i, Copy: new BasicModel.Item
            {
                Id = Guid.NewGuid(),
                Definition = skins.GetValueOrDefault(i.Definition!.GetId(), i.Definition!),
                ItemSlot = i.ItemSlot,
            })).ToArray();
        var detached = new BasicModel.ItemStorage();
        foreach (var entry in copies)
        {
            detached.Items.Add(entry.Copy);
        }

        try
        {
            _ = new Storage(size, 0, offset, detached);
        }
        catch (ArgumentException)
        {
            detached.Items.Clear();
            var repacked = new Storage(size, 0, offset, detached);
            foreach (var entry in copies.OrderByDescending(e => e.Copy.Definition!.Width * e.Copy.Definition.Height)
                         .ThenByDescending(e => e.Copy.Definition!.Height).ThenBy(e => e.Original.ItemSlot))
            {
                if (!await repacked.AddItemAsync(entry.Copy).ConfigureAwait(false))
                {
                    throw NotEnoughSpace(location ?? $"Storage slots {offset}-{offset + size - 1}", size, copies.Select(e => e.Copy), skins);
                }
            }
        }

        return copies.Where(e => e.Original.ItemSlot != e.Copy.ItemSlot).Select(e => (e.Original, e.Copy.ItemSlot)).ToArray();
    }

    /// <summary>Preflights all affected inventory, store and vault pages, then commits only their new positions.</summary>
    /// <param name="context">The update context.</param>
    /// <param name="configuration">The updated configuration.</param>
    /// <returns>A task representing the operation.</returns>
    public static async ValueTask ApplyAsync(IContext context, GameConfiguration configuration)
    {
        var skins = configuration.Items.Where(i => i.ItemSlot?.ItemSlots.Any(s => s >= 240 && s <= 248) == true).ToDictionary(i => i.GetId());
        var plan = new List<(Item Item, byte Slot)>();
        foreach (var character in await context.GetAsync<Character>().ConfigureAwait(false))
        {
            if (character.Inventory is not { } inventory)
            {
                continue;
            }

            plan.AddRange(await PlanAsync(inventory, InventoryConstants.InventoryRows * InventoryConstants.RowSize, InventoryConstants.EquippableSlotsCount, skins, $"Character '{character.Name}': main inventory").ConfigureAwait(false));
            for (var extension = 0; extension < Math.Clamp(character.InventoryExtensions, 0, InventoryConstants.MaximumNumberOfExtensions); extension++)
            {
                var size = InventoryConstants.RowsOfOneExtension * InventoryConstants.RowSize;
                plan.AddRange(await PlanAsync(inventory, size, InventoryConstants.FirstExtensionItemSlotIndex + (extension * size), skins, $"Character '{character.Name}': inventory extension {extension + 1}").ConfigureAwait(false));
            }

            plan.AddRange(await PlanAsync(inventory, InventoryConstants.StoreSize, InventoryConstants.FirstStoreItemSlotIndex, skins, $"Character '{character.Name}': personal store").ConfigureAwait(false));
        }

        foreach (var account in await context.GetAsync<Account>().ConfigureAwait(false))
        {
            if (account.Vault is not { } vault)
            {
                continue;
            }

            plan.AddRange(await PlanAsync(vault, InventoryConstants.WarehouseSize, 0, skins, $"Account '{account.LoginName}': warehouse page 1").ConfigureAwait(false));
            if (account.IsVaultExtended)
            {
                plan.AddRange(await PlanAsync(vault, InventoryConstants.WarehouseSize, InventoryConstants.WarehouseSize, skins, $"Account '{account.LoginName}': warehouse page 2").ConfigureAwait(false));
            }
        }

        foreach (var entry in plan)
        {
            entry.Item.ItemSlot = entry.Slot;
        }
    }

    private static InvalidOperationException NotEnoughSpace(string location, int capacity, IEnumerable<Item> items, IReadOnlyDictionary<Guid, ItemDefinition> skins)
    {
        var entries = items.ToArray();
        var required = entries.Sum(i => i.Definition!.Width * i.Definition.Height);
        var summary = string.Join(", ", entries.Where(i => skins.ContainsKey(i.Definition!.GetId()))
            .GroupBy(i => i.Definition!.GetId()).Select(group =>
            {
                var definition = group.First().Definition!;
                return $"{definition.Name.ValueInNeutralLanguage} x{group.Count()} ({definition.Width}x{definition.Height})";
            }));
        return new InvalidOperationException($"{location}: enlarged skins cannot fit after repacking. Items require {required} cells; page capacity is {capacity} cells. Skins: {summary}. Move some items to another storage page and retry. No saved item positions were changed.");
    }
}
