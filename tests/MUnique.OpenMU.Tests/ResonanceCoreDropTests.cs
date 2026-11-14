// <copyright file="ResonanceCoreDropTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using Moq;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.PlayerActions.Items;

/// <summary>
/// Tests the <see cref="ResonanceCoreDropPlugIn"/>.
/// </summary>
[TestFixture]
internal class ResonanceCoreDropTests
{
    private const short Kundun = 275;

    private const short Balgass = 349;

    private const short Goblin = 26;

    /// <summary>
    /// Tests that a boss drops at most one core per day, and again on the next day.
    /// </summary>
    [Test]
    public void BossDropsOncePerDay()
    {
        var today = new DateOnly(2026, 11, 14);
        var plugIn = Create(0.0, () => today);

        Assert.That(plugIn.ShouldDrop(Kundun), Is.True);
        Assert.That(plugIn.ShouldDrop(Kundun), Is.False);
        Assert.That(plugIn.ShouldDrop(Balgass), Is.True);

        today = today.AddDays(1);
        Assert.That(plugIn.ShouldDrop(Kundun), Is.True);
    }

    /// <summary>
    /// Tests that a failed roll doesn't use up the drop of the day, so a later kill can still drop it.
    /// </summary>
    [Test]
    public void BossDropIsNotBoundToTheFirstKill()
    {
        var rolls = new Queue<double>([0.5, 0.0]);
        var plugIn = Create(rolls.Dequeue, () => new DateOnly(2026, 11, 14));

        Assert.That(plugIn.ShouldDrop(Kundun), Is.False);
        Assert.That(plugIn.ShouldDrop(Kundun), Is.True);
    }

    /// <summary>
    /// Tests the chance of other monsters, which have no daily limit.
    /// </summary>
    [Test]
    public void MonsterDropUsesConfiguredChance()
    {
        var plugIn = Create(0.0000009, () => new DateOnly(2026, 11, 14));
        Assert.That(plugIn.ShouldDrop(Goblin), Is.True);
        Assert.That(plugIn.ShouldDrop(Goblin), Is.True);

        plugIn = Create(0.000001, () => new DateOnly(2026, 11, 14));
        Assert.That(plugIn.ShouldDrop(Goblin), Is.False);
    }

    private static ResonanceCoreDropPlugIn Create(double roll, Func<DateOnly> today) => Create(() => roll, today);

    private static ResonanceCoreDropPlugIn Create(Func<double> roll, Func<DateOnly> today)
    {
        var randomizer = new Mock<IRandomizer>();
        randomizer.Setup(r => r.NextDouble()).Returns(roll);
        return new ResonanceCoreDropPlugIn(randomizer.Object, today, new Dictionary<short, DateOnly>())
        {
            Configuration = new ResonanceCoreDropConfiguration(),
        };
    }
}
