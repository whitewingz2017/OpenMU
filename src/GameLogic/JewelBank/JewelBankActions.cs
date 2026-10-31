// <copyright file="JewelBankActions.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.JewelBank;

using System.Runtime.CompilerServices;
using System.Text.Json;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.GameLogic.Views.Inventory;

/// <summary>Account-wide, server-authoritative jewel banking.</summary>
public sealed class JewelBankActions
{
    private readonly ConditionalWeakTable<Player, Session> _sessions = new();

    /// <summary>Executes a request under the same lock as inventory actions and progress saves.</summary>
    /// <param name="player">The player.</param>
    /// <param name="configuration">The bank configuration.</param>
    /// <param name="request">A monotonically increasing request identifier.</param>
    /// <param name="operation">Zero refreshes, one deposits all, two withdraws.</param>
    /// <param name="itemId">The configured item identifier.</param>
    /// <param name="amount">The withdrawal count, or zero for all that fits.</param>
    public ValueTask ExecuteAsync(Player player, JewelBankConfiguration configuration, uint request, byte operation, int itemId, uint amount)
        => player.RunPersistenceExclusiveAsync(() => this.ExecuteCoreAsync(player, configuration, request, operation, itemId, amount));

    private static Dictionary<string, int> ReadLedger(string json)
        => JsonSerializer.Deserialize<Dictionary<string, int>>(string.IsNullOrWhiteSpace(json) ? "{}" : json) ?? new();

    private static string Key(int itemId) => $"{itemId}:0";

    private static bool Plain(Item item) => item.ItemOptions.Count == 0 && item.ItemSetGroups.Count == 0
        && !item.HasSkill && item.SocketCount == 0 && !item.StorePrice.HasValue;

    private static IEnumerable<(Item Item, int Count)> DepositItems(Player player, ItemDefinition definition)
    {
        var mixed = player.GameContext.Configuration.JewelMixes.FirstOrDefault(m => m.SingleJewel == definition)?.MixedJewel;
        foreach (var item in player.Inventory!.Items)
        {
            if (item.ItemSlot <= MUnique.OpenMU.DataModel.InventoryConstants.LastEquippableItemSlotIndex || !Plain(item))
            {
                continue;
            }

            if (item.Definition == definition && item.Level == 0)
            {
                yield return (item, 1);
            }
            else if (mixed != null && item.Definition == mixed && item.Level <= 2)
            {
                yield return (item, (item.Level + 1) * 10);
            }
        }
    }

    private static async ValueTask ShowAsync(Player player, JewelBankConfiguration configuration, uint request, string status)
    {
        if (player.Account == null || player.Inventory == null)
        {
            return;
        }

        var ledger = ReadLedger(player.Account.JewelBankBalances);
        var rows = new List<JewelBankRow>();
        foreach (var id in configuration.Items.Distinct().Take(32))
        {
            var definition = player.GameContext.Configuration.Items.FirstOrDefault(d => (d.Group * 512) + d.Number == id);
            if (definition == null)
            {
                continue;
            }

            ledger.TryGetValue(Key(id), out var balance);
            rows.Add(new(id, definition.Name.ValueInNeutralLanguage, balance, DepositItems(player, definition).Sum(i => i.Count), configuration.MaximumBalance));
        }

        await player.InvokeViewPlugInAsync<IJewelBankView>(p => p.ShowAsync(request, status, rows)).ConfigureAwait(false);
    }

