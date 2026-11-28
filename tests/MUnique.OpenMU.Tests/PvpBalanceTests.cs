// <copyright file="PvpBalanceTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using Moq;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.Pvp;
using MUnique.OpenMU.Persistence.Initialization.CharacterClasses;
using MUnique.OpenMU.PlugIns;
using BasicModel = MUnique.OpenMU.Persistence.BasicModel;

/// <summary>Verifies hit chance and class tuning in the server's PvP damage calculation.</summary>
[TestFixture]
internal sealed class PvpBalanceTests
{
    [TestCase(1000f, 0f, 1f)]
    [TestCase(1000f, 950f, 0.05f)]
    [TestCase(1000f, 985f, 0.03f)]
    [TestCase(1000f, 1000f, 0.03f)]
    [TestCase(1000f, 1015f, 0.03f)]
    [TestCase(0f, 0f, 0.03f)]
    public void HitChanceKeepsItsFloorAcrossTheRateBoundary(float attackRate, float defenseRate, float expected)
    {
        Assert.That(AttackableExtensions.GetPvpHitChance(attackRate, defenseRate), Is.EqualTo(expected).Within(0.00001));
    }

    [TestCase(CharacterClassNumber.DarkWizard, CharacterClassNumber.GrandMaster)]
    [TestCase(CharacterClassNumber.DarkKnight, CharacterClassNumber.BladeMaster)]
    [TestCase(CharacterClassNumber.FairyElf, CharacterClassNumber.HighElf)]
    [TestCase(CharacterClassNumber.MagicGladiator, CharacterClassNumber.DuelMaster)]
    [TestCase(CharacterClassNumber.DarkLord, CharacterClassNumber.LordEmperor)]
    [TestCase(CharacterClassNumber.Summoner, CharacterClassNumber.DimensionMaster)]
    [TestCase(CharacterClassNumber.RageFighter, CharacterClassNumber.FistMaster)]
    public async Task PromotionUsesFamilyTuningAgainstPlayersAsync(CharacterClassNumber family, CharacterClassNumber promotion)
    {
        var settings = CreatePlugInConfiguration(isActive: true, new PvpBalanceSettings
        {
            ClassRates = [new() { BaseClassNumber = (byte)family, DamagePercent = 75 }],
        });
        var context = GameContextTestHelper.CreateGameContext([settings]);
        await using var attacker = await PlayerTestHelper.CreatePlayerAsync(context).ConfigureAwait(false);
        await using var defender = await PlayerTestHelper.CreatePlayerAsync(context).ConfigureAwait(false);

        var promoted = new BasicModel.CharacterClass { Number = (byte)promotion };
        context.Configuration.CharacterClasses.Add(new BasicModel.CharacterClass { Number = (byte)family, NextGenerationClass = promoted });
        context.Configuration.CharacterClasses.Add(promoted);
        attacker.SelectedCharacter!.CharacterClass = promoted;
        SetDamageAndHitRates(attacker, defender);

        var hit = await attacker.CalculateDamageAsync(defender, null, false).ConfigureAwait(false);
        Assert.That(hit.ShieldDamage, Is.EqualTo(75));
        Assert.That(hit.HealthDamage, Is.Zero);

        var monster = new Mock<IAttackable>();
        var monsterAttributes = new AttributeSystem([], [], []);
        monsterAttributes.AddElement(new SimpleElement(1, AggregateType.AddRaw), Stats.DamageReceiveDecrement);
        monster.SetupGet(target => target.Attributes).Returns(monsterAttributes);
        var pvmHit = await attacker.CalculateDamageAsync(monster.Object, null, false).ConfigureAwait(false);
        Assert.That(pvmHit.TotalDamage, Is.EqualTo(100));
    }

