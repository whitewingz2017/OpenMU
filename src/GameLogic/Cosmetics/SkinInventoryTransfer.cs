// <copyright file="SkinInventoryTransfer.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>
namespace MUnique.OpenMU.GameLogic.Cosmetics;

using MUnique.OpenMU.GameLogic.Views.Inventory;

/// <summary>Transfers the same owned item under the caller's persistence lock, with rollback on failed saves.</summary>
public static class SkinInventoryTransfer
{
    /// <summary>Maximum number of stored skins per character.</summary>
    public const int Capacity = 120;

    /// <summary>Deposits or withdraws a skin. The cosmetic request handler serializes and deduplicates calls.</summary>
    public static async ValueTask<string> ExecuteAsync(Player player, byte source, bool deposit)
    {
        var character = player.SelectedCharacter!;
        var inventory = player.Inventory!;
        var storage = character.SkinInventory;
        var item = deposit ? inventory.GetItem(source) : storage?.Items.SingleOrDefault(i => i.ItemSlot == source);
        if (item is null || (deposit && (source <= MUnique.OpenMU.DataModel.InventoryConstants.LastEquippableItemSlotIndex
            || (CosmeticArmorRules.GetSlot(item.Definition) is null && !FenrirMountRules.IsCustom(item.Definition)))))
        {
            return "Select a skin from your inventory.";
        }

        if (deposit && storage?.Items.Count >= Capacity)
        {
            return "Skin inventory is full.";
        }

        storage ??= character.SkinInventory = player.PersistenceContext.CreateNew<ItemStorage>();
        var originalSlot = item.ItemSlot;
        var moved = false;
        var committed = false;
        try
        {
            if (deposit)
            {
                var destination = Enumerable.Range(0, Capacity).First(s => storage.Items.All(i => i.ItemSlot != s));
                await inventory.RemoveItemAsync(item).ConfigureAwait(false);
                item.ItemSlot = (byte)destination;
                storage.Items.Add(item);
                moved = true;
            }
            else
            {
                storage.Items.Remove(item);
                if (!await inventory.AddItemAsync(item).ConfigureAwait(false))
                {
                    item.ItemSlot = originalSlot;
                    storage.Items.Add(item);
                    return "Make room in your inventory first.";
                }
                moved = true;
            }

            if (!await player.SaveProgressAsync().ConfigureAwait(false))
            {
                throw new InvalidOperationException("Saving skin inventory transfer failed.");
            }
            committed = true;
        }
        finally
        {
            if (moved && !committed)
            {
                if (deposit)
                {
                    storage.Items.Remove(item);
                    if (!await inventory.AddItemAsync(originalSlot, item).ConfigureAwait(false))
                        throw new InvalidOperationException("Could not restore the uncommitted skin deposit.");
                }
                else
                {
                    await inventory.RemoveItemAsync(item).ConfigureAwait(false);
                    item.ItemSlot = originalSlot;
                    storage.Items.Add(item);
                }
            }
            await player.InvokeViewPlugInAsync<IUpdateInventoryListPlugIn>(v => v.UpdateInventoryListAsync()).ConfigureAwait(false);
        }
        return deposit ? "Skin stored." : "Skin returned to inventory.";
    }
}