    private async ValueTask ExecuteCoreAsync(Player player, JewelBankConfiguration configuration, uint request, byte operation, int itemId, uint amount)
    {
        if (player.Account == null || player.Inventory == null || player.SelectedCharacter == null)
        {
            return;
        }

        var session = this._sessions.GetValue(player, _ => new Session());
        if (request == 0 || request <= session.LastRequest)
        {
            await ShowAsync(player, configuration, request, "Request already processed. Refresh the bank.").ConfigureAwait(false);
            return;
        }

        session.LastRequest = request;
        if (!configuration.Enabled || configuration.MaximumBalance <= 0)
        {
            await ShowAsync(player, configuration, request, "Jewel Bank is disabled.").ConfigureAwait(false);
            return;
        }

        if (operation == 0)
        {
            await ShowAsync(player, configuration, request, "Ready").ConfigureAwait(false);
            return;
        }

        if (player.IsTemplatePlayer || player.PlayerState.CurrentState != PlayerState.EnteredWorld || player.OpenedNpc != null
            || player.TemporaryStorage?.Items.Any() == true || player.ShopStorage?.StoreOpen == true || player.IsCashShopOpen)
        {
            await ShowAsync(player, configuration, request, "Close trade, shop, warehouse and mixing windows first.").ConfigureAwait(false);
            return;
        }

        var definition = player.GameContext.Configuration.Items.FirstOrDefault(d => (d.Group * 512) + d.Number == itemId);
        if (operation > 2 || !configuration.Items.Contains(itemId) || definition == null || definition.Width != 1 || definition.Height != 1
            || (amount != 0 && amount != 1 && (amount > 50 || amount % 10 != 0)))
        {
            await ShowAsync(player, configuration, request, "Invalid bank request.").ConfigureAwait(false);
            return;
        }

        var original = player.Account.JewelBankBalances;
        var ledger = ReadLedger(original);
        ledger.TryGetValue(Key(itemId), out var balance);
        if (balance < 0)
        {
            throw new InvalidOperationException("Invalid persisted jewel bank balance.");
        }

        var removed = new List<(Item Item, byte Slot)>();
        var added = new List<Item>();
        var committed = false;
        var transferred = 0;
        try
        {
            if (operation == 1)
            {
                var items = DepositItems(player, definition).ToArray();
                if (amount > 0)
                {
                    // Select whole singles/bundles for the exact count; never destroy the remainder of a bundle.
                    var plans = new List<(Item Item, int Count)>?[amount + 1];
                    plans[0] = [];
                    foreach (var entry in items)
                        for (var count = (int)amount; count >= entry.Count; count--)
                            if (plans[count] is null && plans[count - entry.Count] is { } previous)
                                plans[count] = [.. previous, entry];
                    if (plans[amount] is not { } selected || (long)balance + amount > configuration.MaximumBalance)
                    {
                        await ShowAsync(player, configuration, request, "Not enough matching inventory jewels for that exact quantity, or bank limit reached. Unpack bundles if needed.").ConfigureAwait(false);
                        return;
                    }
                    items = selected.ToArray();
                }
                foreach (var entry in items)
                {
                    if ((long)balance + transferred + entry.Count > configuration.MaximumBalance)
                    {
                        continue;
                    }

                    removed.Add((entry.Item, entry.Item.ItemSlot));
                    await player.Inventory.RemoveItemAsync(entry.Item).ConfigureAwait(false);
                    await player.PersistenceContext.DeleteAsync(entry.Item).ConfigureAwait(false);
                    transferred += entry.Count;
                }

                ledger[Key(itemId)] = checked(balance + transferred);
            }
            else
            {
                var wanted = amount == 0 ? balance : (int)amount;
                var slots = player.Inventory.FreeSlots.Take(Math.Min(wanted, balance)).ToArray();
                if (amount != 0 && (balance < wanted || slots.Length < wanted))
                {
                    await ShowAsync(player, configuration, request, "Not enough jewels or inventory space.").ConfigureAwait(false);
                    return;
                }

                foreach (var slot in slots)
                {
                    var item = player.PersistenceContext.CreateNew<Item>();
                    item.Definition = definition;
                    item.Durability = definition.Durability;
                    added.Add(item);
                    if (!await player.Inventory.AddItemAsync(slot, item).ConfigureAwait(false))
                    {
                        throw new InvalidOperationException("Reserved jewel bank inventory slot became unavailable.");
                    }
                }

                transferred = added.Count;
                ledger[Key(itemId)] = balance - transferred;
            }

            if (transferred > 0)
            {
                player.Account.JewelBankBalances = JsonSerializer.Serialize(ledger);
                if (!await player.SaveProgressAsync().ConfigureAwait(false))
                {
                    throw new InvalidOperationException("Saving jewel bank transaction failed.");
                }
            }

            committed = true;
        }
        finally
        {
            if (!committed)
            {
                player.Account.JewelBankBalances = original;
                foreach (var item in added)
                {
                    await player.Inventory.RemoveItemAsync(item).ConfigureAwait(false);
                    await player.PersistenceContext.DeleteAsync(item).ConfigureAwait(false);
                }

                foreach (var entry in removed)
                {
                    player.PersistenceContext.Attach(entry.Item);
                    if (!await player.Inventory.AddItemAsync(entry.Slot, entry.Item).ConfigureAwait(false))
                    {
                        throw new InvalidOperationException("Could not restore an uncommitted bank deposit.");
                    }
                }
            }
        }

        foreach (var entry in removed)
        {
            await player.InvokeViewPlugInAsync<IItemRemovedPlugIn>(p => p.RemoveItemAsync(entry.Slot)).ConfigureAwait(false);
        }

        foreach (var item in added)
        {
            await player.InvokeViewPlugInAsync<IItemAppearPlugIn>(p => p.ItemAppearAsync(item)).ConfigureAwait(false);
        }

        await ShowAsync(player, configuration, request, transferred == 0 ? "Nothing transferred. Check inventory space and bank limit." : $"{transferred} jewels {(operation == 1 ? "deposited" : "withdrawn")}.").ConfigureAwait(false);
    }

    private sealed class Session
    {
        public uint LastRequest { get; set; }
    }
}
