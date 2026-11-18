// <copyright file="ItemBonusStatistics.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.ItemBonuses;

using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.GameLogic.Attributes;

/// <summary>
/// What the worn picked items add, for Combat statistics: each attribute's total beside the statistics it adds to
/// as it is (e.g. Final Defense beside both Defense lines), or on its own line.
/// </summary>
public static class ItemBonusStatistics
{
    /// <summary>Creates the lines.</summary>
    /// <param name="player">The player.</param>
    /// <returns>The lines: the statistic's label (empty for a line of its own) and the text.</returns>
    public static IReadOnlyList<ItemBonusStatistic> Create(Player player)
    {
        var picked = ItemBonusFeaturePlugIn.GetItems(player.GameContext);
        if (picked.Count == 0 || player.Inventory is not { } inventory)
        {
            return [];
        }

        var totals = new Dictionary<(AttributeDefinition Target, AggregateType Aggregate), float>();
        foreach (var item in inventory.EquippedItems.Where(item => item.Durability > 0 && ItemBonusFeaturePlugIn.Contains(picked, item.Definition)))
        {
            foreach (var attribute in item.Definition!.BasePowerUpAttributes.Where(attribute => attribute.TargetAttribute is not null))
            {
                var key = (attribute.TargetAttribute!, attribute.AggregateType);
                var value = ItemBonusText.ValueAt(attribute, item.Level);
                totals[key] = totals.TryGetValue(key, out var total) ? Combine(attribute.AggregateType, total, value) : value;
            }
        }

        IEnumerable<AttributeRelationship> relationships = player.SelectedCharacter?.CharacterClass?.AttributeCombinations ?? Enumerable.Empty<AttributeRelationship>();
        var lines = new List<ItemBonusStatistic>();
        foreach (var ((target, aggregate), value) in totals)
        {
            var text = ItemBonusText.Write(target, aggregate, value);
            var labels = CharacterCombatStatistics.Rows
                .Where(row => row.Attribute == target || relationships.Any(relation => AddsAsItIs(relation, target, row.Attribute)))
                .Select(row => row.Label)
                .ToList();
            if (labels.Count == 0)
            {
                lines.Add(new ItemBonusStatistic(string.Empty, text));
            }
            else
            {
                lines.AddRange(labels.Select(label => new ItemBonusStatistic(label, text)));
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
    private static bool AddsAsItIs(AttributeRelationship relation, AttributeDefinition input, AttributeDefinition statistic)
        => relation.InputAttribute == input
           && relation.TargetAttribute == statistic
           && relation.OperandAttribute is null
           && relation.InputOperator == InputOperator.Multiply
           && Math.Abs(relation.InputOperand - 1) < 0.0001f;
}
