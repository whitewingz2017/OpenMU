// <copyright file="GroundAreaSkillHitChanceTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.PlayerActions.Skills;

/// <summary>
/// Checks that an accurately aimed ground spell does not lose hits merely
/// because its caster is standing farther from the fire.
/// </summary>
[TestFixture]
public class GroundAreaSkillHitChanceTest
{
    /// <summary>
    /// Standing in the center of Flame has the same chance at melee and maximum range.
    /// </summary>
    [TestCase(1)]
    [TestCase(6)]
    public void AimedGroundSpellUsesItsImpactArea(double casterDistance)
    {
        var settings = new AreaSkillSettings { UseTargetAreaFilter = true, HitChancePerDistanceMultiplier = 0.5f };
        Assert.That(AreaSkillAttackAction.CalculateDistanceHitChance(settings, casterDistance, 0), Is.EqualTo(1));
        Assert.That(AreaSkillAttackAction.CalculateDistanceHitChance(settings, casterDistance, 1), Is.EqualTo(0.5));
    }

    /// <summary>
    /// Caster-origin spells retain their configured distance falloff.
    /// </summary>
    [Test]
    public void OtherAreaSpellsKeepCasterDistance()
    {
        var settings = new AreaSkillSettings { HitChancePerDistanceMultiplier = 0.5f };
        Assert.That(AreaSkillAttackAction.CalculateDistanceHitChance(settings, 6, 0), Is.EqualTo(1d / 64));
    }
}
