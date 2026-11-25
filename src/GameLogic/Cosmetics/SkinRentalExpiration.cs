// <copyright file="SkinRentalExpiration.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Cosmetics;

using MUnique.OpenMU.GameLogic.Views.Inventory;
using MUnique.OpenMU.GameLogic.Views.World;

/// <summary>Expires rented skins in cosmetic equipment and Skin Inventory under the caller's persistence lock.</summary>
public static class SkinRentalExpiration
{
    /// <summary>Removes elapsed rentals and refreshes their appearance and attributes.</summary>
    /// <param name="player">The rental owner.</param>
    /// <param name="utcNow">The current UTC time.</param>
    /// <returns>Whether any stored or equipped skin was removed.</returns>
    public static async ValueTask<bool> RemoveExpiredAsync(Player player, DateTime utcNow)
    {
        var equipped = player.SelectedCharacter?.Inventory?.Items
            .Where(item => item.ItemSlot >= CosmeticArmorRules.FirstSlot
                && item.ItemSlot < CosmeticArmorRules.FirstSlot + CosmeticArmorRules.SlotCount
                && item.ExpiresAtUtc <= utcNow).ToArray() ?? [];
        var stored = player.SelectedCharacter?.SkinInventory?.Items.Where(item => item.ExpiresAtUtc <= utcNow).ToArray() ?? [];
        if (equipped.Length == 0 && stored.Length == 0)
        {
            return false;
        }

        foreach (var item in equipped.Concat(stored))
        {
            player.SelectedCharacter!.Inventory!.Items.Remove(item);
            player.SelectedCharacter.SkinInventory?.Items.Remove(item);
            player.BackupInventory?.Items.Remove(item);
            await player.PersistenceContext.DeleteAsync(item).ConfigureAwait(false);
            ItemEventRecorder.Record(player, item, "DESTROY", "Skin rental expiry", "Rental lifetime elapsed");
        }

        await RefreshEquippedAsync(player, equipped).ConfigureAwait(false);
        await player.InvokeViewPlugInAsync<ICosmeticArmorView>(view => view.ShowAsync(0, "Skin rental expired.")).ConfigureAwait(false);
        return true;
    }

    private static async ValueTask RefreshEquippedAsync(Player player, Item[] expired)
    {
        if (expired.Length == 0)
        {
            return;
        }

        (player.Inventory as InventoryStorage)?.RefreshCosmeticPowerUps();
        player.OnAppearanceChanged();
        var slots = expired.SelectMany(item => item.ItemSlot == CosmeticArmorRules.FirstSlot + 1
            ? Enumerable.Range(2, 5).Select(slot => (byte)slot)
            : [CosmeticArmorRules.GearSlot(item.ItemSlot - CosmeticArmorRules.FirstSlot)]).Distinct();
        foreach (var slot in slots)
        {
            var changed = new TemporaryItem { ItemSlot = slot };
            await player.ForEachWorldObserverAsync<IAppearanceChangedPlugIn>(view => view.AppearanceChangedAsync(player, changed, true), false).ConfigureAwait(false);
        }
    }
}
