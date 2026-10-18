// <copyright file="MuHelperPaymentTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.MuHelper;

/// <summary>
/// Verifies server-authoritative helper payments and insufficient-funds rejection.
/// </summary>
[TestFixture]
public class MuHelperPaymentTest
{
    /// <summary>
    /// Starting a level-400 helper deducts the configured 8,000 Zen once.
    /// </summary>
    [Test]
    public async ValueTask StartingChargesOnceAsync()
    {
        var context = GameContextTestHelper.CreateGameContext();
        var feature = context.FeaturePlugIns.GetPlugIn<MuHelperFeaturePlugIn>()!;
        feature.Configuration = new MuHelperConfiguration();
        var player = await PlayerTestHelper.CreatePlayerAsync(context).ConfigureAwait(false);
        player.Attributes![Stats.Level] = 400;
        player.Money = 20_000;
        try
        {
            Assert.That(await player.MuHelper.TryStartAsync().ConfigureAwait(false), Is.True);
            Assert.That(player.Money, Is.EqualTo(12_000));
            Assert.That(await player.MuHelper.TryStartAsync().ConfigureAwait(false), Is.False);
            Assert.That(player.Money, Is.EqualTo(12_000));
        }
        finally
        {
            await player.MuHelper.StopAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Insufficient funds prevent starting and do not remove any money.
    /// </summary>
    [Test]
    public async ValueTask InsufficientZenRejectsStartAsync()
    {
        var context = GameContextTestHelper.CreateGameContext();
        context.FeaturePlugIns.GetPlugIn<MuHelperFeaturePlugIn>()!.Configuration = new MuHelperConfiguration();
        var player = await PlayerTestHelper.CreatePlayerAsync(context).ConfigureAwait(false);
        player.Attributes![Stats.Level] = 400;
        player.Money = 7_999;
        Assert.That(await player.MuHelper.TryStartAsync().ConfigureAwait(false), Is.False);
        Assert.That(player.Money, Is.EqualTo(7_999));
    }
}
