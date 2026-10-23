// <copyright file="LevelBasedMoneyPlugInTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.Persistence.BasicModel;

/// <summary>
/// Checks the hardcore Zen range and configuration boundaries.
/// </summary>
[TestFixture]
public class LevelBasedMoneyPlugInTest
{
    /// <summary>
    /// Checks that default Zen scales by monster level and ignores the original EXP-based amount.
    /// </summary>
    /// <param name="level">The monster level.</param>
    /// <param name="minimum">The expected minimum Zen.</param>
    /// <param name="maximum">The expected maximum Zen.</param>
    [TestCase(4, 500, 600)]
    [TestCase(36, 2900, 3000)]
    [TestCase(100, 7700, 7800)]
    [TestCase(200, 15200, 15300)]
    public async Task DefaultRangeAsync(int level, int minimum, int maximum)
    {
        var monster = new MonsterDefinition { Id = Guid.NewGuid() };
        monster.Attributes.Add(new MonsterAttribute { AttributeDefinition = Stats.Level, Value = level });
        var plugIn = new LevelBasedMoneyPlugIn();
        for (var i = 0; i < 100; i++)
        {
            var args = new MonsterMoneyAmountArgs(monster, 1_000_000);
            await plugIn.CalculateMoneyAmountAsync(args).ConfigureAwait(false);
            Assert.That(args.Amount, Is.InRange(minimum, maximum));
        }
    }

    /// <summary>
    /// Checks fixed and zero ranges, including the supported upper bound.
    /// </summary>
    /// <param name="rate">The fixed Zen per level.</param>
    [TestCase(0)]
    [TestCase(7)]
    [TestCase(int.MaxValue)]
    public async Task FixedRangeAsync(int rate)
    {
        var monster = new MonsterDefinition { Id = Guid.NewGuid() };
        monster.Attributes.Add(new MonsterAttribute { AttributeDefinition = Stats.Level, Value = ushort.MaxValue });
        var plugIn = new LevelBasedMoneyPlugIn
        {
            Configuration = new() { BaseZen = 0, ZenPerMonsterLevel = rate, MaximumRandomZen = 0 },
        };
        var args = new MonsterMoneyAmountArgs(monster, 1_000_000);
        await plugIn.CalculateMoneyAmountAsync(args).ConfigureAwait(false);
        Assert.That(args.Amount, Is.EqualTo((uint)Math.Min(int.MaxValue - 1L, (long)ushort.MaxValue * rate)));
    }

    /// <summary>
    /// Rejects invalid ranges even when configuration bypasses the admin form.
    /// </summary>
    /// <param name="baseZen">The configured base Zen.</param>
    /// <param name="perLevel">The configured Zen per monster level.</param>
    /// <param name="randomZen">The configured maximum random Zen.</param>
    [TestCase(-1, 75, 100)]
    [TestCase(200, -1, 100)]
    [TestCase(200, 75, -1)]
    public void InvalidRange(int baseZen, int perLevel, int randomZen)
    {
        var plugIn = new LevelBasedMoneyPlugIn
        {
            Configuration = new() { BaseZen = baseZen, ZenPerMonsterLevel = perLevel, MaximumRandomZen = randomZen },
        };
        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await plugIn.CalculateMoneyAmountAsync(new MonsterMoneyAmountArgs(new MonsterDefinition(), 1)).ConfigureAwait(false));
    }
}
