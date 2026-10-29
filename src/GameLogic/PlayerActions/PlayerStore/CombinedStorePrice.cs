// <copyright file="CombinedStorePrice.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions.PlayerStore;

using System.Text.Json;

/// <summary>Validates and persists combined prices independently of legacy single-currency listings.</summary>
public static class CombinedStorePrice
{
    /// <summary>Legacy price marker. Old clients cannot pay this currency.</summary>
    public const int Marker = 0xFFEF;

    /// <summary>Maximum number of distinct required currencies.</summary>
    public const int MaximumParts = 8;

    /// <summary>Reads a listing's required payments.</summary>
    /// <param name="item">The item.</param>
    public static StorePricePart[] Read(Item item) => string.IsNullOrEmpty(item.StorePricesJson)
        ? [] : JsonSerializer.Deserialize<StorePricePart[]>(item.StorePricesJson) ?? [];

    /// <summary>Rejects empty, duplicate, excessive, negative or unauthorized requirements.</summary>
    /// <param name="parts">The parts.</param>
    /// <param name="settings">The settings.</param>
    public static bool IsValid(StorePricePart[] parts, StoreCurrencySettings? settings)
        => parts.Length is > 0 and <= MaximumParts && parts.Select(p => p.Type).Distinct().Count() == parts.Length
            && parts.All(p => StoreCurrency.IsValid(p.Type, p.Amount, settings));

    /// <summary>Sets a combined price while the store is closed.</summary>
    /// <param name="player">The player.</param>
    /// <param name="slot">The slot.</param>
    /// <param name="parts">The parts.</param>
    public static async ValueTask SetAsync(Player player, byte slot, StorePricePart[] parts)
    {
        if (player.PlayerState.CurrentState != PlayerState.EnteredWorld) return;
        var item = player.SelectedCharacter?.Inventory?.Items.FirstOrDefault(i => i.ItemSlot == slot);
        var valid = player.Level >= 6 && slot >= InventoryConstants.EquippableSlotsCount
            && slot < InventoryConstants.FirstStoreItemSlotIndex + InventoryConstants.StoreSize
            && player.ShopStorage is { StoreOpen: false } && item is not null
            && IsValid(parts, StoreCurrencyFeaturePlugIn.GetSettings(player.GameContext));
        if (valid)
        {
            item!.StorePricesJson = JsonSerializer.Serialize(parts);
            item.StorePrice = 1;
            item.StorePriceItemType = Marker;
        }

        await player.InvokeViewPlugInAsync<Views.Inventory.IItemPriceSetResponsePlugIn>(p => p.ItemPriceSetResponseAsync(
            slot,
            valid ? Views.PlayerShop.ItemPriceResult.Success : Views.PlayerShop.ItemPriceResult.Failed)).ConfigureAwait(false);
    }
}
