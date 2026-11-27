// <copyright file="DailyRewardActions.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.DailyRewards;

using System.Runtime.CompilerServices;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.GameLogic.CashShop;
using MUnique.OpenMU.GameLogic.Cosmetics;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.GameLogic.Views.Inventory;
using MUnique.OpenMU.Persistence;

/// <summary>Loads the daily login reward calendar and claims the reward of the current day.</summary>
public sealed class DailyRewardActions
{
    /// <summary>The operation number for claiming today's rewards.</summary>
    public const byte ClaimOperation = 1;

    private readonly ConditionalWeakTable<Player, Session> _sessions = new();

    /// <summary>Processes a calendar request or a claim.</summary>
    /// <param name="player">The player.</param>
    /// <param name="requestId">The request identifier used to reject replays.</param>
    /// <param name="operation">Zero loads the calendar, one claims the reward of <paramref name="day"/>.</param>
    /// <param name="day">The day of the month the client claims; it has to be the server's current day.</param>
    /// <param name="utcNow">The current time; <c>null</c> for <see cref="DateTime.UtcNow"/>.</param>
    /// <param name="offerKey">The displayed offer identifier for a multi-item day.</param>
    /// <param name="choice">The one-based choice index, or zero for a bundle.</param>
    /// <returns>The completion of the request and its response.</returns>
    public async ValueTask ExecuteAsync(Player player, uint requestId, byte operation, byte day, DateTime? utcNow = null, uint offerKey = 0, byte choice = 0)
    {
        var (status, itemAdded) = await player.RunPersistenceExclusiveAsync(
            () => this.ProcessAsync(player, requestId, operation, day, utcNow ?? DateTime.UtcNow, offerKey, choice)).ConfigureAwait(false);
        if (itemAdded)
        {
            await player.InvokeViewPlugInAsync<IUpdateInventoryListPlugIn>(view => view.UpdateInventoryListAsync()).ConfigureAwait(false);
        }

        await player.InvokeViewPlugInAsync<IDailyRewardView>(view => view.ShowAsync(requestId, status)).ConfigureAwait(false);
    }

    private static async ValueTask<(string Status, bool ItemAdded)> ClaimAsync(Player player, DailyRewardSettings settings, byte day, DateTime utcNow, uint offerKey, byte choice)
    {
        if (player.OpenedNpc is not null || player.TemporaryStorage?.Items.Any() == true
            || player.ShopStorage?.StoreOpen == true || player.IsCashShopOpen)
        {
            return ("DailyReward_CloseWindows", false);
        }

        var (month, today, _) = DailyRewardCalendar.Today(utcNow);
        if (day != today)
        {
            // E.g. the calendar was opened before midnight.
            return ("DailyReward_NotToday", false);
        }

        var account = player.Account!;
        var claimedDays = DailyRewardCalendar.ClaimedDays(account, month);
        if (DailyRewardCalendar.IsClaimed(claimedDays, today))
        {
            return ("DailyReward_AlreadyClaimed", false);
        }

        if (settings.GetDay(today)?.ForClass(CharacterClassFamily.Of(player)) is not { } reward)
        {
            return ("DailyReward_NoReward", false);
        }

        if (reward.Zen > 0 && (long)player.Money + reward.Zen > (player.GameContext.Configuration.MaximumInventoryMoney is > 0 and var maximum ? maximum : int.MaxValue))
        {
            return ("DailyReward_ZenLimit", false);
        }

        if ((reward.RequiresOffer || offerKey != 0) && offerKey != reward.GetOfferKey(month))
        {
            return ("DailyReward_OfferChanged", false);
        }

        if (reward.ChooseOneItem ? choice < 1 || choice > reward.ItemCount : choice != 0)
        {
            return ("DailyReward_SelectItem", false);
        }

        var items = reward.ChooseOneItem ? [reward.GetItems().ElementAt(choice - 1)] : reward.GetItems().ToArray();
        if (ValidateExpiration(player, items, utcNow) is { } expirationError)
        {
            return (expirationError, false);
        }

        return await GrantAsync(player, reward, items, month, claimedDays | DailyRewardCalendar.DayBit(today), utcNow).ConfigureAwait(false);
    }

    private static string? ValidateExpiration(Player player, IReadOnlyList<DailyRewardItem> items, DateTime utcNow)
    {
        var longestDuration = items.Select(item => item.ItemDurationHours).DefaultIfEmpty().Max();
        if (longestDuration == 0)
        {
            return null;
        }

        if (TimeSpan.FromHours(longestDuration).TotalSeconds > int.MaxValue - new DateTimeOffset(utcNow).ToUnixTimeSeconds())
        {
            return "DailyReward_Invalid";
        }

        return player.GameContext.PlugInManager.IsPlugInActive(typeof(ItemExpirationPlugIn)) ? null : "DailyReward_ExpirationDisabled";
    }

