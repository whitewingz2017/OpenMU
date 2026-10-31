// <copyright file="EventTimerTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using System.Buffers.Binary;
using System.Text.Json;
using Moq;
using MUnique.OpenMU.GameLogic;
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
            It.Is<IReadOnlyList<(string Name, DateTime StartsAtUtc)>>(rows => rows.Count == 1 && rows[0].Name == "Golden Invasion" && rows[0].StartsAtUtc > before), It.IsAny<IReadOnlyList<(string Name, DateTime EndsAtUtc)>>()), Times.Once);
    }

    /// <summary>The response contains the matching request id, server UTC reference, and event rows.</summary>
    [Test]
    public async Task ViewWritesClientCompatibleSnapshotAsync()
    {
        var (player, output) = CastleSiegeRemoteViewTestHelper.CreatePlayer();
        using var stream = output;
        var now = new DateTime(2026, 10, 23, 12, 0, 0, DateTimeKind.Utc);
        await new EventTimerView(player).ShowAsync(17, now, new[] { ("Blood Castle", now.AddSeconds(1289)) }, new[] { ("Golden Invasion", now.AddSeconds(300)) }).ConfigureAwait(false);
        var bytes = output.ToArray();
        Assert.That(bytes[..1], Is.EqualTo(new byte[] { 0xC2 }));
        Assert.That(BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(1)), Is.EqualTo(bytes.Length));
        Assert.That(bytes[3..5], Is.EqualTo(new byte[] { 0xFB, 0x21 }));
        using var json = JsonDocument.Parse(bytes.AsMemory(5));
        Assert.That(json.RootElement.GetProperty("Active")[0].GetProperty("Seconds").GetInt32(), Is.EqualTo(300));
        Assert.That(json.RootElement.GetProperty("Rows")[0].GetProperty("Seconds").GetInt32(), Is.EqualTo(1289));
        Assert.That(json.RootElement.GetProperty("RequestId").GetUInt32(), Is.EqualTo(17));
        Assert.That(json.RootElement.GetProperty("ServerUtc").GetDateTime(), Is.EqualTo(now));
        Assert.That(json.RootElement.GetProperty("Rows")[0].GetProperty("StartsAtUtc").GetDateTime(), Is.EqualTo(now.AddSeconds(1289)));
    }
    /// <summary>Only running tasks contribute active countdowns; reading schedules does not start them.</summary>
    [Test]
    public async Task ActiveTasksUseTheirRealEndTimeAsync()
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        var task = new TimedTask();
        player.GameContext.PlugInManager.RegisterPlugInAtPlugInPoint<IPeriodicTaskPlugIn>(task);
        var now = DateTime.UtcNow;
        Assert.That(UpcomingEvents.GetActive(player.GameContext, now, player.Culture), Is.Empty);
        task.Start(player.GameContext, now.AddMinutes(4));
        var rows = UpcomingEvents.GetActive(player.GameContext, now, player.Culture);
        Assert.That(rows.Single().EndsAtUtc, Is.EqualTo(now.AddMinutes(4)));
        Assert.That(UpcomingEvents.GetActive(player.GameContext, now.AddMinutes(5), player.Culture), Is.Empty);
    }

    /// <summary>Weekly Crywolf starts use the hosting server and its timezone.</summary>
    [Test]
    public async Task CrywolfScheduleUsesWeekdayTimezoneAndServerAsync()
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        var plugin = new MUnique.OpenMU.GameLogic.Crywolf.CrywolfPlugIn
        {
            Configuration = new MUnique.OpenMU.GameLogic.Crywolf.CrywolfEventDefinition
            {
                GameServerId = 1,
                StartDays = new List<DayOfWeek> { DayOfWeek.Wednesday },
                StartTimes = new List<TimeOnly> { new(20, 30) },
            },
        };
        player.GameContext.PlugInManager.RegisterPlugInAtPlugInPoint<IPeriodicTaskPlugIn>(plugin);
        var server = new Mock<IGameServerContext>();
        server.SetupGet(c => c.Id).Returns((byte)1);
        var zone = TimeZoneInfo.CreateCustomTimeZone("Test+8", TimeSpan.FromHours(8), "Test+8", "Test+8");
        var now = new DateTime(2026, 11, 2, 0, 0, 0, DateTimeKind.Utc);
        var rows = UpcomingEvents.Get(player.GameContext.PlugInManager, now, zone, TimeSpan.FromDays(7), player.Culture, server.Object);
        Assert.That(rows.Single().StartsAtUtc, Is.EqualTo(new DateTime(2026, 11, 4, 12, 30, 0, DateTimeKind.Utc)));
        server.SetupGet(c => c.Id).Returns((byte)2);
        Assert.That(UpcomingEvents.Get(player.GameContext.PlugInManager, now, zone, TimeSpan.FromDays(7), player.Culture, server.Object), Is.Empty);
    }

    [System.Runtime.InteropServices.Guid("f06fcff0-49c8-4646-9d53-70422f860991")]
    private sealed class TimedTask : PeriodicTaskBasePlugIn<PeriodicTaskConfiguration, PeriodicTaskGameServerState>
    {
        public void Start(IGameContext context, DateTime end)
        {
            var state = this.GetStateByGameContext(context);
            state.State = PeriodicTaskState.Started;
            state.NextRunUtc = end;
        }

        protected override PeriodicTaskGameServerState CreateState(IGameContext context) => new(context);
        protected override ValueTask OnPrepareEventAsync(PeriodicTaskGameServerState state) => ValueTask.CompletedTask;
        protected override ValueTask OnPreparedAsync(PeriodicTaskGameServerState state) => ValueTask.CompletedTask;
        protected override ValueTask OnStartedAsync(PeriodicTaskGameServerState state) => ValueTask.CompletedTask;
        protected override ValueTask OnFinishedAsync(PeriodicTaskGameServerState state) => ValueTask.CompletedTask;
    }

}
