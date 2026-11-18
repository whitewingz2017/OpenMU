// <copyright file="ItemBonusStatistics.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.ItemBonuses;

using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.GameLogic.Attributes;

/// <summary>
/// What the worn items of <see cref="ItemBonusFeaturePlugIn"/> add, for Combat statistics: a statistic which takes an
/// attribute as it is (e.g. both Defense lines take Final Defense) shows its value without it and the amount beside
/// it, "1676.57 (+250)"; an attribute no statistic takes gets a line of its own.
/// </summary>
public static class ItemBonusStatistics
{
    /// <summary>Takes the worn items' attributes out of the statistics and returns them as lines.</summary>
    /// <param name="player">The player.</param>
    /// <param name="statistics">The statistics by label, as <see cref="CharacterCombatStatistics.Create"/> made them; changed.</param>
    /// <returns>The lines: the statistic's label (empty for a line of its own) and the text.</returns>
    public static IReadOnlyList<ItemBonusStatistic> Apply(Player player, IDictionary<string, float> statistics)
    {
        var shown = ItemBonusFeaturePlugIn.GetItems(player.GameContext);
        if (shown.Count == 0 || player.Inventory is not { } inventory)
        {
            return [];
        }

        var totals = new Dictionary<(AttributeDefinition Target, AggregateType Aggregate), float>();
        foreach (var item in inventory.EquippedItems.Where(item => item.Durability > 0 && ItemBonusFeaturePlugIn.Contains(shown, item.Definition)))
        {
            foreach (var attribute in item.Definition!.BasePowerUpAttributes.Where(attribute => attribute.TargetAttribute is not null))
            {
                var key = (attribute.TargetAttribute!, attribute.AggregateType);
                var value = ItemBonusText.ValueAt(attribute, item.Level);
                totals[key] = totals.TryGetValue(key, out var total) ? Combine(attribute.AggregateType, total, value) : value;
            }
        }

        IEnumerable<AttributeRelationship> relationships = player.SelectedCharacter?.CharacterClass?.AttributeCombinations
            ?? Enumerable.Empty<AttributeRelationship>();
        var lines = new List<ItemBonusStatistic>();
        foreach (var ((target, aggregate), value) in totals)
        {
            // A maximum isn't added: the statistic can't be shown without it.
            var rows = CharacterCombatStatistics.Rows
                .Where(row => aggregate != AggregateType.Maximum
                    && statistics.ContainsKey(row.Label)
                    && (row.Attribute == target || relationships.Any(relation => TakesAsItIs(relation, target, row.Attribute))))
                .ToList();
            if (rows.Count == 0)
            {
                lines.Add(new ItemBonusStatistic(string.Empty, ItemBonusText.Write(target, aggregate, value)));
                continue;
            }

            foreach (var row in rows)
            {
                if (aggregate == AggregateType.Multiplicate)
                {
                    statistics[row.Label] = value == 0 ? statistics[row.Label] : statistics[row.Label] / value;
                    lines.Add(new ItemBonusStatistic(row.Label, ItemBonusText.WriteAmount(aggregate, value)));
                }
                else
                {
                    statistics[row.Label] -= value * row.Factor;
                    lines.Add(new ItemBonusStatistic(row.Label, ItemBonusText.WriteAmount(aggregate, value * row.Factor)));
                }
            }
        }

        return lines;
    }

    private static float Combine(AggregateType aggregate, float total, float value) => aggregate switch
    {
        AggregateType.Multiplicate => total * value,
        AggregateType.Maximum => Math.Max(total, value),
        _ => total + value,
    };

    // The statistic takes the attribute's value as it is: e.g. Defense (PvM) = 1 * Final Defense.
    private static bool TakesAsItIs(AttributeRelationship relation, AttributeDefinition input, AttributeDefinition statistic)
        => relation.InputAttribute == input
           && relation.TargetAttribute == statistic
           && relation.OperandAttribute is null
           && relation.InputOperator == InputOperator.Multiply
           && Math.Abs(relation.InputOperand - 1) < 0.0001f;
}
