// <copyright file="SupportBuffDurations.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Skills;

using MUnique.OpenMU.DataModel.Attributes;

/// <summary>Configures one-minute support buffs without changing their strength.</summary>
internal static class SupportBuffDurations
{
    /// <summary>The requested duration of player-cast support effects, in seconds.</summary>
    internal const float DurationSeconds = 60;

    /// <summary>Applies the duration to regular and master support buffs.</summary>
    /// <param name="context">The persistence context.</param>
    /// <param name="configuration">The game configuration.</param>
    internal static void Apply(IContext context, GameConfiguration configuration)
    {
        var buffs = configuration.Skills.Where(skill => skill.SkillType == SkillType.Buff).ToArray();
        foreach (var effect in buffs.Select(skill => skill.MagicEffectDef).OfType<MagicEffectDefinition>().Distinct())
        {
            effect.Duration = SetDuration(effect.Duration);
            if (effect.DurationPvp is { } durationPvp)
            {
                SetDuration(durationPvp);
            }

            effect.DurationDependsOnTargetLevel = false;
        }

        foreach (var skill in buffs)
        {
            if (skill.MasterDefinition is { } master)
            {
                master.ExtendsDuration = false;
            }
        }

        PowerUpDefinitionValue SetDuration(PowerUpDefinitionValue? duration)
        {
            duration ??= context.CreateNew<PowerUpDefinitionValue>();
            duration.ConstantValue.Value = DurationSeconds;
            duration.MaximumValue = DurationSeconds;
            duration.RelatedValues.Clear();
            return duration;
        }
    }
}