    private static async ValueTask<(string Status, bool ItemAdded)> GrantAsync(Player player, DailyRewardDay reward, IReadOnlyList<DailyRewardItem> rewards, int month, int claimedDays, DateTime utcNow)
    {
        var account = player.Account!;
        var (oldMonth, oldClaimedDays) = (account.DailyRewardMonth, account.DailyRewardClaimedDays);
        var (oldWCoinC, oldWCoinP, oldGoblinPoints) = (account.WCoinC, account.WCoinP, account.GoblinPoints);
        var oldMoney = player.Money;
        var items = new List<Item>(rewards.Count);
        var committed = false;
        try
        {
            foreach (var entry in rewards)
            {
                var item = player.PersistenceContext.CreateNew<Item>();
                items.Add(item);
                PopulateItem(player.PersistenceContext, item, entry.Item!, entry, utcNow);
                if (!await player.Inventory!.AddItemAsync(item).ConfigureAwait(false))
                {
                    return ("DailyReward_InventoryFull", false);
                }
            }

            if (reward.Zen > 0)
            {
                player.Money = oldMoney + reward.Zen;
            }

            account.WCoinC = Add(oldWCoinC, reward.WCoinC);
            account.WCoinP = Add(oldWCoinP, reward.WCoinP);
            account.GoblinPoints = Add(oldGoblinPoints, reward.GoblinPoints);
            account.DailyRewardMonth = month;
            account.DailyRewardClaimedDays = claimedDays;
            committed = await player.SaveProgressAsync().ConfigureAwait(false);
            return committed ? ("DailyReward_Claimed", items.Count > 0) : ("DailyReward_SaveFailed", false);
        }
        catch (Exception exception)
        {
            player.Logger.LogError(exception, "Claiming the daily reward failed for {Player}.", player);
            return ("DailyReward_SaveFailed", false);
        }
        finally
        {
            if (!committed)
            {
                (account.DailyRewardMonth, account.DailyRewardClaimedDays) = (oldMonth, oldClaimedDays);
                (account.WCoinC, account.WCoinP, account.GoblinPoints) = (oldWCoinC, oldWCoinP, oldGoblinPoints);
                player.Money = oldMoney;
                foreach (var item in items)
                {
                    await RollbackItemAsync(player, item).ConfigureAwait(false);
                }
            }
        }
    }

    private static async ValueTask RollbackItemAsync(Player player, Item item)
    {
        if (player.Inventory!.Items.Contains(item))
        {
            await player.Inventory.RemoveItemAsync(item).ConfigureAwait(false);
        }

        foreach (var link in item.ItemOptions.ToArray())
        {
            item.ItemOptions.Remove(link);
            await player.PersistenceContext.DeleteAsync(link).ConfigureAwait(false);
        }

        await player.PersistenceContext.DeleteAsync(item).ConfigureAwait(false);
    }

    private static int Add(int balance, int amount) => (int)Math.Min((long)balance + amount, int.MaxValue);

    private static void PopulateItem(IContext context, Item item, ItemDefinition definition, DailyRewardItem reward, DateTime utcNow)
    {
        item.Definition = definition;
        item.Level = Math.Min(reward.ItemLevel, definition.MaximumItemLevel);
        item.Durability = item.IsStackable() && reward.ItemQuantity > 0 ? Math.Min(reward.ItemQuantity, definition.Durability) : definition.Durability;
        item.HasSkill = definition.Skill is not null && (reward.ItemSkill || FenrirMountRules.IsCustom(definition));
        item.ExpiresAtUtc = reward.ItemDurationHours > 0 ? utcNow.AddHours(reward.ItemDurationHours) : null;

        var possibleOptions = definition.PossibleItemOptions.SelectMany(group => group.PossibleOptions).ToList();
        if (CosmeticArmorRules.GetSlot(definition) is not null)
        {
            // A skin has the one allowed bonus of its flag, and no other option.
            if (SkinBonuses.FromFlags(definition, reward.ItemExcellentOptions) is { } bonus)
            {
                AddOption(context, item, bonus, 1);
            }

            return;
        }

        if (reward.ItemOption > 0 && possibleOptions.FirstOrDefault(option => option.OptionType == ItemOptionTypes.Option && !option.IsFixedOption()) is { } normalOption)
        {
            AddOption(context, item, normalOption, reward.ItemOption);
        }

        if (reward.ItemLuck && possibleOptions.FirstOrDefault(option => option.OptionType == ItemOptionTypes.Luck) is { } luck)
        {
            AddOption(context, item, luck, 0);
        }

        foreach (var excellent in possibleOptions.Where(option => option.OptionType == ItemOptionTypes.Excellent
                     && option.Number is >= 1 and <= 6 && (reward.ItemExcellentOptions & (1 << (option.Number - 1))) != 0))
        {
            AddOption(context, item, excellent, 0);

            // Like the GM item command, every excellent item has its skill.
            item.HasSkill |= definition.Skill is not null;
        }
    }

    private static void AddOption(IContext context, Item item, IncreasableItemOption option, int level)
    {
        var link = context.CreateNew<ItemOptionLink>();
        link.ItemOption = option;
        link.Level = level;
        item.ItemOptions.Add(link);
    }

    private async ValueTask<(string? Status, bool ItemAdded)> ProcessAsync(Player player, uint requestId, byte operation, byte day, DateTime utcNow, uint offerKey, byte choice)
    {
        if (player.SelectedCharacter is not { } character || player.Account is null || player.Inventory is null
            || player.IsTemplatePlayer || player.PlayerState.CurrentState != PlayerState.EnteredWorld)
        {
            return ("DailyReward_Invalid", false);
        }

        var session = this._sessions.GetValue(player, _ => new());
        if (!ReferenceEquals(session.Character, character))
        {
            session.Character = character;
            session.LastRequest = 0;
        }

        if (requestId == 0 || requestId <= session.LastRequest)
        {
            return ("DailyReward_Retry", false);
        }

        session.LastRequest = requestId;
        if (DailyRewardFeaturePlugIn.Settings(player.GameContext) is not { } settings)
        {
            return ("DailyReward_Disabled", false);
        }

        await player.ApplyPendingCashShopCoinGrantsAsync().ConfigureAwait(false);
        return operation switch
        {
            0 => (null, false),
            ClaimOperation => await ClaimAsync(player, settings, day, utcNow, offerKey, choice).ConfigureAwait(false),
            _ => ("DailyReward_Invalid", false),
        };
    }

    private sealed class Session
    {
        public Character? Character { get; set; }

        public uint LastRequest { get; set; }
    }
}
