// <copyright file="ItemBonusText.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.ItemBonuses;

using System.Globalization;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.GameLogic.Attributes;

/// <summary>Writes an item's base power-up attributes as the custom client shows them, e.g. "+250 Final Defense".</summary>
public static class ItemBonusText
{
    /// <summary>Gets the value of an attribute at an item level: its base value and the level's bonus.</summary>
    /// <param name="attribute">The attribute.</param>
    /// <param name="level">The item level.</param>
    /// <returns>The value.</returns>
    public static float ValueAt(ItemBasePowerUpDefinition attribute, int level)
        => attribute.BaseValue + (attribute.BonusPerLevelTable?.BonusPerLevel.FirstOrDefault(bonus => bonus.Level == level)?.AdditionalValue ?? 0);

    /// <summary>Gets the levels which have a level bonus.</summary>
    /// <param name="attribute">The attribute.</param>
    /// <returns>The levels.</returns>
    public static IEnumerable<int> BonusLevels(ItemBasePowerUpDefinition attribute)
        => attribute.BonusPerLevelTable?.BonusPerLevel.Select(bonus => bonus.Level).Distinct().Order() ?? Enumerable.Empty<int>();

    /// <summary>Writes an attribute's value.</summary>
    /// <param name="target">The attribute.</param>
    /// <param name="aggregate">How the value is applied.</param>
    /// <param name="value">The value.</param>
    /// <returns>The text, e.g. "+250 Final Defense", "+10% Excellent Damage Chance" or "Movement Speed 19".</returns>
    public static string Write(AttributeDefinition target, AggregateType aggregate, float value)
    {
        if (target == Stats.CanFly)
        {
            return "Can enter Icarus";
        }

        var name = (target.GetDisplayName() ?? target.Designation ?? string.Empty).TrimEnd('.', ' ');
        return aggregate switch
        {
            AggregateType.Multiplicate => Signed((value - 1) * 100) + "% " + name,
            AggregateType.Maximum => name + " " + Number(value),

            // A fraction is a chance or a rate: 0.1 is 10%.
            _ when value != 0 && Math.Abs(value) < 1 => Signed(value * 100) + "% " + name,
            _ => Signed(value) + " " + name,
        };
    }

    private static string Signed(float value) => (value < 0 ? string.Empty : "+") + Number(value);

    private static string Number(float value) => value.ToString("0.##", CultureInfo.InvariantCulture);
}
