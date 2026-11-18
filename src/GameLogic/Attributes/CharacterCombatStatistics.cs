// <copyright file="CharacterCombatStatistics.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Attributes;

using MUnique.OpenMU.AttributeSystem;

/// <summary>Reads the live combat totals used by the server, including equipment, skins and buffs.</summary>
public static class CharacterCombatStatistics
{
    /// <summary>Gets the statistics in their order: the label, the attribute and the factor of its display.</summary>
    /// <remarks>Chance and multiplier values are expressed as percentages.</remarks>
    public static IReadOnlyList<(string Label, AttributeDefinition Attribute, float Factor)> Rows { get; } =
    [
        ("Physical damage minimum", Stats.MinimumPhysBaseDmg, 1),
        ("Physical damage maximum", Stats.MaximumPhysBaseDmg, 1),
        ("Wizardry damage minimum", Stats.MinimumWizBaseDmg, 1),
        ("Wizardry damage maximum", Stats.MaximumWizBaseDmg, 1),
        ("Attack speed", Stats.AttackSpeed, 1),
        ("Magic speed", Stats.MagicSpeed, 1),
        ("Excellent damage rate (%)", Stats.ExcellentDamageChance, 100),
        ("Critical damage rate (%)", Stats.CriticalDamageChance, 100),
        ("Double damage rate (%)", Stats.DoubleDamageChance, 100),
        ("Ignore defense rate (%)", Stats.DefenseIgnoreChance, 100),
        ("Damage reflection (%)", Stats.DamageReflection, 100),
        ("Damage reduction (%)", Stats.ArmorDamageDecrease, 100),
        ("Defense (PvM)", Stats.DefensePvm, 1),
        ("Defense (PvP)", Stats.DefensePvp, 1),
        ("Attack rate (PvM)", Stats.AttackRatePvm, 1),
        ("Attack rate (PvP)", Stats.AttackRatePvp, 1),
    ];

    /// <summary>Creates a display snapshot. Chance and multiplier values are expressed as percentages.</summary>
    /// <param name="player">The player.</param>
    /// <returns>The values by label.</returns>
    public static Dictionary<string, float> Create(Player player)
    {
        if (player.Attributes is not { } attributes)
        {
            return new Dictionary<string, float>();
        }

        return Rows.ToDictionary(row => row.Label, row => attributes[row.Attribute] * row.Factor);
    }
}
