// <copyright file="MuunActions.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Muun;

using System.Runtime.CompilerServices;
using MUnique.OpenMU.DataModel;
using MUnique.OpenMU.GameLogic.Views.Inventory;

/// <summary>Server-owned transfers, equipment, duplicate feeding, evolution and life restoration.</summary>
public sealed class MuunActions
{
    private readonly ConditionalWeakTable<Player, Session> _sessions = new();
    /// <summary>Processes 0 refresh, 1 deposit, 2 withdraw, 3 equip, 4 unequip, 5 level, 6 evolve, 7 repair.</summary>
    public ValueTask ExecuteAsync(Player player, uint request, byte operation, byte source, byte target)
        => player.RunPersistenceExclusiveAsync(() => this.ExecuteCoreAsync(player, request, operation, source, target));

    private async ValueTask ExecuteCoreAsync(Player player, uint request, byte operation, byte source, byte target)
    {
        if (player.SelectedCharacter is not { } character || player.Inventory is not { } inventory
            || player.PlayerState.CurrentState != PlayerState.EnteredWorld) return;
        var session = this._sessions.GetValue(player, _ => new());
        var status = "Ready";
        if (!ReferenceEquals(session.Character, character)) { session.LastRequest = 0; session.Character = character; }
        if (request == 0 || request <= session.LastRequest) status = "MuunRetry";
        else
        {
            session.LastRequest = request;
            var config = MuunFeaturePlugIn.Settings(player.GameContext);
            if (config is null) status = "MuunDisabled";
            else if (operation > 7 || config.Capacity is < 3 or > 120) status = "MuunInvalid";
            else if (operation != 0)
            {
                if (player.IsTemplatePlayer || player.OpenedNpc is not null || player.TemporaryStorage?.Items.Any() == true
                    || player.ShopStorage?.StoreOpen == true || player.IsCashShopOpen) status = "MuunCloseWindows";
                else status = await ChangeAsync(player, config, operation, source, target).ConfigureAwait(false);
            }
        }
        MuunBonuses.Refresh(player);
        await player.InvokeViewPlugInAsync<IUpdateInventoryListPlugIn>(v => v.UpdateInventoryListAsync()).ConfigureAwait(false);
        await player.InvokeViewPlugInAsync<IMuunView>(v => v.ShowAsync(request, status)).ConfigureAwait(false);
        await player.ForEachWorldObserverAsync<IMuunView>(v => v.AppearanceAsync(player), true).ConfigureAwait(false);
    }

