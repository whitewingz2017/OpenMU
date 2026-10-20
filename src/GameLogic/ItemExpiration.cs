// <copyright file="ItemExpiration.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic;

using MUnique.OpenMU.GameLogic.Views.Inventory;
using MUnique.OpenMU.GameLogic.Views.Vault;

/// <summary>
/// Removes elapsed item instances through their storage so equipment power-ups are detached.
/// </summary>
public static class ItemExpiration
{
    /// <summary>
    /// Removes expired items from the player's loaded inventory and vault under its persistence lock.
    /// A vault which isn't loaded is checked when it is opened; UTC deadlines don't pause offline.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="utcNow">The current UTC time.</param>
    public static async ValueTask RemoveExpiredAsync(Player player, DateTime utcNow)
    {
        await player.RunPersistenceExclusiveAsync(async () =>
        {
            var changed = await CashShop.PurchasedEffectDelivery.SynchronizeAsync(player, utcNow).ConfigureAwait(false);
            if (player.Inventory is { } inventory)
            {
                foreach (var item in inventory.Items.Where(i => i.ExpiresAtUtc <= utcNow).ToList())
                {
                    byte slot = item.ItemSlot;
                    await inventory.RemoveItemAsync(item).ConfigureAwait(false);
                    player.BackupInventory?.Items.Remove(item);
                    await player.PersistenceContext.DeleteAsync(item).ConfigureAwait(false);
                    await player.InvokeViewPlugInAsync<IItemRemovedPlugIn>(p => p.RemoveItemAsync(slot)).ConfigureAwait(false);
                    changed = true;
                }
            }

            if (player.Vault is { } vault)
            {
                var expired = vault.Items.Where(i => i.ExpiresAtUtc <= utcNow).ToList();
                foreach (var item in expired)
                {
                    await vault.RemoveItemAsync(item).ConfigureAwait(false);
                    await player.PersistenceContext.DeleteAsync(item).ConfigureAwait(false);
                    changed = true;
                }

                if (expired.Count > 0)
                {
                    await player.InvokeViewPlugInAsync<IShowVaultPlugIn>(p => p.ShowVaultAsync()).ConfigureAwait(false);
                }
            }

            if (changed)
            {
                await player.SaveProgressAsync().ConfigureAwait(false);
            }
        }).ConfigureAwait(false);
    }
}
