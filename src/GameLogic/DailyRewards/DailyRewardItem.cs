// <copyright file="DailyRewardItem.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.DailyRewards;

using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;

/// <summary>An item and its configured options and lifetime.</summary>
public class DailyRewardItem
{
    /// <summary>The maximum item level of a reward item.</summary>
    internal const byte MaximumItemLevel = 15;

    /// <summary>The maximum option level of a reward item.</summary>
    internal const byte MaximumItemOption = 7;

    /// <summary>The flags of all six excellent options.</summary>
    internal const byte AllExcellentOptions = 0b11_1111;

    /// <summary>The period-item protocol represents expiration as signed Unix seconds.</summary>
    internal const int MaximumItemDurationHours = int.MaxValue / 3600;

    /// <summary>Gets or sets the item definition granted to the character.</summary>
    [Display(Name = nameof(DailyRewardResources.Item), ResourceType = typeof(DailyRewardResources))]
    public ItemDefinition? Item { get; set; }

    /// <summary>Gets or sets a base class which further restricts who may claim the item.</summary>
    [Display(Name = nameof(DailyRewardResources.ClassFamily), ResourceType = typeof(DailyRewardResources))]
    public CharacterClass? ClassFamily { get; set; }

    /// <summary>Gets or sets the level of the item, limited by its maximum level.</summary>
    [Display(Name = nameof(DailyRewardResources.ItemLevel), ResourceType = typeof(DailyRewardResources))]
    [Range(0, MaximumItemLevel)]
    public byte ItemLevel { get; set; }

    /// <summary>Gets or sets the quantity of a stackable item, e.g. potions; zero for its full stack.</summary>
    [Display(Name = nameof(DailyRewardResources.ItemQuantity), ResourceType = typeof(DailyRewardResources))]
    [Range(0, byte.MaxValue)]
    public byte ItemQuantity { get; set; }

    /// <summary>Gets or sets the item's lifetime from claiming, in hours; zero means permanent.</summary>
    [Display(Name = nameof(DailyRewardResources.ItemDurationHours), ResourceType = typeof(DailyRewardResources))]
    [Range(0, MaximumItemDurationHours)]
    public int ItemDurationHours { get; set; }

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

    /// <summary>Gets a value indicating whether the item options are within their limits.</summary>
    internal bool HasValidOptions => this.ItemLevel <= MaximumItemLevel && this.ItemOption <= MaximumItemOption
        && this.ItemExcellentOptions <= AllExcellentOptions && this.ItemDurationHours is >= 0 and <= MaximumItemDurationHours;

    /// <summary>Checks the item definition and the optional restriction set for this reward.</summary>
    /// <param name="family">The base class of the character claiming the reward.</param>
    /// <returns>Whether this family can receive the item.</returns>
    internal bool IsAvailableTo(CharacterClass? family)
        => this.Item is { } item && (this.ClassFamily is null || this.ClassFamily.Number == family?.Number)
            && CharacterClassFamily.Allows(item, family);
}
