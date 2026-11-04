// <copyright file="ExcellentCombinationActions.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions.Items;

using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using MUnique.OpenMU.DataModel;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.GameLogic.Views.Inventory;

/// <summary>Combines excellent options and commits material consumption for either roll outcome.</summary>
public sealed class ExcellentCombinationActions
{
    private readonly ConditionalWeakTable<Player, Session> _sessions = new();
    private readonly IRandomizer _randomizer;

    /// <summary>Initializes a new instance of the <see cref="ExcellentCombinationActions"/> class.</summary>
    public ExcellentCombinationActions()
        : this(Rand.GetRandomizer())
    {
    }

    /// <summary>Initializes a new instance of the <see cref="ExcellentCombinationActions"/> class.</summary>
    /// <param name="randomizer">The server randomizer.</param>
    internal ExcellentCombinationActions(IRandomizer randomizer) => this._randomizer = randomizer;

    /// <summary>Validates two items without changing them.</summary>
    /// <param name="primary">Main gear.</param>
    /// <param name="secondary">Donor gear.</param>
    /// <returns>A rejection message, or null.</returns>
    public static string? Validate(Item? primary, Item? secondary)
    {
        if (primary?.Definition is not { } definition || secondary?.Definition != definition || ReferenceEquals(primary, secondary))
        {
            return ExcellentCombinationResources.Message("DifferentItems");
        }

        if (definition.ItemSlot?.ItemSlots.Any(s => s is >= 0 and <= 11) != true)
        {
            return ExcellentCombinationResources.Message("EquipmentOnly");
        }

        if (definition.MaximumSockets > 0 || primary.SocketCount > 0 || secondary.SocketCount > 0
            || primary.ItemSetGroups.Concat(secondary.ItemSetGroups).Any(g => g.AncientSetDiscriminator != 0))
        {
            return ExcellentCombinationResources.Message("AncientSocket");
        }

        var options = Excellent(primary);
        var material = Excellent(secondary);
        if (options.Length is < 1 or > 2 || material.Length != 1)
        {
            return ExcellentCombinationResources.Message("OptionCount");
        }

        var supported = definition.PossibleItemOptions.SelectMany(g => g.PossibleOptions).Where(o => o.OptionType == ItemOptionTypes.Excellent).ToArray();
        if (options.Concat(material).Any(o => o.ItemOption!.Number is < 1 or > 6 || !supported.Contains(o.ItemOption)) || options.Select(o => o.ItemOption).Distinct().Count() != options.Length)
        {
            return ExcellentCombinationResources.Message("Unsupported");
        }

        if (options.Any(o => o.ItemOption == material[0].ItemOption))
        {
            return ExcellentCombinationResources.Message("Duplicate");
        }

        if (primary.ExpiresAtUtc is not null || secondary.ExpiresAtUtc is not null || primary.StorePrice.HasValue || secondary.StorePrice.HasValue)
        {
            return ExcellentCombinationResources.Message("TimedListed");
        }

        return null;
    }

    /// <summary>Quotes (0), confirms (1), or requests current settings and core count (2).</summary>
    /// <param name="player">The player.</param>
    /// <param name="request">The monotonic request identifier.</param>
    /// <param name="operation">The operation.</param>
    /// <param name="primarySlot">Main gear inventory slot.</param>
    /// <param name="secondarySlot">Donor inventory slot.</param>
    /// <param name="coreCount">The chosen number of cores.</param>
    /// <returns>The asynchronous operation.</returns>
    public async ValueTask ExecuteAsync(Player player, uint request, byte operation, byte primarySlot, byte secondarySlot, byte coreCount = 0)
    {
        try
        {
            await player.RunPersistenceExclusiveAsync(() => this.ExecuteCoreAsync(player, request, operation, primarySlot, secondarySlot, coreCount)).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            player.Logger.LogError(exception, "Excellent combination failed for request {RequestId}", request);
            await ReplyAsync(player, request, ExcellentCombinationResources.Message("FailedRequest")).ConfigureAwait(false);
        }
    }

    private static ItemOptionLink[] Excellent(Item item)
        => item.ItemOptions.Where(o => o.ItemOption?.OptionType == ItemOptionTypes.Excellent).ToArray();

    private static IEnumerable<Item> Cores(Player player)
        => player.Inventory?.Items.Where(i => i.Definition is { Group: 14, Number: 252 }
            && i.ItemSlot >= InventoryConstants.EquippableSlotsCount && i.Level == 0 && i.ItemOptions.Count == 0
            && i.ExpiresAtUtc is null && !i.StorePrice.HasValue && (!i.IsStackable() || (i.Durability >= 1
                && i.Durability <= i.Definition.Durability && i.Durability == Math.Truncate(i.Durability))))
            .OrderBy(i => i.ItemSlot) ?? Enumerable.Empty<Item>();

