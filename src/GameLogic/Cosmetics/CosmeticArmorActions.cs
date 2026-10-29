// <copyright file="CosmeticArmorActions.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Cosmetics;

using System.Runtime.CompilerServices;
using MUnique.OpenMU.GameLogic.Views.Inventory;
using MUnique.OpenMU.GameLogic.Views.World;

/// <summary>Moves owned skin items between inventory and cosmetic slots under the persistence lock.</summary>
public sealed class CosmeticArmorActions
{
    private readonly ConditionalWeakTable<Player, Session> _sessions = new();

    /// <summary>Executes refresh (0), equip (1), or unequip (2); retries never repeat a move.</summary>
    public ValueTask ExecuteAsync(Player player, uint request, byte operation, byte source, byte part)
        => player.RunPersistenceExclusiveAsync(() => this.ExecuteCoreAsync(player, request, operation, source, part));

    private static ValueTask ShowAsync(Player player, uint request, string status)
        => player.InvokeViewPlugInAsync<ICosmeticArmorView>(view => view.ShowAsync(request, status));

    private async ValueTask ExecuteCoreAsync(Player player, uint request, byte operation, byte source, byte part)
    {
        if (player.SelectedCharacter?.Inventory is not { } persisted || player.Inventory is not { } inventory)
        {
            return;
        }

        var session = this._sessions.GetValue(player, _ => new());
        if (request == 0 || request <= session.LastRequest)
        {
            await ShowAsync(player, request, "Request already processed. Refresh the skin page.").ConfigureAwait(false);
            return;
        }

        session.LastRequest = request;
        if (operation == 0)
        {
            await ShowAsync(player, request, "Ready").ConfigureAwait(false);
            return;
        }

        if (part >= CosmeticArmorRules.SlotCount || operation > 2 || player.IsTemplatePlayer
            || player.PlayerState.CurrentState != PlayerState.EnteredWorld || player.OpenedNpc is not null
            || player.TemporaryStorage?.Items.Any() == true || player.ShopStorage?.StoreOpen == true || player.IsCashShopOpen)
        {
            await ShowAsync(player, request, "Close other inventory operations before changing skins.").ConfigureAwait(false);
            return;
        }

        var skinSlot = (byte)(CosmeticArmorRules.FirstSlot + part);
        var item = operation == 1 ? inventory.GetItem(source) : persisted.Items.SingleOrDefault(i => i.ItemSlot == skinSlot);
        if (item is null || (operation == 1 && !CosmeticArmorRules.CanEquip(player, item.Definition))
            || (operation == 1 && !SkinBonuses.IsValid(item))
            || !CosmeticArmorRules.FitsSlot(item.Definition, skinSlot)
            || (operation == 1 && (source <= MUnique.OpenMU.DataModel.InventoryConstants.LastEquippableItemSlotIndex
                || persisted.Items.Any(i => i.ItemSlot == skinSlot)))
            || (item.ExpiresAtUtc is { } expires && expires <= DateTime.UtcNow))
        {
            await ShowAsync(player, request, "That skin cannot be equipped in this slot. Unequip the current skin first.").ConfigureAwait(false);
            return;
        }

        var originalSlot = item.ItemSlot;
        var committed = false;
        try
        {
            if (operation == 1)
            {
                await inventory.RemoveItemAsync(item).ConfigureAwait(false);
                item.ItemSlot = skinSlot;
                persisted.Items.Add(item);
            }
            else
            {
                persisted.Items.Remove(item);
                if (!await inventory.AddItemAsync(item).ConfigureAwait(false))
                {
                    persisted.Items.Add(item);
                    await ShowAsync(player, request, "Make room in your inventory first.").ConfigureAwait(false);
                    return;
                }
            }

            if (!await player.SaveProgressAsync().ConfigureAwait(false))
            {
                throw new InvalidOperationException("Saving cosmetic equipment failed.");
            }

            committed = true;
        }
        finally
        {
            if (!committed && item.ItemSlot != originalSlot)
            {
                if (operation == 1)
                {
                    persisted.Items.Remove(item);
                    if (!await inventory.AddItemAsync(originalSlot, item).ConfigureAwait(false))
                    {
                        throw new InvalidOperationException("Could not restore the uncommitted skin item.");
                    }
                }
                else
                {
                    await inventory.RemoveItemAsync(item).ConfigureAwait(false);
                    item.ItemSlot = originalSlot;
                    persisted.Items.Add(item);
                }
            }
        }

        (inventory as InventoryStorage)?.RefreshCosmeticPowerUps();
        player.OnAppearanceChanged();
        var slots = part == 1 ? Enumerable.Range(2, 5).Select(slot => (byte)slot) : [CosmeticArmorRules.GearSlot(part)];
        foreach (var slot in slots)
        {
            var changed = new TemporaryItem { ItemSlot = slot };
            await player.ForEachWorldObserverAsync<IAppearanceChangedPlugIn>(v => v.AppearanceChangedAsync(player, changed, true), false).ConfigureAwait(false);
        }
        await player.InvokeViewPlugInAsync<IUpdateInventoryListPlugIn>(v => v.UpdateInventoryListAsync()).ConfigureAwait(false);
        await ShowAsync(player, request, operation == 1 ? "Skin equipped." : "Skin returned to inventory.").ConfigureAwait(false);
    }

    private sealed class Session
    {
        public uint LastRequest { get; set; }
    }
}
