// <copyright file="ExcellentCombinationActions.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions.Items;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.GameLogic.Views.Inventory;

/// <summary>Combines excellent options without replacing the primary item or its other metadata.</summary>
public sealed class ExcellentCombinationActions
{
    public const int Cost = 1_000_000;
    private readonly ConditionalWeakTable<Player, Session> _sessions = new();
    private sealed class Session
    {
        public uint LastRequest;
        public Item? Primary;
        public Item? Secondary;
        public ItemOptionLink[] Options = [];
        public DateTime QuotedAt;
        public (IncreasableItemOption? Option, int Level)[] Values = [];
    }
    public async ValueTask ExecuteAsync(Player player, uint request, byte operation, byte primarySlot, byte secondarySlot)
    {
        try
        {
            await player.RunPersistenceExclusiveAsync(() => this.ExecuteCoreAsync(player, request, operation, primarySlot, secondarySlot)).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            player.Logger.LogError(exception, "Excellent combination failed for request {RequestId}", request);
            await Reply(player, request, "Combination request failed. Check inventory before retrying.").ConfigureAwait(false);
        }
    }

    private static ItemOptionLink[] Excellent(Item item)
        => item.ItemOptions.Where(o => o.ItemOption?.OptionType == ItemOptionTypes.Excellent).ToArray();

    public static string? Validate(Item? primary, Item? secondary)
    {
        if (primary?.Definition is not { } definition || secondary?.Definition != definition || ReferenceEquals(primary, secondary))
            return "Select two different items of the same exact equipment type.";
        if (definition.ItemSlot?.ItemSlots.Any(s => s is >= 0 and <= 11) != true)
            return "Only Excellent equipment can be combined.";
        var options = Excellent(primary);
        var material = Excellent(secondary);
        if (options.Length is < 1 or > 2 || material.Length != 1)
            return "Primary needs one or two Excellent options; Secondary needs exactly one.";
        var supported = definition.PossibleItemOptions.SelectMany(g => g.PossibleOptions).Where(o => o.OptionType == ItemOptionTypes.Excellent).ToArray();
        if (options.Concat(material).Any(o => o.ItemOption!.Number is < 1 or > 6 || !supported.Contains(o.ItemOption)) || options.Select(o => o.ItemOption).Distinct().Count() != options.Length)
            return "The item contains unsupported Excellent options.";
        if (options.Any(o => o.ItemOption == material[0].ItemOption)) return "That Excellent option is already on the Primary item.";
        if (primary.ExpiresAtUtc is not null || secondary.ExpiresAtUtc is not null || primary.StorePrice.HasValue || secondary.StorePrice.HasValue)
            return "Timed or listed items cannot be combined.";
        return null;
    }
    private static ValueTask Reply(Player p, uint request, string status, bool valid = false, bool success = false, Item? result = null)
        => p.InvokeViewPlugInAsync<IExcellentCombinationView>(v => v.ShowAsync(request, status, valid, success, Cost, result));

    private async ValueTask ExecuteCoreAsync(Player player, uint request, byte operation, byte primarySlot, byte secondarySlot)
    {
        var session = this._sessions.GetValue(player, _ => new());
        if (request == 0 || request <= session.LastRequest) { await Reply(player, request, "Request already processed. Select the items again.").ConfigureAwait(false); return; }
        session.LastRequest = request;
        if (player.Inventory is not { } inventory || player.SelectedCharacter?.Inventory is not { } storage
            || player.IsTemplatePlayer || player.PlayerState.CurrentState != PlayerState.EnteredWorld
            || player.OpenedNpc is not null || player.TemporaryStorage?.Items.Any() == true || player.ShopStorage?.StoreOpen == true || player.IsCashShopOpen
            || operation > 1 || primarySlot < DataModel.InventoryConstants.EquippableSlotsCount || secondarySlot < DataModel.InventoryConstants.EquippableSlotsCount)
        { session.Primary = null; await Reply(player, request, "Close other inventory operations before combining.").ConfigureAwait(false); return; }
        var primary = inventory.GetItem(primarySlot);
        var secondary = inventory.GetItem(secondarySlot);
        if (Validate(primary, secondary) is { } error) { session.Primary = null; await Reply(player, request, error).ConfigureAwait(false); return; }
        if (storage.Money < Cost) { session.Primary = null; await Reply(player, request, "Not enough Zen.").ConfigureAwait(false); return; }
        var links = Excellent(primary!).Concat(Excellent(secondary!)).ToArray();
        if (operation == 0)
        {
            session.Primary = primary; session.Secondary = secondary; session.Options = links; session.Values = links.Select(l => (l.ItemOption, l.Level)).ToArray(); session.QuotedAt = DateTime.UtcNow;
            await Reply(player, request, "Ready. Excellent options may change.", true).ConfigureAwait(false); return;
        }
        if (session.Primary != primary || session.Secondary != secondary || (!session.Options.SequenceEqual(links) || !session.Values.SequenceEqual(links.Select(l => (l.ItemOption, l.Level))))
            || DateTime.UtcNow - session.QuotedAt > TimeSpan.FromSeconds(30))
        { session.Primary = null; await Reply(player, request, "Items changed or quote expired. Select the items again.").ConfigureAwait(false); return; }
        session.Primary = null;
        var originalMoney = storage.Money;
        var primaryOptions = Excellent(primary!);
        var replaced = primaryOptions.Length == 2 ? primaryOptions[System.Security.Cryptography.RandomNumberGenerator.GetInt32(2)] : null;
        var added = player.PersistenceContext.CreateNew<ItemOptionLink>();
        added.ItemOption = links[^1].ItemOption; added.Level = links[^1].Level;
        var committed = false;
        try
        {
            if (replaced is not null) primary!.ItemOptions.Remove(replaced);
            primary!.ItemOptions.Add(added);
            await inventory.RemoveItemAsync(secondary!).ConfigureAwait(false);
            await player.PersistenceContext.DeleteAsync(secondary!).ConfigureAwait(false);
            if (replaced is not null) await player.PersistenceContext.DeleteAsync(replaced).ConfigureAwait(false);
            storage.Money -= Cost;
            if (!await player.SaveProgressAsync().ConfigureAwait(false)) throw new InvalidOperationException("Saving Excellent combination failed.");
            committed = true;
        }
        finally
        {
            if (!committed)
            {
                storage.Money = originalMoney;
                primary!.ItemOptions.Remove(added);
                await player.PersistenceContext.DeleteAsync(added).ConfigureAwait(false);
                if (replaced is not null) { player.PersistenceContext.Attach(replaced); primary.ItemOptions.Add(replaced); }
                player.PersistenceContext.Attach(secondary!);
                if (!inventory.Items.Contains(secondary!) && !await inventory.AddItemAsync(secondarySlot, secondary!).ConfigureAwait(false))
                    throw new InvalidOperationException("Could not restore combination material.");
            }
            await player.InvokeViewPlugInAsync<IUpdateInventoryListPlugIn>(v => v.UpdateInventoryListAsync()).ConfigureAwait(false);
            await player.InvokeViewPlugInAsync<IUpdateMoneyPlugIn>(v => v.UpdateMoneyAsync()).ConfigureAwait(false);
        }
        await Reply(player, request, "Combination successful.", success: true, result: primary).ConfigureAwait(false);
    }
}
