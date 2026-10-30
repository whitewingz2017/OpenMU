// <copyright file="CharacterCombatStatistics.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Attributes;

/// <summary>Reads the live combat totals used by the server, including equipment, skins and buffs.</summary>
public static class CharacterCombatStatistics
{
    /// <summary>Creates a display snapshot. Chance and multiplier values are expressed as percentages.</summary>
    public static IReadOnlyDictionary<string, float> Create(Player player)
    {
        if (player.Attributes is not { } attributes)
        {
            return new Dictionary<string, float>();
        }

        return new Dictionary<string, float>
        {
            ["Physical damage minimum"] = attributes[Stats.MinimumPhysBaseDmg],
            ["Physical damage maximum"] = attributes[Stats.MaximumPhysBaseDmg],
            ["Wizardry damage minimum"] = attributes[Stats.MinimumWizBaseDmg],
            ["Wizardry damage maximum"] = attributes[Stats.MaximumWizBaseDmg],
            ["Attack speed"] = attributes[Stats.AttackSpeed],
            ["Magic speed"] = attributes[Stats.MagicSpeed],
            ["Excellent damage rate (%)"] = attributes[Stats.ExcellentDamageChance] * 100,
            ["Critical damage rate (%)"] = attributes[Stats.CriticalDamageChance] * 100,
            ["Double damage rate (%)"] = attributes[Stats.DoubleDamageChance] * 100,
            ["Ignore defense rate (%)"] = attributes[Stats.DefenseIgnoreChance] * 100,
            ["Damage reflection (%)"] = attributes[Stats.DamageReflection] * 100,
            ["Damage reduction (%)"] = attributes[Stats.ArmorDamageDecrease] * 100,
            ["Defense (PvM)"] = attributes[Stats.DefensePvm],
            ["Defense (PvP)"] = attributes[Stats.DefensePvp],
            ["Attack rate (PvM)"] = attributes[Stats.AttackRatePvm],
            ["Attack rate (PvP)"] = attributes[Stats.AttackRatePvp],
        };
    }
}
