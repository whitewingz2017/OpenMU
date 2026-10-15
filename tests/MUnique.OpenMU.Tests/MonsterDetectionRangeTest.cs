// <copyright file="MonsterDetectionRangeTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using Moq;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.NPC;
using MUnique.OpenMU.Pathfinding;

/// <summary>
/// Checks that an observer outside the configured aggro range is not acquired.
/// </summary>
[TestFixture]
public class MonsterDetectionRangeTest
{
    /// <summary>
    /// The range boundary is included, but observing from farther away does not aggro.
    /// </summary>
    [TestCase(7, true)]
    [TestCase(8, false)]
    [TestCase(15, false)]
    public void DetectionRespectsConfiguredBoundary(byte distance, bool acquired)
    {
        var viewer = CreateLocateable(100, 100);
        var candidate = CreateLocateable((byte)(100 + distance), 100);
        var selected = NpcTargetSelection.GetNearestPreferVisible(new[] { candidate }, viewer, target => target.GetDistanceTo(viewer), 7);
        Assert.That(selected is not null, Is.EqualTo(acquired));
    }

    private static ILocateable CreateLocateable(byte x, byte y)
    {
        var mock = new Mock<ILocateable>();
        mock.Setup(l => l.Position).Returns(new Point(x, y));
        return mock.Object;
    }
}
