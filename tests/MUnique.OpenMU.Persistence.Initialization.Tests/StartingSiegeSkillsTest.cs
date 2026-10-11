// <copyright file="StartingSiegeSkillsTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Tests;

using Microsoft.Extensions.Logging.Abstractions;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.Persistence.Initialization.PlugIns.CharacterCreated;
using MUnique.OpenMU.Persistence.Initialization.Updates;
using MUnique.OpenMU.Persistence.InMemory;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Tests the starting skill grants, reference damage and existing database update.
/// </summary>
[TestFixture]
internal class StartingSiegeSkillsTest
{
    private static readonly (byte ClassNumber, short SkillNumber, int Damage)[] StartingSkills =
    [
        (4, 44, 90), (0, 45, 90), (8, 46, 120), (12, 57, 75),
        (16, 74, 150), (20, 45, 90), (24, 269, 90),
    ];

    /// <summary>
    /// Verifies the seeded configuration grants each starting skill once and
    /// matches the base damage of MuMain's supplied English skill data.
    /// </summary>
    [Test]
    public async Task FreshConfigurationGrantsSkillsWithReferenceDamageAsync()
    {
        var (context, configuration) = await CreateConfigurationAsync().ConfigureAwait(false);
        using var _ = context;
        AssertGrants(context, configuration);
        foreach (var (_, skillNumber, damage) in StartingSkills)
        {
            Assert.That(configuration.Skills.Single(skill => skill.Number == skillNumber).AttackDamage, Is.EqualTo(damage));
        }
    }

    /// <summary>
    /// Repairs missing or disabled grants and wizard eligibility without creating duplicates.
    /// </summary>
    [Test]
    public async Task ExistingConfigurationIsRepairedIdempotentlyAsync()
    {
        var (context, configuration) = await CreateConfigurationAsync().ConfigureAwait(false);
        using var _ = context;
        var wizard = configuration.CharacterClasses.Single(characterClass => characterClass.Number == 0);
        configuration.Skills.Single(skill => skill.Number == 45).QualifiedCharacters.Remove(wizard);
        var wizardGrant = configuration.PlugInConfigurations.Single(entry => entry.TypeId == typeof(AddLanceForDarkWizard).GUID);
        configuration.PlugInConfigurations.Remove(wizardGrant);
        configuration.PlugInConfigurations.Single(entry => entry.TypeId == typeof(AddCrescentMoonSlashForDarkKnight).GUID).IsActive = false;

        var update = new EnableStartingSiegeSkillsPlugIn();
        await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        var count = configuration.PlugInConfigurations.Count;
        await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        Assert.That(configuration.PlugInConfigurations.Count, Is.EqualTo(count));
        Assert.That(configuration.Skills.Single(skill => skill.Number == 45).QualifiedCharacters.Count(entry => entry.Number == 0), Is.EqualTo(1));
        AssertGrants(context, configuration);
    }

    private static void AssertGrants(IContext context, GameConfiguration configuration)
    {
        var manager = new PlugInManager(configuration.PlugInConfigurations, NullLoggerFactory.Instance, null, null);
        var grants = manager.GetActivePlugInsOf<GameLogic.PlugIns.ICharacterCreatedPlugIn>().OfType<AddInitialSkillPlugInBase>().ToArray();
        foreach (var (classNumber, skillNumber, _) in StartingSkills)
        {
            var account = context.CreateNew<Account>();
            var character = context.CreateNew<Character>();
            character.CharacterClass = configuration.CharacterClasses.Single(entry => entry.Number == classNumber);
            account.Characters.Add(character);
            for (int invocation = 0; invocation < 2; invocation++)
            {
                foreach (var grant in grants)
                {
                    grant.CharacterCreated(account, character, context, configuration, NullLogger.Instance);
                }
            }

            Assert.That(character.LearnedSkills.Count(entry => entry.Skill?.Number == skillNumber), Is.EqualTo(1), $"Starting skill {skillNumber} for class {classNumber}");
        }
    }

    private static async Task<(IContext Context, GameConfiguration Configuration)> CreateConfigurationAsync()
    {
        var provider = new InMemoryPersistenceContextProvider();
        var initializer = new VersionSeasonSix.DataInitialization(provider, NullLoggerFactory.Instance);
        await initializer.CreateInitialDataAsync(1, true).ConfigureAwait(false);
        var context = provider.CreateNewContext();
        var configuration = (await context.GetAsync<GameConfiguration>().ConfigureAwait(false)).First();
        return (context, configuration);
    }
}
