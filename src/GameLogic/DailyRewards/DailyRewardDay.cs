// <copyright file="DailyRewardDay.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.DailyRewards;

using MUnique.OpenMU.DataModel.Configuration.Items;

/// <summary>The daily login reward of one day of the month.</summary>
public sealed class DailyRewardDay
{
    /// <summary>The maximum item level of a reward item.</summary>
    internal const byte MaximumItemLevel = 15;

    /// <summary>The maximum option level of a reward item.</summary>
    internal const byte MaximumItemOption = 7;

    /// <summary>The flags of all six excellent options.</summary>
    internal const byte AllExcellentOptions = 0b11_1111;

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

    /// <summary>Gets or sets the item which is added to the character's inventory, if any.</summary>
    [Display(Name = nameof(DailyRewardResources.Item), ResourceType = typeof(DailyRewardResources))]
    public ItemDefinition? Item { get; set; }

    /// <summary>Gets or sets the level of the item, limited by its maximum level.</summary>
    [Display(Name = nameof(DailyRewardResources.ItemLevel), ResourceType = typeof(DailyRewardResources))]
    [Range(0, MaximumItemLevel)]
    public byte ItemLevel { get; set; }

    /// <summary>Gets or sets the quantity of a stackable item, e.g. potions; zero for its full stack.</summary>
    [Display(Name = nameof(DailyRewardResources.ItemQuantity), ResourceType = typeof(DailyRewardResources))]
    [Range(0, byte.MaxValue)]
    public byte ItemQuantity { get; set; }

    /// <summary>Gets or sets a value indicating whether the item has its skill.</summary>
    [Display(Name = nameof(DailyRewardResources.ItemSkill), ResourceType = typeof(DailyRewardResources))]
    public bool ItemSkill { get; set; }

    /// <summary>Gets or sets a value indicating whether the item has luck.</summary>
    [Display(Name = nameof(DailyRewardResources.ItemLuck), ResourceType = typeof(DailyRewardResources))]
    public bool ItemLuck { get; set; }

    /// <summary>Gets or sets the level of the item's normal option, e.g. 1 for +4; zero for none.</summary>
    [Display(Name = nameof(DailyRewardResources.ItemOption), ResourceType = typeof(DailyRewardResources))]
    [Range(0, MaximumItemOption)]
    public byte ItemOption { get; set; }

    /// <summary>
    /// Gets or sets the excellent options of the item as flags, like the GM item command: 1, 2, 4, 8, 16 and 32 for the
    /// first to sixth option. A skin receives the one allowed bonus of its flag instead.
    /// </summary>
    [Display(Name = nameof(DailyRewardResources.ItemExcellentOptions), ResourceType = typeof(DailyRewardResources))]
    [Range(0, AllExcellentOptions)]
    public byte ItemExcellentOptions { get; set; }

    /// <summary>Gets a value indicating whether anything is granted on this day.</summary>
    public bool HasReward => this.Zen > 0 || this.WCoinC > 0 || this.WCoinP > 0 || this.GoblinPoints > 0 || this.Item is not null;

    /// <summary>Gets a value indicating whether the values are within their limits.</summary>
    internal bool IsValid => this is { Day: >= 1 and <= DailyRewardCalendar.MaximumDays, Zen: >= 0, WCoinC: >= 0, WCoinP: >= 0, GoblinPoints: >= 0 }
        && this.ItemLevel <= MaximumItemLevel && this.ItemOption <= MaximumItemOption && this.ItemExcellentOptions <= AllExcellentOptions;

    /// <inheritdoc />
    public override string ToString() => FormattableString.Invariant($"{this.Day}: {this.Item?.Name.ValueInNeutralLanguage ?? string.Empty} {this.Zen}");
}