    private static async ValueTask<string> ChangeAsync(Player player, MuunConfiguration config, byte operation, byte source, byte target)
    {
        var character = player.SelectedCharacter!;
        var inventory = player.Inventory!;
        var storage = character.MuunInventory;
        var item = operation == 1 ? inventory.GetItem(source) : storage?.Items.SingleOrDefault(i => i.ItemSlot == source);
        if (item is null || MuunRules.Find(config, MuunRules.Type(item)) is not { } rule || item.Level > rule.MaximumLevel
            || (operation != 2 && item.ExpiresAtUtc is { } end && end <= DateTime.UtcNow)) return "MuunInvalid";
        if (operation == 1 && source <= DataModel.InventoryConstants.LastEquippableItemSlotIndex) return "MuunInvalid";
        storage ??= character.MuunInventory = player.PersistenceContext.CreateNew<ItemStorage>();
        var oldSlot = item.ItemSlot; var oldLevel = item.Level; var oldDefinition = item.Definition; var oldLife = item.Durability;
        Item? consumed = null; byte consumedSlot = 0; bool consumedFromBag = false; double consumedLife = 0; bool removedMaterial = false;
        var moved = false; var committed = false;
        try
        {
            if (operation is 1 or 4)
            {
                if (operation == 4 && source >= MuunRules.EquipmentCount) return "MuunInvalid";
                var free = Enumerable.Range(MuunRules.EquipmentCount, config.Capacity - MuunRules.EquipmentCount)
                    .FirstOrDefault(s => storage.Items.All(i => i.ItemSlot != s), -1);
                if (free < 0) return "MuunFull";
                if (operation == 1) await inventory.RemoveItemAsync(item).ConfigureAwait(false);
                item.ItemSlot = (byte)free;
                if (operation == 1) storage.Items.Add(item);
                moved = true;
            }
            else if (operation == 2)
            {
                storage.Items.Remove(item);
                if (!await inventory.AddItemAsync(item).ConfigureAwait(false)) { item.ItemSlot = oldSlot; storage.Items.Add(item); return "MuunBagFull"; }
                moved = true;
            }
            else if (operation == 3)
            {
                if (source < MuunRules.EquipmentCount || target >= MuunRules.EquipmentCount || storage.Items.Any(i => i.ItemSlot == target)
                    || storage.Items.Any(i => i.ItemSlot < MuunRules.EquipmentCount && i.Definition == item.Definition)) return "MuunEquipInvalid";
                item.ItemSlot = target; moved = true;
            }
            else
            {
                consumedFromBag = operation != 5;
                consumed = consumedFromBag ? inventory.GetItem(target) : storage.Items.SingleOrDefault(i => i.ItemSlot == target);
                if (consumed is null || ReferenceEquals(item, consumed) || consumed.ExpiresAtUtc is not null
                    || (consumedFromBag ? target <= DataModel.InventoryConstants.LastEquippableItemSlotIndex : target < MuunRules.EquipmentCount)) return "MuunInvalid";
                if (operation == 5)
                {
                    if (item.Level >= rule.MaximumLevel || consumed.Definition != item.Definition || consumed.Level != 0) return "MuunFeedInvalid";
                    item.Level++;
                }
                else if (operation == 6)
                {
                    var evolved = player.GameContext.Configuration.Items.SingleOrDefault(d => (d.Group * 512) + d.Number == rule.EvolutionItemType);
                    if (item.Level != rule.MaximumLevel || MuunRules.Type(consumed) != rule.EvolutionStoneType || evolved is null
                        || MuunRules.Find(config, rule.EvolutionItemType) is null
                        || (source < MuunRules.EquipmentCount && storage.Items.Any(i => i != item && i.ItemSlot < MuunRules.EquipmentCount && i.Definition == evolved))) return "MuunEvolutionInvalid";
                    item.Definition = evolved; item.Level = 0; item.Durability = evolved.Durability;
                }
                else
                {
                    if (MuunRules.Type(consumed) != rule.RepairItemType || item.Durability >= item.Definition!.Durability) return "MuunRepairInvalid";
                    item.Durability = item.Definition!.Durability;
                }
                consumedSlot = consumed.ItemSlot; consumedLife = consumed.Durability;
                if (consumedFromBag && consumed.IsStackable() && consumed.Durability > 1) consumed.Durability--;
                else
                {
                    if (consumedFromBag) await inventory.RemoveItemAsync(consumed).ConfigureAwait(false);
                    else storage.Items.Remove(consumed);
                    removedMaterial = true;
                    await player.PersistenceContext.DeleteAsync(consumed).ConfigureAwait(false);
                }
            }
            if (!await player.SaveProgressAsync().ConfigureAwait(false)) throw new InvalidOperationException("Muun save failed.");
            committed = true;
            return "MuunChanged";
        }
        catch (Exception exception)
        {
            player.Logger.LogError(exception, "Muun transaction failed for {Player}.", player);
            return "MuunSaveFailed";
        }
        finally
        {
            if (!committed)
            {
                item.Level = oldLevel; item.Definition = oldDefinition; item.Durability = oldLife;
                if (moved)
                {
                    if (operation == 2) { await inventory.RemoveItemAsync(item).ConfigureAwait(false); item.ItemSlot = oldSlot; storage.Items.Add(item); }
                    else if (operation == 1)
                    {
                        storage.Items.Remove(item);
                        if (!await inventory.AddItemAsync(oldSlot, item).ConfigureAwait(false)) throw new InvalidOperationException("Muun deposit rollback failed.");
                    }
                    else item.ItemSlot = oldSlot;
                }
                if (consumed is not null && consumedSlot != 0)
                {
                    consumed.Durability = consumedLife;
                    if (removedMaterial)
                    {
                    player.PersistenceContext.Attach(consumed); consumed.ItemSlot = consumedSlot;
                    if (consumedFromBag)
                    {
                        if (!await inventory.AddItemAsync(consumedSlot, consumed).ConfigureAwait(false)) throw new InvalidOperationException("Muun material rollback failed.");
                    }
                    else storage.Items.Add(consumed);
                    }
                }
            }
        }
    }
    private sealed class Session
    {
        public uint LastRequest { get; set; }
        public Character? Character { get; set; }
    }
}
