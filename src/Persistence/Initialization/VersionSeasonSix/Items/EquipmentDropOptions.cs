// <copyright file="EquipmentDropOptions.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Items;

using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Attributes;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.Interfaces;
using MUnique.OpenMU.Persistence.Initialization.Properties;

/// <summary>Configures the requested luck chance for 380/socket equipment and the drop socket cap.</summary>
public static class EquipmentDropOptions
{
    private static readonly Guid LuckDefinitionId = new("f73e8b73-e610-4f17-b6f6-2e0b9835c04a");

    /// <summary>Updates configuration only; existing items and crafting socket capacity are preserved.</summary>
    /// <param name="context">The configuration context.</param>
    /// <param name="configuration">The Season 6 configuration.</param>
    public static void Apply(IContext context, GameConfiguration configuration)
    {
        configuration.MaximumSocketCountDrop = 2;
        var luck = configuration.ItemOptions.FirstOrDefault(option => option.GetId() == LuckDefinitionId);
        if (luck is null)
        {
            var source = configuration.ItemOptions.FirstOrDefault(option => option.PossibleOptions.Any(possible => possible.OptionType == ItemOptionTypes.Luck));
            if (source is null)
            {
                return;
            }

            luck = context.CreateNew<ItemOptionDefinition>();
            luck.SetGuid(LuckDefinitionId);
            luck.Name = LocalizedString.FromResource(() => ItemOptionNames.Luck);
            foreach (var option in source.PossibleOptions.Where(option => option.OptionType == ItemOptionTypes.Luck))
            {
                // Options and their boosts are owned entities; sharing them would move the original luck away.
                var copy = context.CreateNew<IncreasableItemOption>();
                copy.Number = option.Number;
                copy.SubOptionType = option.SubOptionType;
                copy.OptionType = option.OptionType;
                copy.LevelType = option.LevelType;
                copy.PowerUpDefinition = CopyPowerUp(context, option.PowerUpDefinition);
                foreach (var level in option.LevelDependentOptions)
                {
                    var copiedLevel = context.CreateNew<ItemOptionOfLevel>();
                    copiedLevel.Level = level.Level;
                    copiedLevel.RequiredItemLevel = level.RequiredItemLevel;
                    copiedLevel.PowerUpDefinition = CopyPowerUp(context, level.PowerUpDefinition);
                    copy.LevelDependentOptions.Add(copiedLevel);
                }

                luck.PossibleOptions.Add(copy);
            }

            configuration.ItemOptions.Add(luck);
        }

        luck.AddsRandomly = true;
        luck.MaximumOptionsPerItem = 1;
        luck.AddChance = 0.30f;
        foreach (var item in configuration.Items.Where(item => item.MaximumSockets > 0
            || item.PossibleItemOptions.Any(option => option.PossibleOptions.Any(possible => possible.OptionType == ItemOptionTypes.GuardianOption))))
        {
            foreach (var previous in item.PossibleItemOptions.Where(option => option != luck && option.PossibleOptions.Any(possible => possible.OptionType == ItemOptionTypes.Luck)).ToArray())
            {
                item.PossibleItemOptions.Remove(previous);
            }

            if (!item.PossibleItemOptions.Contains(luck))
            {
                item.PossibleItemOptions.Add(luck);
            }
        }
    }

    private static PowerUpDefinition? CopyPowerUp(IContext context, PowerUpDefinition? source)
    {
        if (source is null)
        {
            return null;
        }

        var copy = context.CreateNew<PowerUpDefinition>();
        copy.TargetAttribute = source.TargetAttribute;
        if (source.Boost is { } boost)
        {
            copy.Boost = context.CreateNew<PowerUpDefinitionValue>();
            copy.Boost.ConstantValue.Value = boost.ConstantValue.Value;
            copy.Boost.ConstantValue.AggregateType = boost.ConstantValue.AggregateType;
            foreach (var relation in boost.RelatedValues)
            {
                var copiedRelation = context.CreateNew<AttributeRelationship>();
                copiedRelation.InputAttribute = relation.InputAttribute;
                copiedRelation.InputOperand = relation.InputOperand;
                copiedRelation.InputOperator = relation.InputOperator;
                copiedRelation.TargetAttribute = relation.TargetAttribute;
                copiedRelation.AggregateType = relation.AggregateType;
                copy.Boost.RelatedValues.Add(copiedRelation);
            }
        }

        return copy;
    }
}
