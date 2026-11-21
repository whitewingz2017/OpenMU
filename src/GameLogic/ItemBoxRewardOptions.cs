// <copyright file="ItemBoxRewardOptions.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic;

using MUnique.OpenMU.DataModel.Configuration.Items;

/// <summary>Applies item-box overrides without changing the shared option definitions.</summary>
internal static class ItemBoxRewardOptions
{
    /// <summary>Applies only the options supported by the selected reward item.</summary>
    /// <param name="item">The generated reward.</param>
    /// <param name="group">The box reward configuration.</param>
    internal static void Apply(Item item, ItemDropItemGroup group)
    {
        var possibleOptions = item.Definition!.PossibleItemOptions.SelectMany(options => options.PossibleOptions).ToArray();
        if (group.HasLuck is { } hasLuck)
        {
            Remove(item, ItemOptionTypes.Luck);
            if (hasLuck && possibleOptions.FirstOrDefault(option => option.OptionType == ItemOptionTypes.Luck) is { } luck)
            {
                item.ItemOptions.Add(new ItemOptionLink { ItemOption = luck });
            }
        }

        if (group.HasSkill is { } hasSkill)
        {
            item.HasSkill = hasSkill && item.CanHaveSkill();
        }

        if (group.OptionLevel is { } optionLevel)
        {
            Remove(item, ItemOptionTypes.Option);
            if (optionLevel > 0 && possibleOptions.FirstOrDefault(option => option.OptionType == ItemOptionTypes.Option && !option.IsFixedOption()) is { } option)
            {
                var level = option.LevelDependentOptions.Select(entry => entry.Level)
                    .Where(value => value <= Math.Min(optionLevel, (byte)7)).Append(1).Max();
                item.ItemOptions.Add(new ItemOptionLink { ItemOption = option, Level = level });
            }
        }

        if (group.ExcellentOptions is { } flags)
        {
            Remove(item, ItemOptionTypes.Excellent);
            foreach (var option in possibleOptions.Where(option => option.OptionType == ItemOptionTypes.Excellent
                         && option.Number is >= 1 and <= 6 && (flags & (1 << (option.Number - 1))) != 0))
            {
                item.ItemOptions.Add(new ItemOptionLink { ItemOption = option });
            }
        }
    }

    private static void Remove(Item item, ItemOptionType type)
    {
        foreach (var link in item.ItemOptions.Where(link => link.ItemOption?.OptionType == type
                     && (type != ItemOptionTypes.Option || link.ItemOption.IsFixedOption() is false)).ToArray())
        {
            item.ItemOptions.Remove(link);
        }
    }
}
