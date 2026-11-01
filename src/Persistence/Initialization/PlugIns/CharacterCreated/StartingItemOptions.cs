// <copyright file="StartingItemOptions.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.PlugIns.CharacterCreated;

using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.GameLogic;

/// <summary>Builds starter items using only options supported by their definitions.</summary>
public static class StartingItemOptions
{
    /// <summary>Creates a validated, temporary starter item before anything is persisted.</summary>
    /// <param name="reward">The configured reward.</param>
    /// <param name="definition">The item definition.</param>
    /// <returns>The item with its configured options.</returns>
    public static TemporaryItem CreateItem(StartingItemReward reward, ItemDefinition definition)
    {
        if (reward.Level > definition.MaximumItemLevel || reward.AdditionalOptionLevel > 7
            || reward.ExcellentOptions is null || reward.ExcellentOptions.Distinct().Count() != reward.ExcellentOptions.Count
            || (reward.Skill && definition.Skill is null))
        {
            throw new InvalidOperationException("Unsupported starting item level, skill, or duplicate excellent options.");
        }

        var item = new TemporaryItem { Definition = definition, Level = reward.Level, Durability = definition.Durability, HasSkill = reward.Skill };
        var options = definition.PossibleItemOptions.SelectMany(o => o.PossibleOptions).ToArray();
        if (reward.Luck)
        {
            item.ItemOptions.Add(new ItemOptionLink { ItemOption = FindOption(options, ItemOptionTypes.Luck) });
        }

        if (reward.AdditionalOptionLevel > 0)
        {
            var option = options.FirstOrDefault(o => o.OptionType == ItemOptionTypes.Option
                && (!reward.AdditionalOptionNumber.HasValue || o.Number == reward.AdditionalOptionNumber)
                && !o.IsFixedOption() && o.LevelDependentOptions.Any(l => l.Level == reward.AdditionalOptionLevel))
                ?? throw new InvalidOperationException("This item does not support the requested additional option level.");
            item.ItemOptions.Add(new ItemOptionLink { ItemOption = option, Level = reward.AdditionalOptionLevel });
        }

        foreach (var number in reward.ExcellentOptions)
        {
            var option = options.SingleOrDefault(o => o.OptionType == ItemOptionTypes.Excellent && o.Number == number)
                ?? throw new InvalidOperationException($"This item does not support excellent option {number}.");
            item.ItemOptions.Add(new ItemOptionLink { ItemOption = option });
        }

        // Excellent weapons carry their skill, as with the existing GM item command.
        item.HasSkill |= reward.ExcellentOptions.Count > 0 && definition.Skill is not null;
        return item;
    }

    private static IncreasableItemOption FindOption(IEnumerable<IncreasableItemOption> options, ItemOptionType type)
    {
        return options.FirstOrDefault(o => o.OptionType == type)
            ?? throw new InvalidOperationException($"This item does not support {type}.");
    }
}