    private static int Count(Item item) => item.IsStackable() ? (int)item.Durability : 1;

    private static CoreMaterial[] Materials(Player player, int count)
    {
        var materials = new List<CoreMaterial>();
        foreach (var item in Cores(player))
        {
            if (count == 0)
            {
                break;
            }

            var take = Math.Min(count, Count(item));
            materials.Add(new(item, item.ItemSlot, item.Durability, take));
            count -= take;
        }

        return materials.ToArray();
    }

    private static ValueTask ReplyAsync(Player player, uint request, string status, ExcellentCombinationConfiguration? settings = null, int cores = 0, bool valid = false, bool success = false, bool completed = false, Item? result = null)
        => player.InvokeViewPlugInAsync<IExcellentCombinationView>(v => v.ShowAsync(request, status, valid, success, completed, settings ?? new ExcellentCombinationConfiguration(), Cores(player).Sum(Count), cores, result));

    private static async ValueTask RestoreAsync(Player player, Item primary, Item secondary, byte secondarySlot, CoreMaterial[] materials, int originalMoney, ItemOptionLink? added, ItemOptionLink? replaced)
    {
        var inventory = player.Inventory!;
        player.SelectedCharacter!.Inventory!.Money = originalMoney;
        if (added is not null)
        {
            primary!.ItemOptions.Remove(added);
            await player.PersistenceContext.DeleteAsync(added).ConfigureAwait(false);
        }

        if (replaced is not null)
        {
            player.PersistenceContext.Attach(replaced);
            primary!.ItemOptions.Add(replaced);
        }

        player.PersistenceContext.Attach(secondary!);
        if (!inventory.Items.Contains(secondary!) && !await inventory.AddItemAsync(secondarySlot, secondary!).ConfigureAwait(false))
        {
            throw new InvalidOperationException("Could not restore combination material.");
        }

        foreach (var material in materials)
        {
            material.Item.Durability = material.Durability;
            if (inventory.Items.Contains(material.Item))
            {
                continue;
            }

            player.PersistenceContext.Attach(material.Item);
            if (!await inventory.AddItemAsync(material.Slot, material.Item).ConfigureAwait(false))
            {
                throw new InvalidOperationException("Could not restore Resonance Cores.");
            }
        }
    }

    private async ValueTask ExecuteCoreAsync(Player player, uint request, byte operation, byte primarySlot, byte secondarySlot, byte coreCount)
    {
        var session = this._sessions.GetValue(player, _ => new());
        if (request == 0 || request <= session.LastRequest)
        {
            await ReplyAsync(player, request, ExcellentCombinationResources.Message("Replay")).ConfigureAwait(false);
            return;
        }

        session.LastRequest = request;
        var configured = ExcellentCombinationFeaturePlugIn.Settings(player.GameContext);
        if (configured?.IsValid != true)
        {
            session.Primary = null;
            await ReplyAsync(player, request, ExcellentCombinationResources.Message("Disabled")).ConfigureAwait(false);
            return;
        }

        // Capture mutable admin settings so a concurrent edit cannot change the price mid-attempt.
        var settings = new ExcellentCombinationConfiguration { BaseRate = configured.BaseRate, BonusPerCore = configured.BonusPerCore, MaximumRate = configured.MaximumRate, ZenCost = configured.ZenCost };
        if (!settings.IsValid)
        {
            return;
        }

        var rates = (settings.BaseRate, settings.BonusPerCore, settings.MaximumRate, settings.ZenCost);
        if (player.Inventory is not { } inventory || player.SelectedCharacter?.Inventory is not { } storage
            || player.IsTemplatePlayer || player.PlayerState.CurrentState != PlayerState.EnteredWorld
            || player.OpenedNpc is not null || player.TemporaryStorage?.Items.Any() == true || player.ShopStorage?.StoreOpen == true || player.IsCashShopOpen
            || operation > 2)
        {
            session.Primary = null;
            await ReplyAsync(player, request, ExcellentCombinationResources.Message("Busy"), settings).ConfigureAwait(false);
            return;
        }

        if (operation == 2)
        {
            session.Primary = null;
            await ReplyAsync(player, request, ExcellentCombinationResources.Message("Configuration"), settings).ConfigureAwait(false);
            return;
        }

        var primary = primarySlot < InventoryConstants.EquippableSlotsCount ? null : inventory.GetItem(primarySlot);
        var secondary = secondarySlot < InventoryConstants.EquippableSlotsCount ? null : inventory.GetItem(secondarySlot);
        if (Validate(primary, secondary) is { } error)
        {
            session.Primary = null;
            await ReplyAsync(player, request, error, settings).ConfigureAwait(false);
            return;
        }

        if (storage.Money < settings.ZenCost)
        {
            session.Primary = null;
            await ReplyAsync(player, request, ExcellentCombinationResources.Message("NoZen"), settings).ConfigureAwait(false);
            return;
        }

        var materials = Materials(player, coreCount);
        if (coreCount > settings.MaximumCores || materials.Sum(m => m.Take) != coreCount)
        {
            session.Primary = null;
            await ReplyAsync(player, request, ExcellentCombinationResources.Message("NoCores"), settings).ConfigureAwait(false);
            return;
        }

        var links = Excellent(primary!).Concat(Excellent(secondary!)).ToArray();
        var values = links.Select(l => (l.ItemOption, l.Level)).ToArray();
        if (operation == 0)
        {
            session.Primary = primary;
            session.Secondary = secondary;
            session.Options = links;
            session.Values = values;
            session.Materials = materials;
            session.Rates = rates;
            session.QuotedAt = DateTime.UtcNow;
            await ReplyAsync(player, request, ExcellentCombinationResources.Message("Ready"), settings, coreCount, valid: true).ConfigureAwait(false);
            return;
        }

        if (session.Primary != primary || session.Secondary != secondary || !session.Options.SequenceEqual(links)
            || !session.Values.SequenceEqual(values) || !session.Materials.SequenceEqual(materials) || session.Rates != rates
            || DateTime.UtcNow - session.QuotedAt > TimeSpan.FromSeconds(30))
        {
            session.Primary = null;
            await ReplyAsync(player, request, ExcellentCombinationResources.Message("Stale"), settings).ConfigureAwait(false);
            return;
        }

        session.Primary = null;
        await this.CommitAsync(player, request, primary!, secondary!, secondarySlot, links, materials, settings, coreCount).ConfigureAwait(false);
    }

