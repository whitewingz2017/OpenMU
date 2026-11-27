// <copyright file="DailyRewardDay.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.DailyRewards;

using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text.Json;
using MUnique.OpenMU.DataModel.Composition;
using MUnique.OpenMU.DataModel.Configuration;

/// <summary>The daily login reward of one day of the month.</summary>
public sealed class DailyRewardDay : DailyRewardItem
{
    /// <summary>The largest number of items a single class family can see on a day.</summary>
    public const int MaximumItems = 8;

    /// <summary>Allows eight items for each of the seven Season 6 class families in the calendar editor.</summary>
    public const int MaximumConfiguredItems = 56;

    /// <summary>Gets or sets the day of the month, from 1 to 31.</summary>
    [Display(Name = nameof(DailyRewardResources.Day), ResourceType = typeof(DailyRewardResources))]
    [Range(1, DailyRewardCalendar.MaximumDays)]
    public int Day { get; set; } = 1;

    /// <summary>Gets or sets the zen which is added to the character's inventory.</summary>
    [Display(Name = nameof(DailyRewardResources.Zen), ResourceType = typeof(DailyRewardResources))]
    [Range(0, int.MaxValue)]
    public int Zen { get; set; }

    /// <summary>Gets or sets the WCoinC which is added to the account.</summary>
    [Display(Name = nameof(DailyRewardResources.WCoinC), ResourceType = typeof(DailyRewardResources))]
    [Range(0, int.MaxValue)]
    public int WCoinC { get; set; }

    /// <summary>Gets or sets the WCoinP which is added to the account.</summary>
    [Display(Name = nameof(DailyRewardResources.WCoinP), ResourceType = typeof(DailyRewardResources))]
    [Range(0, int.MaxValue)]
    public int WCoinP { get; set; }

    /// <summary>Gets or sets the Goblin Points which are added to the account.</summary>
    [Display(Name = nameof(DailyRewardResources.GoblinPoints), ResourceType = typeof(DailyRewardResources))]
    [Range(0, int.MaxValue)]
    public int GoblinPoints { get; set; }

    /// <summary>Gets or sets the additional items offered by this day.</summary>
    [MemberOfAggregate]
    public List<DailyRewardItem> Items { get; set; } = [];

    /// <summary>Gets or sets a value indicating whether the player selects exactly one item.</summary>
    public bool ChooseOneItem { get; set; }

    /// <summary>Gets a value indicating whether anything is granted on this day.</summary>
    public bool HasReward => this.HasCurrency || this.ItemCount > 0;

    /// <summary>Gets the number of items, including the item from an older calendar configuration.</summary>
    public int ItemCount => (this.Item is null ? 0 : 1) + this.Items.Count;

    /// <summary>Gets a value indicating whether a claim must identify the displayed item list.</summary>
    public bool RequiresOffer => this.ChooseOneItem || this.ItemCount > 1;

    /// <summary>Gets a value indicating whether the values are within their limits.</summary>
    internal bool IsValid => this is { Day: >= 1 and <= DailyRewardCalendar.MaximumDays, Zen: >= 0, WCoinC: >= 0, WCoinP: >= 0, GoblinPoints: >= 0, Items: not null }
        && this.ItemCount <= MaximumConfiguredItems && (!this.ChooseOneItem || this.ItemCount > 1)
        && this.HasValidOptions && this.Items.All(item => item is { Item: not null, HasValidOptions: true });

    private bool HasCurrency => this.Zen > 0 || this.WCoinC > 0 || this.WCoinP > 0 || this.GoblinPoints > 0;

    /// <summary>Gets all offered items in the order displayed by the client.</summary>
    /// <returns>The configured items.</returns>
    public IEnumerable<DailyRewardItem> GetItems() => this.Item is null ? this.Items : this.Items.Prepend(this);

    /// <summary>Projects this day into the offer the current character is able to claim.</summary>
    /// <param name="family">The base class of the selected character.</param>
    /// <returns>The projected day, or null if there is no eligible reward.</returns>
    public DailyRewardDay? ForClass(CharacterClass? family)
    {
        if (this.ItemCount == 0)
        {
            return this.HasCurrency ? this : null;
        }

        var items = this.GetItems().Where(item => item.IsAvailableTo(family)).ToList();
        if (items.Count > MaximumItems || (items.Count == 0 && !this.HasCurrency))
        {
            return null;
        }

        return items.Count == this.ItemCount ? this : new DailyRewardDay
        {
            Day = this.Day,
            Zen = this.Zen,
            WCoinC = this.WCoinC,
            WCoinP = this.WCoinP,
            GoblinPoints = this.GoblinPoints,
            ChooseOneItem = this.ChooseOneItem && items.Count > 1,
            Items = items,
        };
    }

    /// <summary>Identifies the terms displayed for this day, so an admin edit cannot change a pending choice.</summary>
    /// <param name="month">The calendar month, including the year.</param>
    /// <returns>The offer identifier.</returns>
    public uint GetOfferKey(int month)
    {
        var data = JsonSerializer.SerializeToUtf8Bytes(new
        {
            month,
            this.Day,
            this.Zen,
            this.WCoinC,
            this.WCoinP,
            this.GoblinPoints,
            this.ChooseOneItem,
            Items = this.GetItems().Select(entry => new
            {
                entry.Item!.Group,
                entry.Item!.Number,
                entry.ItemLevel,
                entry.ItemQuantity,
                entry.ItemDurationHours,
                entry.ItemSkill,
                entry.ItemLuck,
                entry.ItemOption,
                entry.ItemExcellentOptions,
            }),
        });
        var key = BinaryPrimitives.ReadUInt32LittleEndian(SHA256.HashData(data));
        return key == 0 ? 1 : key;
    }

    /// <inheritdoc />
    public override string ToString() => FormattableString.Invariant($"{this.Day}: {this.Item?.Name.ValueInNeutralLanguage ?? string.Empty} {this.Zen}");
}
