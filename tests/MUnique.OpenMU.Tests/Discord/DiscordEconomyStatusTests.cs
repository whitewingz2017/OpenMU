// <copyright file="DiscordEconomyStatusTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests.Discord;

using System.Globalization;
using MUnique.OpenMU.Discord;
using MUnique.OpenMU.Persistence;

/// <summary>Checks the combined Discord status message and countdown transitions.</summary>
[TestFixture]
public class DiscordEconomyStatusTests
{
    /// <summary>Jewel sections remain separate and use human-readable counts.</summary>
    [Test]
    public void StatusContainsBothJewelTotals()
    {
        var formatter = new DiscordStatusFormatter(CultureInfo.GetCultureInfo("en"));
        var result = formatter.CreateStatus([new GameServerStatus(0, "Server 0", true, 1, 1000)], DateTime.UtcNow,
            new DiscordEconomyStatus([new JewelAmount(7181, 1500), new JewelAmount(7182, 2000)], [new JewelAmount(7181, 500)]));
        Assert.That(result.Description, Does.Contain("Server 0").And.Contain("Server Jewels").And.Contain("Bless: 1,500").And.Contain("Soul: 2,000").And.Contain("Players Jewels").And.Contain("Bless: 500"));
        Assert.That(result.Footer, Does.Contain("earlier drops are not included"));
    }

    /// <summary>The same timer changes from waiting, to entry countdown, to combat without negative time.</summary>
    [Test]
    public void CountdownShowsOpeningEntryAndRunningPhases()
    {
        var formatter = new DiscordStatusFormatter(CultureInfo.GetCultureInfo("en"));
        var now = new DateTime(2026, 10, 23, 0, 0, 0, DateTimeKind.Utc);
        var opening = new DiscordEventTimer("ChaosCastle", 0, DiscordEventPhase.Opening, now.AddSeconds(4820));
        var result = formatter.CreateStatus([], now, timers: [opening]);
        Assert.That(result.Description, Does.Contain("Chaos Castle").And.Contain("01:20:20").And.Contain("<t:"));
        result = formatter.CreateStatus([], now, timers: [opening with { Phase = DiscordEventPhase.Entrance, DeadlineUtc = now.AddMinutes(5) }]);
        Assert.That(result.Description, Does.Contain("entrance closes in 00:05:00"));
        result = formatter.CreateStatus([], now, timers: [opening with { DeadlineUtc = now.AddSeconds(-10) }]);
        Assert.That(result.Description, Does.Contain("00:00:00").And.Not.Contain("-00:"));
        result = formatter.CreateStatus([], now, timers: [opening with { Phase = DiscordEventPhase.Active, DeadlineUtc = null }]);
        Assert.That(result.Description, Does.Contain("In progress"));
    }

    /// <summary>A failed database does not display a fabricated zero or hide the server state.</summary>
    [Test]
    public void UnavailableStatisticsDoNotHideServerStatus()
    {
        var formatter = new DiscordStatusFormatter(CultureInfo.GetCultureInfo("en"));
        var result = formatter.CreateStatus([new GameServerStatus(0, "Server 0", true, 1, 1000)], DateTime.UtcNow, statisticsUnavailable: true);
        Assert.That(result.Description, Does.Contain("Server 0").And.Contain("statistics are currently unavailable").And.Not.Contain("Bless: 0"));
    }
}
