// <copyright file="SafeZoneRecoveryTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Tests;

using Microsoft.Extensions.Logging.Abstractions;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.Persistence.Initialization.Updates;
using MUnique.OpenMU.Persistence.InMemory;

/// <summary>
/// Verifies starting resource values and safe-zone recovery for fresh and upgraded configurations.
/// </summary>
[TestFixture]
public class SafeZoneRecoveryTest
{
    /// <summary>
    /// New characters start with full computed health and mana, and recover both in town.
    /// </summary>
    [Test]
    public async Task FreshCharactersStartFullAndRecoverInSafezonesAsync()
    {
        var (context, configuration) = await CreateConfigurationAsync().ConfigureAwait(false);
        using var persistence = context;
        var account = context.CreateNew<Account>();
        foreach (var characterClass in configuration.CharacterClasses.Where(characterClass => characterClass.CanGetCreated))
        {
            var character = context.CreateNew<Character>();
            context.InitializeNewCharacter(character, characterClass);
            using var attributes = new ItemAwareAttributeSystem(account, character, configuration);
            Assert.That(attributes[Stats.CurrentHealth], Is.EqualTo(attributes[Stats.MaximumHealth]), characterClass.Name);
            Assert.That(attributes[Stats.CurrentMana], Is.EqualTo(attributes[Stats.MaximumMana]), characterClass.Name);
            attributes[Stats.IsResting] = 0;
            attributes[Stats.IsInSafezone] = 0;
            var outsideHealth = attributes[Stats.HealthRecoveryMultiplier];
            var outsideMana = attributes[Stats.ManaRecoveryMultiplier];
            attributes[Stats.IsInSafezone] = 1;
            Assert.That(attributes[Stats.HealthRecoveryMultiplier] - outsideHealth, Is.EqualTo(0.03f).Within(0.0001f));
            Assert.That(attributes[Stats.ManaRecoveryMultiplier] - outsideMana, Is.EqualTo(0.03f).Within(0.0001f));
        }
    }

    /// <summary>
    /// Upgrading version one adds the missing recovery once without replaying the old refactor.
    /// </summary>
    [Test]
    public async Task VersionOneUpgradeIsIdempotentAndPreservesRestingRelationshipsAsync()
    {
        var (context, configuration) = await CreateConfigurationAsync().ConfigureAwait(false);
        using var persistence = context;
        var update = new RegenerationsRefactorPlugInSeason6();
        var entry = (await context.GetAsync<ConfigurationUpdate>().ConfigureAwait(false)).Single(value => value.Key == update.Key);
        entry.Version = 1;
        foreach (var characterClass in configuration.CharacterClasses)
        {
            foreach (var relationship in characterClass.AttributeCombinations.Where(IsSafezoneRecovery).ToArray())
            {
                characterClass.AttributeCombinations.Remove(relationship);
            }
        }

        var restingCount = configuration.CharacterClasses.Sum(characterClass => characterClass.AttributeCombinations.Count(relationship => relationship.InputAttribute == Stats.IsResting));
        await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
        Assert.That(entry.Version, Is.EqualTo(2));
        Assert.That(configuration.CharacterClasses.Sum(characterClass => characterClass.AttributeCombinations.Count(relationship => relationship.InputAttribute == Stats.IsResting)), Is.EqualTo(restingCount));
        foreach (var characterClass in configuration.CharacterClasses)
        {
            Assert.That(characterClass.AttributeCombinations.Count(IsSafezoneRecovery), Is.EqualTo(2), characterClass.Name);
        }
    }

    private static bool IsSafezoneRecovery(MUnique.OpenMU.AttributeSystem.AttributeRelationship relationship) =>
        relationship.InputAttribute == Stats.IsInSafezone
        && (relationship.TargetAttribute == Stats.HealthRecoveryMultiplier || relationship.TargetAttribute == Stats.ManaRecoveryMultiplier);

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
