// <copyright file="CombatResistanceTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.Attributes;
using BasicModel = MUnique.OpenMU.Persistence.BasicModel;

/// <summary>Verifies the resistance chances and the values shown by Combat Statistics.</summary>
[TestFixture]
internal class CombatResistanceTests
{
    [TestCase(0.5f, 0f, 0.5)]
    [TestCase(0.5f, 0.2f, 0.4)]
    [TestCase(1f, 1f, 0)]
    [TestCase(1.5f, 0.5f, 0.5)]
    public void DamageEffectChanceAccountsForResistance(float chance, float resistance, double expected)
        => Assert.That(AttackableExtensions.ResistedDamageChance(chance, resistance), Is.EqualTo(expected).Within(0.00001));

    [Test]
    public async Task SnapshotIncludesLiveChancesAndElementRatingsAsync()
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        var attributes = player.Attributes!;
        attributes.AddElement(new SimpleElement(0.25f, AggregateType.AddRaw), Stats.ExcellentDamageResistance);
        attributes.AddElement(new SimpleElement(0.5f, AggregateType.AddRaw), Stats.IgnoreDefenseResistance);
        attributes.AddElement(new SimpleElement(14f, AggregateType.AddRaw), Stats.EarthResistance);
        attributes.AddElement(new SimpleElement(7f, AggregateType.AddRaw), Stats.WindResistance);
        attributes.AddElement(new SimpleElement(33f, AggregateType.AddRaw), Stats.IceResistance);

        var statistics = CharacterCombatStatistics.Create(player);
        Assert.That(statistics["Excellent damage resistance (%)"], Is.EqualTo(25));
        Assert.That(statistics["Ignore defense resistance (%)"], Is.EqualTo(50));
        Assert.That(statistics["Earth resistance"], Is.EqualTo(14));
        Assert.That(statistics["Wind resistance"], Is.EqualTo(7));
        Assert.That(statistics["Ice resistance"], Is.EqualTo(33));
    }

    [Test]
    public async Task MasterDefenseRateShowsActualLearnedBonusAsync()
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        var masterSkill = new BasicModel.MasterSkillDefinition { MaximumLevel = 20, ValueFormula = "614.08 * level" };
        var boost = new BasicModel.PowerUpDefinitionValue();
        boost.ConstantValue.AggregateType = AggregateType.AddRaw;
        boost.RelatedValues.Add(new BasicModel.AttributeRelationship
        {
            InputAttribute = Stats.MasterSkillValue,
            InputOperand = 1,
            InputOperator = InputOperator.Multiply,
        });
        masterSkill.PassivePowerUps.Add(new BasicModel.PowerUpDefinition { TargetAttribute = Stats.DefenseRatePvp, Boost = boost });
        var skill = new BasicModel.Skill { Number = 300, SkillType = SkillType.PassiveBoost, MasterDefinition = masterSkill };
        var skills = player.SkillList!;
        await skills.AddLearnedSkillAsync(skill).ConfigureAwait(false);
        Assert.That(CharacterCombatStatistics.MasterBonuses(player), Is.Empty);

        skills.GetSkill(300)!.Level = 1;
        Assert.That(CharacterCombatStatistics.Create(player)["Defense rate (PvP)"], Is.EqualTo(614.08f).Within(0.001f));
        Assert.That(CharacterCombatStatistics.MasterBonuses(player).Single().Text, Does.Contain("+614.08"));

        skills.GetSkill(300)!.Level = 2;
        Assert.That(CharacterCombatStatistics.MasterBonuses(player).Single().Text, Does.Contain("+1228.16"));
    }
}
