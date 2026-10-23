// <copyright file="EventTimerTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using System.Buffers.Binary;
using System.Text.Json;
using Moq;
using MUnique.OpenMU.GameLogic.PlayerActions;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.GameLogic.PlugIns.InvasionEvents;
using MUnique.OpenMU.GameLogic.PlugIns.PeriodicTasks;
using MUnique.OpenMU.GameLogic.Views;
using MUnique.OpenMU.GameServer.RemoteView;

/// <summary>Checks the server schedule action and the custom client's wire response.</summary>
[TestFixture]
public class EventTimerTests
{
    /// <summary>Requests use the active server's configured timetable and return its UTC clock.</summary>
    [Test]
    public async Task RequestReturnsConfiguredEventAsync()
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        var next = TimeOnly.FromDateTime(DateTime.UtcNow.AddHours(1));
        player.GameContext.PlugInManager.RegisterPlugInAtPlugInPoint<IPeriodicTaskPlugIn>(new GoldenInvasionPlugIn
        {
            Configuration = new PeriodicInvasionConfiguration { Timetable = new List<TimeOnly> { next }, PreStartMessageDelay = TimeSpan.Zero },
        });
        var view = Mock.Get(player.ViewPlugIns.GetPlugIn<IEventTimerView>() ?? throw new InvalidOperationException("Missing event timer view."));
        var before = DateTime.UtcNow;
        await new EventTimerRequestAction().RequestAsync(player, 42).ConfigureAwait(false);
        view.Verify(v => v.ShowAsync(42, It.Is<DateTime>(utc => utc >= before && utc <= DateTime.UtcNow),
            It.Is<IReadOnlyList<(string Name, DateTime StartsAtUtc)>>(rows => rows.Count == 1 && rows[0].Name == "Golden Invasion" && rows[0].StartsAtUtc > before)), Times.Once);
    }

    /// <summary>The response contains the matching request id, server UTC reference, and event rows.</summary>
    [Test]
    public async Task ViewWritesClientCompatibleSnapshotAsync()
    {
        var (player, output) = CastleSiegeRemoteViewTestHelper.CreatePlayer();
        using var stream = output;
        var now = new DateTime(2026, 10, 23, 12, 0, 0, DateTimeKind.Utc);
        await new EventTimerView(player).ShowAsync(17, now, new[] { ("Blood Castle", now.AddSeconds(1289)) }).ConfigureAwait(false);
        var bytes = output.ToArray();
        Assert.That(bytes[..1], Is.EqualTo(new byte[] { 0xC2 }));
        Assert.That(BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(1)), Is.EqualTo(bytes.Length));
        Assert.That(bytes[3..5], Is.EqualTo(new byte[] { 0xFB, 0x21 }));
        using var json = JsonDocument.Parse(bytes.AsMemory(5));
        Assert.That(json.RootElement.GetProperty("RequestId").GetUInt32(), Is.EqualTo(17));
        Assert.That(json.RootElement.GetProperty("ServerUtc").GetDateTime(), Is.EqualTo(now));
        Assert.That(json.RootElement.GetProperty("Rows")[0].GetProperty("StartsAtUtc").GetDateTime(), Is.EqualTo(now.AddSeconds(1289)));
    }
}