    private async ValueTask CommitAsync(Player player, uint request, Item primary, Item secondary, byte secondarySlot, ItemOptionLink[] links, CoreMaterial[] materials, ExcellentCombinationConfiguration settings, byte coreCount)
    {
        var inventory = player.Inventory!;
        var storage = player.SelectedCharacter!.Inventory!;
        var originalMoney = storage.Money;
        var success = this._randomizer.NextInt(0, 100) < settings.Rate(coreCount);
        var primaryOptions = Excellent(primary!);
        var replaced = success && primaryOptions.Length == 2 ? primaryOptions[this._randomizer.NextInt(0, 2)] : null;
        var added = success ? player.PersistenceContext.CreateNew<ItemOptionLink>() : null;
        if (added is not null)
        {
            added.ItemOption = links[^1].ItemOption;
            added.Level = links[^1].Level;
        }

        var committed = false;
        try
        {
            if (replaced is not null)
            {
                primary!.ItemOptions.Remove(replaced);
            }

            if (added is not null)
            {
                primary!.ItemOptions.Add(added);
            }

            await inventory.RemoveItemAsync(secondary!).ConfigureAwait(false);
            await player.PersistenceContext.DeleteAsync(secondary!).ConfigureAwait(false);
            if (replaced is not null)
            {
                await player.PersistenceContext.DeleteAsync(replaced).ConfigureAwait(false);
            }

            foreach (var material in materials)
            {
                if (material.Take == Count(material.Item))
                {
                    await inventory.RemoveItemAsync(material.Item).ConfigureAwait(false);
                    await player.PersistenceContext.DeleteAsync(material.Item).ConfigureAwait(false);
                }
                else
                {
                    material.Item.Durability -= material.Take;
                }
            }

            storage.Money -= settings.ZenCost;
            if (!await player.SaveProgressAsync().ConfigureAwait(false))
            {
                throw new InvalidOperationException("Saving Excellent combination failed.");
            }

            committed = true;
        }
        finally
        {
            if (!committed)
            {
                await RestoreAsync(player, primary, secondary, secondarySlot, materials, originalMoney, added, replaced).ConfigureAwait(false);
            }

            await player.InvokeViewPlugInAsync<IUpdateInventoryListPlugIn>(v => v.UpdateInventoryListAsync()).ConfigureAwait(false);
            await player.InvokeViewPlugInAsync<IUpdateMoneyPlugIn>(v => v.UpdateMoneyAsync()).ConfigureAwait(false);
        }

        await ReplyAsync(player, request, ExcellentCombinationResources.Message(success ? "Success" : "Failure"), settings, coreCount, success: success, completed: true, result: primary).ConfigureAwait(false);
    }

    private sealed record CoreMaterial(Item Item, byte Slot, double Durability, int Take);

    private sealed class Session
    {
        public uint LastRequest { get; set; }
        public Item? Primary { get; set; }
        public Item? Secondary { get; set; }
        public ItemOptionLink[] Options { get; set; } = [];
        public (IncreasableItemOption? Option, int Level)[] Values { get; set; } = [];
        public CoreMaterial[] Materials { get; set; } = [];
        public (int Base, int Bonus, int Cap, int Cost) Rates { get; set; }
        public DateTime QuotedAt { get; set; }
    }
}
