// <copyright file="HorseMountSkillsTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Tests;

using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.Cosmetics;
using MUnique.OpenMU.GameLogic.Pets;
using MUnique.OpenMU.Persistence.InMemory;
using MUnique.OpenMU.Persistence.Initialization.Updates;
using MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Items;

/// <summary>Checks fresh and upgraded horse mounts without changing their equipment identity.</summary>
[TestFixture]
internal class HorseMountSkillsTests
{
    /// <summary>Disabling the configured horse skill must not give imported horses Plasma Storm.</summary>
    [Test]
    public void DisabledHorseSkillDoesNotBecomePlasmaStorm()
    {
        var provider = new InMemoryPersistenceContextProvider();
        using var context = provider.CreateNewContext();
        var configuration = context.CreateNew<GameConfiguration>();
        FenrirMountFixture.Add(context, configuration);
        configuration.Items.Single(item => item.Group == 13 && item.Number == 4).Skill = null;
        CosmeticArmorItems.Ensure(context, configuration);
        Assert.That(configuration.Items.Single(item => item.Group == 13 && item.Number == 309).Skill, Is.Null);
        Assert.That(configuration.Items.Single(item => item.Group == 13 && item.Number == 369).Skill, Is.Null);
        Assert.That(configuration.Items.Single(item => item.Group == 13 && item.Number == 37).Skill, Is.Not.Null);
    }

    /// <summary>Both horse models receive Earthquake, including when upgrading old Plasma Storm items.</summary>
    /// <param name="number">The imported horse item number.</param>
    [TestCase(309)]
    [TestCase(369)]
    public async Task HorseSkillSurvivesConfigurationUpgradeAsync(short number)
    {
        var provider = new InMemoryPersistenceContextProvider();
        using var context = provider.CreateNewContext();
        var configuration = context.CreateNew<GameConfiguration>();
        FenrirMountFixture.Add(context, configuration);
        CosmeticArmorItems.Ensure(context, configuration);
        var horse = configuration.Items.Single(item => item.Group == 13 && item.Number == number);
        var reference = configuration.Items.Single(item => item.Group == 13 && item.Number == 4);
        Assert.That(horse.Skill, Is.SameAs(reference.Skill));
        Assert.That(horse.Skill!.QualifiedCharacters, Is.SupersetOf(horse.QualifiedCharacters));
        Assert.That(FenrirMountRules.IsDarkHorse(horse), Is.True);
        Assert.That(FenrirMountRules.IsCustom(horse), Is.True);
        Assert.That(CompanionEquipmentRules.IsOrdinaryPet(horse), Is.False);

        horse.Skill = configuration.Items.Single(item => item.Group == 13 && item.Number == 37).Skill;
        reference.Skill!.QualifiedCharacters.Clear();
        foreach (var power in horse.BasePowerUpAttributes.Where(power => power.TargetAttribute?.Id == Stats.IsHorseEquipped.Id).ToArray())
        {
            horse.BasePowerUpAttributes.Remove(power);
        }

        var update = new CosmeticArmorItemsUpdatePlugIn();
        await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        Assert.That(configuration.Items.Single(item => item.Group == 13 && item.Number == number), Is.SameAs(horse));
        Assert.That(horse.Skill, Is.SameAs(reference.Skill));
        Assert.That(horse.Skill!.QualifiedCharacters, Is.SupersetOf(horse.QualifiedCharacters));
        Assert.That(horse.Skill.QualifiedCharacters.Distinct().Count(), Is.EqualTo(horse.Skill.QualifiedCharacters.Count));
        Assert.That(horse.BasePowerUpAttributes.Count(power => power.TargetAttribute?.Id == Stats.IsHorseEquipped.Id), Is.EqualTo(1));
        Assert.That(horse.ItemSlot!.ItemSlots, Does.Contain(DataModel.InventoryConstants.PetSlot));
    }
}