    [TestCase(false, 75, 100)]
    [TestCase(true, null, 100)]
    [TestCase(true, 0, 100)]
    [TestCase(true, 125, 125)]
    public async Task UsesOnlyActivePositiveSettingsAsync(bool active, int? percent, int expected)
    {
        var settings = new PvpBalanceSettings { ClassRates = [] };
        if (percent is { } configuredPercent)
        {
            settings.ClassRates.Add(new PvpClassRate
            {
                BaseClassNumber = (byte)CharacterClassNumber.DarkWizard,
                DamagePercent = configuredPercent,
            });
        }

        var context = GameContextTestHelper.CreateGameContext([CreatePlugInConfiguration(active, settings)]);
        await using var attacker = await PlayerTestHelper.CreatePlayerAsync(context).ConfigureAwait(false);
        await using var defender = await PlayerTestHelper.CreatePlayerAsync(context).ConfigureAwait(false);
        SetDamageAndHitRates(attacker, defender);

        var hit = await attacker.CalculateDamageAsync(defender, null, false).ConfigureAwait(false);
        Assert.That(hit.TotalDamage, Is.EqualTo(expected));
    }

    [TestCase(false, 5884, 0.03)]
    [TestCase(true, 7060.8, 0.14)]
    public async Task NonResetPresetRaisesFistMasterAccuracyOnlyWhenSavedAsync(bool saveSettings, double expectedRate, double expectedChance)
    {
        var settings = new PlugInConfiguration { TypeId = typeof(PvpBalanceFeaturePlugIn).GUID, IsActive = true };
        if (saveSettings)
        {
            settings.SetConfiguration(new PvpBalanceSettings(), referenceHandler: null);
        }

        var context = GameContextTestHelper.CreateGameContext([settings]);
        await using var player = await PlayerTestHelper.CreatePlayerAsync(context).ConfigureAwait(false);
        var fistMaster = new BasicModel.CharacterClass { Number = (byte)CharacterClassNumber.FistMaster };
        context.Configuration.CharacterClasses.Add(new BasicModel.CharacterClass
        {
            Number = (byte)CharacterClassNumber.RageFighter,
            NextGenerationClass = fistMaster,
        });
        context.Configuration.CharacterClasses.Add(fistMaster);
        player.SelectedCharacter!.CharacterClass = fistMaster;
        player.Attributes!.AddElement(new SimpleElement(5884, AggregateType.AddRaw), Stats.AttackRatePvp);

        var adjustedRate = CharacterCombatStatistics.Create(player)["Attack rate (PvP)"];
        var classRate = PvpBalanceFeaturePlugIn.GetClassRate(player);
        Assert.That(classRate?.DamagePercent ?? 100, Is.EqualTo(100));
        Assert.That(adjustedRate, Is.EqualTo(expectedRate).Within(0.01));
        Assert.That(AttackableExtensions.GetPvpHitChance(adjustedRate, 6073), Is.EqualTo(expectedChance).Within(0.001));
    }

    private static PlugInConfiguration CreatePlugInConfiguration(bool isActive, PvpBalanceSettings settings)
    {
        var plugIn = new PlugInConfiguration { TypeId = typeof(PvpBalanceFeaturePlugIn).GUID, IsActive = isActive };
        plugIn.SetConfiguration(settings, referenceHandler: null);
        return plugIn;
    }

    private static void SetDamageAndHitRates(Player attacker, Player defender)
    {
        var attributes = attacker.Attributes!;
        attributes.AddElement(new SimpleElement(100, AggregateType.AddRaw), Stats.MinimumPhysBaseDmg);
        attributes.AddElement(new SimpleElement(100, AggregateType.AddRaw), Stats.MaximumPhysBaseDmg);
        attributes.AddElement(new SimpleElement(100, AggregateType.AddRaw), Stats.AttackRatePvp);
        attributes.AddElement(new SimpleElement(100, AggregateType.AddRaw), Stats.AttackRatePvm);
        defender.Attributes![Stats.CurrentShield] = 200;
    }
}
