// <copyright file="GroundAreaSkillHitChanceTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.PlayerActions.Skills;
using Moq;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.Pathfinding;

/// <summary>
/// Checks that an accurately aimed ground spell does not lose hits merely
/// because its caster is standing farther from the fire.
/// </summary>
[TestFixture]
public class GroundAreaSkillHitChanceTest
{
    /// <summary>All overlapping and adjacent monsters are selected, while distant monsters stay untouched.</summary>
    [Test]
    public async Task FlameSelectsEveryMonsterInsideItsImpactAsync()
    {
        var context = GameContextTestHelper.CreateGameContext();
        var player = await PlayerTestHelper.CreatePlayerAsync(context).ConfigureAwait(false);
        var map = await context.GetMapAsync(0).ConfigureAwait(false);
        var expected = new List<IAttackable>();
        foreach (var position in new[] { new Point(100, 100), new Point(100, 100), new Point(101, 100), new Point(101, 101), new Point(102, 100) })
        {
            var target = new Mock<IAttackable>();
            var identifiable = target.As<ISupportIdUpdate>();
            identifiable.SetupProperty(t => t.Id);
            target.SetupGet(t => t.Id).Returns(() => identifiable.Object.Id);
            target.SetupProperty(t => t.Position, position);
            target.SetupGet(t => t.CurrentMap).Returns(map);
            target.SetupGet(t => t.IsAlive).Returns(true);
            await map!.AddAsync(target.Object).ConfigureAwait(false);
            if (position.X < 102) expected.Add(target.Object);
        }

        var skill = new Mock<Skill>();
        skill.SetupAllProperties();
        skill.Object.Range = 6;
        skill.Object.SkillType = SkillType.AreaSkillAutomaticHits;
        skill.Object.AreaSkillSettings = new AreaSkillSettings { UseTargetAreaFilter = true, TargetAreaDiameter = 3 };
        var targets = AreaSkillAttackAction.GetTargets(player, new Point(100, 100), skill.Object, 0, ushort.MaxValue).ToList();
        Assert.That(targets, Is.EquivalentTo(expected));
    }

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
