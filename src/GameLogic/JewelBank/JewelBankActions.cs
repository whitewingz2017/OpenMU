// <copyright file="JewelBankActions.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.JewelBank;

using System.Runtime.CompilerServices;
using System.Text.Json;
using MUnique.OpenMU.DataModel;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.GameLogic.Views.Inventory;

/// <summary>Account-wide, server-authoritative jewel banking.</summary>
public sealed class JewelBankActions
{
    /// <summary>Deposits all configured jewel types from the entire bag.</summary>
    public const byte DepositAllOperation = 3;

    private readonly ConditionalWeakTable<Player, Session> _sessions = new();

    /// <summary>Executes a request under the same lock as inventory actions and progress saves.</summary>
    /// <param name="player">The player.</param>
    /// <param name="configuration">The bank configuration.</param>
    /// <param name="request">A monotonically increasing request identifier.</param>
    /// <param name="operation">Zero refreshes, one deposits a jewel type, two withdraws, and three deposits every type.</param>
    /// <param name="itemId">The configured item identifier.</param>
    /// <param name="amount">The withdrawal count, or zero for all that fits.</param>
    public ValueTask ExecuteAsync(Player player, JewelBankConfiguration configuration, uint request, byte operation, int itemId, uint amount)
        => player.RunPersistenceExclusiveAsync(() => this.ExecuteCoreAsync(player, configuration, request, operation, itemId, amount));

    private static Dictionary<string, int> ReadLedger(string json)
        => JsonSerializer.Deserialize<Dictionary<string, int>>(string.IsNullOrWhiteSpace(json) ? "{}" : json) ?? new();

    private static string Key(int itemId) => $"{itemId}:0";

    private static ItemDefinition? FindJewel(Player player, int id)
        => player.GameContext.Configuration.Items.FirstOrDefault(d => (d.Group * 512) + d.Number == id && d.Width == 1 && d.Height == 1);

    private static bool Plain(Item item) => item.ItemOptions.Count == 0 && item.ItemSetGroups.Count == 0
        && !item.HasSkill && item.SocketCount == 0 && !item.StorePrice.HasValue;

    private static IEnumerable<(Item Item, int Count)> DepositItems(Player player, ItemDefinition definition)
    {
        var mixed = player.GameContext.Configuration.JewelMixes.FirstOrDefault(m => m.SingleJewel == definition)?.MixedJewel;
        foreach (var item in player.Inventory!.Items)
        {
            if (item.ItemSlot <= InventoryConstants.LastEquippableItemSlotIndex || item.ItemSlot >= player.InventorySize || !Plain(item))
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

    private static int ReadBalance(IReadOnlyDictionary<string, int> ledger, int itemId)
    {
        var balance = ledger.GetValueOrDefault(Key(itemId));
        return balance >= 0 ? balance : throw new InvalidOperationException("Invalid persisted jewel bank balance.");
    }

    private static List<(Item Item, int Count, int Id)>? PlanDeposits(
        Player player,
        JewelBankConfiguration configuration,
        IReadOnlyDictionary<string, int> ledger,
        bool bulkDeposit,
        int itemId,
        uint amount)
    {
        var plan = new List<(Item, int, int)>();
        var reserved = new HashSet<Item>();
        IEnumerable<int> ids = bulkDeposit ? configuration.Items.Distinct().Take(32) : [itemId];
        foreach (var id in ids)
        {
            if (FindJewel(player, id) is not { } definition)
            {
                continue;
            }

            var capacity = configuration.MaximumBalance - ReadBalance(ledger, id);
            var items = DepositItems(player, definition).Where(entry => !reserved.Contains(entry.Item));
            if (amount != 0)
            {
                var exact = capacity >= amount ? SelectExactDeposit(items, (int)amount) : null;
                if (exact is null)
                {
                    return null;
                }

                items = exact;
            }

            foreach (var entry in items)
            {
                if (entry.Count > capacity || !reserved.Add(entry.Item))
                {
                    continue;
                }

                capacity -= entry.Count;
                plan.Add((entry.Item, entry.Count, id));
            }
        }

        return plan;
    }

    private static List<(Item Item, int Count)>? SelectExactDeposit(IEnumerable<(Item Item, int Count)> items, int amount)
    {
        // Select whole singles or bundles for the exact count, keeping remainders in the bag.
        var plans = new List<(Item Item, int Count)>?[amount + 1];
        plans[0] = [];
        foreach (var entry in items)
        {
            for (var count = amount; count >= entry.Count; count--)
            {
                if (plans[count] is null && plans[count - entry.Count] is { } previous)
                {
                    plans[count] = [.. previous, entry];
                }
            }
        }

        return plans[amount];
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

        var bulkDeposit = operation == DepositAllOperation;
        var definition = bulkDeposit ? null : FindJewel(player, itemId);
        if (operation > DepositAllOperation
            || (bulkDeposit ? itemId != 0 || amount != 0 : !configuration.Items.Contains(itemId) || definition is null)
            || (amount != 0 && amount != 1 && (amount > 50 || amount % 10 != 0)))
        {
            await ShowAsync(player, configuration, request, "Invalid bank request.").ConfigureAwait(false);
            return;
        }

        var original = player.Account.JewelBankBalances;
        var ledger = ReadLedger(original);
        List<(Item Item, int Count, int Id)>? deposits = operation == 2 ? [] : PlanDeposits(player, configuration, ledger, bulkDeposit, itemId, amount);
        if (deposits is null)
        {
            await ShowAsync(player, configuration, request, "Not enough matching inventory jewels for that exact quantity, or bank limit reached. Unpack bundles if needed.").ConfigureAwait(false);
            return;
        }

        var removed = new List<(Item Item, byte Slot)>();
        var added = new List<Item>();
        var committed = false;
        var transferred = 0;
        try
        {
            if (operation != 2)
            {
                foreach (var (item, count, id) in deposits)
                {
                    removed.Add((item, item.ItemSlot));
                    await player.Inventory.RemoveItemAsync(item).ConfigureAwait(false);
                    await player.PersistenceContext.DeleteAsync(item).ConfigureAwait(false);
                    ledger[Key(id)] = checked(ReadBalance(ledger, id) + count);
                    transferred += count;
                }
            }
            else
            {
                var balance = ReadBalance(ledger, itemId);
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
                    item.Durability = definition!.Durability;
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

        await ShowAsync(player, configuration, request, transferred == 0 ? "Nothing transferred. Check inventory space and bank limit." : $"{transferred} jewels {(operation == 2 ? "withdrawn" : "deposited")}.").ConfigureAwait(false);
    }

    private sealed class Session
    {
        public uint LastRequest { get; set; }
    }
}
