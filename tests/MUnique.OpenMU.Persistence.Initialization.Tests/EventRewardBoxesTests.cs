// <copyright file="EventRewardBoxesTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Tests;

using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.Persistence.Initialization.Updates;

/// <summary>Checks default event rewards, repair of fireworks-only data, and preservation of customized drops.</summary>
[TestFixture]
public class EventRewardBoxesTests
{
    /// <summary>All five event boxes and both firecracker representations have configurable rewards on fresh servers.</summary>
    [Test]
    public async Task FreshServerHasEventRewardsAsync()
    {
        var (context, configuration) = await TestGameConfiguration.CreateSeason6Async().ConfigureAwait(false);
        using (context)
        {
            var box = configuration.Items.Single(item => item.Group == 14 && item.Number == 11);
            foreach (var level in new byte[] { 2, 3, 5, 6, 7 })
            {
                Assert.That(box.DropItems.Any(group => group.SourceItemLevel == level && group.PossibleItems.Count > 0), Is.True);
            }

            var firecracker = configuration.Items.Single(item => item.Group == 14 && item.Number == 63);
            Assert.That(firecracker.DropItems, Has.Count.EqualTo(box.DropItems.Count(group => group.SourceItemLevel == 2)));
            Assert.That(firecracker.DropItems.Any(group => group.PossibleItems.Count > 0), Is.True);
            Assert.That(firecracker.DropItems.All(group => group.SourceItemLevel == 0 && group.DropEffect == ItemDropEffect.Fireworks), Is.True);
            Assert.That(firecracker.DropItems.Select(group => group.Chance), Is.EqualTo(box.DropItems
                .Where(group => group.SourceItemLevel == 2).Select(group => group.Chance)));
        }
    }

    /// <summary>Existing decoration-only firecrackers are repaired once and later operator edits survive repeated updates.</summary>
    [Test]
    public async Task UpgradeRepairsDecorationAndKeepsCustomRewardsAsync()
    {
        var (context, configuration) = await TestGameConfiguration.CreateSeason6Async().ConfigureAwait(false);
        using (context)
        {
            var firecracker = configuration.Items.Single(item => item.Group == 14 && item.Number == 63);
            firecracker.DropItems.Clear();
            var effect = context.CreateNew<ItemDropItemGroup>();
            effect.Chance = 1;
            effect.DropEffect = ItemDropEffect.Fireworks;
            firecracker.DropItems.Add(effect);
            var update = new EventRewardBoxesUpdatePlugIn();
            await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
            var rewards = firecracker.DropItems.ToArray();
            Assert.That(rewards.Any(group => group.PossibleItems.Count > 0), Is.True);
            rewards[0].HasLuck = true;
            rewards[0].ExcellentOptions = 5;
            rewards[0].Chance = .125;
            await update.ApplyUpdateAsync(context, configuration).ConfigureAwait(false);
            Assert.That(firecracker.DropItems, Is.EqualTo(rewards));
            Assert.That(rewards[0].HasLuck, Is.True);
            Assert.That(rewards[0].ExcellentOptions, Is.EqualTo(5));
            Assert.That(rewards[0].Chance, Is.EqualTo(.125));
        }
    }
}
