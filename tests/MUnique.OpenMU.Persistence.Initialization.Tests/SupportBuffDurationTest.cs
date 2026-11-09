// <copyright file="SupportBuffDurationTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Tests;

using Microsoft.Extensions.Logging.Abstractions;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Attributes;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Persistence.Initialization.Updates;
using MUnique.OpenMU.Persistence.InMemory;

/// <summary>Tests one-minute regular and master support buffs on fresh and existing databases.</summary>
[TestFixture]
public class SupportBuffDurationTest
{
    /// <summary>Fresh databases already have fixed durations.</summary>
    [Test]
    public async ValueTask FreshSupportBuffsLastOneMinuteAsync()
    {
        var provider = new InMemoryPersistenceContextProvider();
        var configuration = await CreateConfigurationAsync(provider).ConfigureAwait(false);
        AssertDurations(configuration);
    }

    /// <summary>The optional update repairs old formulas and missing durations without changing strength.</summary>
    [Test]
    public async ValueTask UpdateRepairsDurationsIdempotentlyAsync()
    {
        var provider = new InMemoryPersistenceContextProvider();
        var configuration = await CreateConfigurationAsync(provider).ConfigureAwait(false);
        using var context = provider.CreateNewContext();
        var buffs = configuration.Skills.Where(skill => skill.SkillType == SkillType.Buff).ToArray();
        var effects = buffs.Select(skill => skill.MagicEffectDef).OfType<MagicEffectDefinition>().Distinct().ToArray();
        var powers = effects.SelectMany(effect => effect.PowerUpDefinitions).Select(power => power.Boost).ToArray();
        var debuff = configuration.Skills.First(skill => skill.SkillType != SkillType.Buff && skill.MagicEffectDef?.Duration is not null).MagicEffectDef!;
        var debuffDuration = debuff.Duration!.ConstantValue.Value;
        foreach (var effect in effects)
        {
            effect.Duration = null;
            effect.DurationPvp = context.CreateNew<PowerUpDefinitionValue>();
            effect.DurationPvp.ConstantValue.Value = 123;
            effect.DurationPvp.RelatedValues.Add(context.CreateNew<AttributeRelationship>());
            effect.DurationDependsOnTargetLevel = true;
        }

        foreach (var master in buffs.Select(skill => skill.MasterDefinition).OfType<MasterSkillDefinition>())
        {
            master.ExtendsDuration = true;
        }

        var update = new SetSupportBuffDurationUpdatePlugIn();
        await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        AssertDurations(configuration);
        var durations = effects.Select(effect => effect.Duration).ToArray();
        await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        AssertDurations(configuration);
        Assert.That(effects.Select(effect => effect.Duration), Is.EqualTo(durations));
        Assert.That(effects.SelectMany(effect => effect.PowerUpDefinitions).Select(power => power.Boost), Is.EqualTo(powers));
        Assert.That(debuff.Duration.ConstantValue.Value, Is.EqualTo(debuffDuration));
    }

    private static async ValueTask<GameConfiguration> CreateConfigurationAsync(InMemoryPersistenceContextProvider provider)
    {
        await new VersionSeasonSix.DataInitialization(provider, new NullLoggerFactory()).CreateInitialDataAsync(1, true).ConfigureAwait(false);
        using var context = provider.CreateNewContext();
        return (await context.GetAsync<GameConfiguration>().ConfigureAwait(false)).First();
    }

    private static void AssertDurations(GameConfiguration configuration)
    {
        var buffs = configuration.Skills.Where(skill => skill.SkillType == SkillType.Buff).ToArray();
        Assert.That(buffs.Any(skill => skill.MasterDefinition is not null), Is.True);
        foreach (var skill in buffs.Where(skill => skill.MagicEffectDef is not null))
        {
            var effect = skill.MagicEffectDef!;
            Assert.That(effect.Duration!.ConstantValue.Value, Is.EqualTo(60), skill.ToString());
            Assert.That(effect.Duration.RelatedValues, Is.Empty, skill.ToString());
            Assert.That(effect.Duration.MaximumValue, Is.EqualTo(60), skill.ToString());
            Assert.That(effect.DurationDependsOnTargetLevel, Is.False, skill.ToString());
            if (effect.DurationPvp is { } pvp)
            {
                Assert.That(pvp.ConstantValue.Value, Is.EqualTo(60));
                Assert.That(pvp.RelatedValues, Is.Empty);
            }

            Assert.That(skill.MasterDefinition?.ExtendsDuration ?? false, Is.False, skill.ToString());
        }
    }
}
