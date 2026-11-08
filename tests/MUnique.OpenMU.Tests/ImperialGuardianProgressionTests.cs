// <copyright file="ImperialGuardianProgressionTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection;
using Moq;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.MiniGames;
using MUnique.OpenMU.GameLogic.MiniGames.ImperialGuardian;
using MUnique.OpenMU.GameLogic.NPC;
using MUnique.OpenMU.GameLogic.PlayerActions;
using MUnique.OpenMU.GameLogic.PlayerActions.MiniGames;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.Pathfinding;
using MonsterAttribute = MUnique.OpenMU.Persistence.BasicModel.MonsterAttribute;
using MonsterDefinition = MUnique.OpenMU.Persistence.BasicModel.MonsterDefinition;

/// <summary>Tests the gate and portal sequence shared by PC and mobile clients.</summary>
[TestFixture]
public class ImperialGuardianProgressionTests
{
    private readonly List<ImperialGuardianContext> _games = new();

    /// <summary>Stops background timers after each test.</summary>
    [TearDown]
    public async Task TearDownAsync()
    {
        foreach (var game in this._games)
        {
            await game.DisposeAsync().ConfigureAwait(false);
        }

        this._games.Clear();
    }

    /// <summary>Every zone waits for both gates and successful portal entry, including later party entries.</summary>
    /// <param name="day">The event day.</param>
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    [TestCase(6)]
    [TestCase(7)]
    public async Task StandbyStartsOnlyAfterPortalEntryAsync(byte day)
    {
        var (game, player, partyMember) = await this.CreateGameAsync(day).ConfigureAwait(false);
        var portals = game.Map.Definition.EnterGates.OrderBy(gate => gate.Number).ToArray();
        for (var zone = 0; zone < game.ZoneCount; zone++)
        {
            await WaitUntilAsync(() => game.Zone == zone && game.RemainingMonsterCount == 1).ConfigureAwait(false);
            var gates = Gates(game).Where(gate => gate.SpawnArea.WaveNumber == ImperialGuardianEventDefinition.GetWaveNumber(day, zone)).ToArray();
            Assert.That(gates, Has.Length.EqualTo(3));
            if (zone < portals.Length)
            {
                Assert.That(await IsPortalDeniedAsync(player, portals[zone].TargetGate!).ConfigureAwait(false), Is.True, "Monsters are still alive.");
            }

            foreach (var monster in GetField<ConcurrentDictionary<Monster, byte>>(game, "_zoneMonsters").Keys)
            {
                SetDead(monster);
            }

            if (zone == game.ZoneCount - 1)
            {
                await WaitUntilAsync(() => Results(player).Contains(ImperialGuardianResult.Success)).ConfigureAwait(false);
                Assert.That(game.Zone, Is.EqualTo(zone));
                return;
            }

            await WaitUntilAsync(() => gates.Where(gate => gate.Definition.Number != 525).All(gate => gate.IsAttackable)).ConfigureAwait(false);
            Assert.That(game.Zone, Is.EqualTo(zone), "The last monster must not advance the zone.");
            Assert.That(TimerType(game), Is.EqualTo(ImperialGuardianTimerType.TimeAttack));
            SetDead(gates.Single(gate => gate.Definition.Number == 525));
            SetDead(gates.Single(gate => gate.Definition.Number == 524));
            Assert.That(await IsPortalDeniedAsync(player, portals[zone].TargetGate!).ConfigureAwait(false), Is.True, "The last gate is still alive.");
            SetDead(gates.Single(gate => gate.Definition.Number == 527));
            Assert.That(await IsPortalDeniedAsync(player, portals[zone].TargetGate!).ConfigureAwait(false), Is.False);
            if (zone + 1 < portals.Length)
            {
                Assert.That(await IsPortalDeniedAsync(player, portals[zone + 1].TargetGate!).ConfigureAwait(false), Is.True, "A later portal must not skip a zone.");
            }

            await Task.Delay(1100).ConfigureAwait(false);
            Assert.That(game.Zone, Is.EqualTo(zone), "Breaking all gates must still wait for the portal.");
            partyMember.IsAlive = false;
            player.Position = new Point(portals[zone].X1, portals[zone].Y1);
            await new WarpGateAction().EnterGateAsync(player, portals[zone]).ConfigureAwait(false);
            Assert.That(player.CurrentMap, Is.Null);
            await Task.Delay(1100).ConfigureAwait(false);
            Assert.That(game.Zone, Is.EqualTo(zone), "A warp request alone must not start standby.");
            Assert.That(Results(player), Does.Not.Contain(ImperialGuardianResult.Failed), "A solo player loading the map is still in the event.");
            await player.ClientReadyAfterMapChangeAsync().ConfigureAwait(false);
            var terrainUpdates = Mock.Get(player.ViewPlugIns.GetPlugIn<IChangeTerrainAttributesViewPlugin>()!).Invocations;
            Assert.That(terrainUpdates.Any(call => call.Method.Name == nameof(IChangeTerrainAttributesViewPlugin.ChangeAttributesAsync)
                && call.Arguments[1] is false
                && ((IReadOnlyCollection<(byte, byte, byte, byte)>)call.Arguments[2]).Count == (zone + 1) * 3), Is.True,
                "Destroyed gates must remain open after the client reloads the map.");
            await WaitUntilAsync(() => game.Zone == zone + 1 && TimerType(game) == ImperialGuardianTimerType.Standby).ConfigureAwait(false);
            var standby = GetField<object>(game, "_timer");
            await player.ClientReadyAfterMapChangeAsync().ConfigureAwait(false);
            Assert.That(GetField<object>(game, "_timer"), Is.SameAs(standby), "Duplicate client readiness must not restart standby.");
            Assert.That(await IsPortalDeniedAsync(player, portals[zone].TargetGate!).ConfigureAwait(false), Is.False, "Party members can follow through the cleared zone.");
            partyMember.Position = new Point(portals[zone].X1, portals[zone].Y1);
            await new WarpGateAction().EnterGateAsync(partyMember, portals[zone]).ConfigureAwait(false);
            await partyMember.ClientReadyAfterMapChangeAsync().ConfigureAwait(false);
            Assert.That(GetField<object>(game, "_timer"), Is.SameAs(standby), "A later party entry must not restart standby.");
            await WaitUntilAsync(() => game.SkipCurrentWait()).ConfigureAwait(false);
        }
    }

    /// <summary>Cleared monsters do not remove the time limit for the remaining gates and portal.</summary>
    [Test]
    public async Task PortalPhaseStillTimesOutAsync()
    {
        var (game, player, _) = await this.CreateGameAsync(1).ConfigureAwait(false);
        foreach (var monster in GetField<ConcurrentDictionary<Monster, byte>>(game, "_zoneMonsters").Keys)
        {
            SetDead(monster);
        }

        await WaitUntilAsync(() => Gates(game).All(gate => gate.IsAttackable)).ConfigureAwait(false);
        typeof(ImperialGuardianContext).GetMethod("SetTimer", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(game, new object[] { ImperialGuardianTimerType.TimeAttack, TimeSpan.FromSeconds(-1) });
        Assert.That(await IsPortalDeniedAsync(player, game.Map.Definition.EnterGates.MinBy(gate => gate.Number)!.TargetGate!).ConfigureAwait(false), Is.True);
        await WaitUntilAsync(() => Results(player).Contains(ImperialGuardianResult.Failed)).ConfigureAwait(false);
        Assert.That(game.Zone, Is.Zero);
    }

    private static T GetField<T>(ImperialGuardianContext game, string name) =>
        (T)typeof(ImperialGuardianContext).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(game)!;

    private static IEnumerable<ImperialGuardianGate> Gates(ImperialGuardianContext game) =>
        GetField<ConcurrentDictionary<ImperialGuardianGate, byte>>(game, "_gates").Keys;

    private static ImperialGuardianTimerType TimerType(ImperialGuardianContext game)
    {
        var timer = GetField<object>(game, "_timer");
        return (ImperialGuardianTimerType)timer.GetType().GetProperty("Type")!.GetValue(timer)!;
    }

    private static IEnumerable<ImperialGuardianResult> Results(Player player) =>
        Mock.Get(player.ViewPlugIns.GetPlugIn<IImperialGuardianViewPlugIn>()!).Invocations
            .Where(call => call.Method.Name == nameof(IImperialGuardianViewPlugIn.ShowResultAsync))
            .Select(call => (ImperialGuardianResult)call.Arguments[0]).ToArray();

    private static void SetDead(AttackableNpcBase npc) =>
        typeof(AttackableNpcBase).GetProperty(nameof(AttackableNpcBase.IsAlive))!.SetValue(npc, false);

    private static async ValueTask<bool> IsPortalDeniedAsync(Player player, ExitGate target)
    {
        var args = new CancelEventArgs();
        await player.GameContext.PlugInManager.GetPlugInPoint<IWarpGateEnteringPlugIn>()!
            .WarpGateEnteringAsync(player, target, args).ConfigureAwait(false);
        return args.Cancel;
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (!condition())
        {
            Assert.That(DateTime.UtcNow, Is.LessThan(deadline), "The expected event state wasn't reached.");
            await Task.Delay(20).ConfigureAwait(false);
        }
    }

    private async Task<(ImperialGuardianContext Game, Player Player, Player PartyMember)> CreateGameAsync(byte day)
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        player.GameContext.PlugInManager.RegisterPlugInAtPlugInPoint<IWarpGateEnteringPlugIn>(new ImperialGuardianFeaturePlugIn());
        var map = player.GameContext.Configuration.Maps.Single();
        var portals = new List<EnterGate>();
        Mock.Get(map).SetupGet(m => m.EnterGates).Returns(portals);
        var zoneCount = day == 7 ? 4 : 3;
        for (var zone = 0; zone < zoneCount; zone++)
        {
            if (zone < zoneCount - 1)
            {
                portals.Insert(0, new EnterGate
                {
                    Number = (short)(300 + zone),
                    X1 = (byte)(150 + (zone * 20)), X2 = (byte)(150 + (zone * 20)), Y1 = 150, Y2 = 150,
                    TargetGate = new ExitGate { Map = map, X1 = (byte)(20 + zone), X2 = (byte)(20 + zone), Y1 = 20, Y2 = 20 },
                });
            }

            foreach (var number in new short[] { 512, 525, 524, 527 })
            {
                var monster = new MonsterDefinition { Id = Guid.NewGuid(), Number = number, ObjectKind = NpcObjectKind.Monster, AttackDelay = TimeSpan.FromHours(1), ViewRange = 0 };
                monster.Attributes.Add(new MonsterAttribute { AttributeDefinition = Stats.MaximumHealth, Value = 100 });
                map.MonsterSpawns.Add(new MonsterSpawnArea
                {
                    MonsterDefinition = monster, GameMap = map, Quantity = 1,
                    SpawnTrigger = SpawnTrigger.OnceAtWaveStart,
                    WaveNumber = ImperialGuardianEventDefinition.GetWaveNumber(day, zone),
                    X1 = (byte)(60 + ((number % 10) * 20)), X2 = (byte)(60 + ((number % 10) * 20)), Y1 = (byte)(50 + (zone * 20)), Y2 = (byte)(50 + (zone * 20)),
                });
            }
        }

        var definition = new Mock<MiniGameDefinition>();
        definition.SetupGet(d => d.Rewards).Returns(new List<MiniGameReward>());
        definition.SetupGet(d => d.SpawnWaves).Returns(new List<MiniGameSpawnWave>());
        definition.SetupGet(d => d.ChangeEvents).Returns(new List<MiniGameChangeEvent>());
        definition.SetupGet(d => d.Entrance).Returns(new ExitGate { Map = map });
        definition.Object.Type = MiniGameType.ImperialGuardian;
        definition.Object.GameLevel = day;
        definition.Object.MaximumPlayerCount = 5;
        definition.Object.MapCreationPolicy = MiniGameMapCreationPolicy.Shared;
        var initializer = new Mock<IMapInitializer>();
        initializer.Setup(i => i.CreateGameMap(It.IsAny<GameMapDefinition>()))
            .Returns<GameMapDefinition>(m => new GameMap(m, TimeSpan.FromMinutes(1), 16));
        var game = new ImperialGuardianContext(new MiniGameMapKey(0, 0, string.Empty), definition.Object, player.GameContext, initializer.Object);
        this._games.Add(game);
        var settings = GetField<ImperialGuardianEventDefinition>(game, "_definition");
        settings.ExperienceRewards.Clear();
        settings.StartNextZoneWhenCleared = day % 2 == 0;
        Assert.That(await game.TryEnterAsync(player).ConfigureAwait(false), Is.EqualTo(EnterResult.Success));
        player.IsAlive = true;
        player.CurrentMap = game.Map;
        player.Position = new Point(10, 10);
        await game.Map.AddAsync(player).ConfigureAwait(false);
        var partyMember = await PlayerTestHelper.CreatePlayerAsync(player.GameContext).ConfigureAwait(false);
        Assert.That(await game.TryEnterAsync(partyMember).ConfigureAwait(false), Is.EqualTo(EnterResult.Success));
        partyMember.IsAlive = true;
        partyMember.CurrentMap = game.Map;
        partyMember.Position = new Point(10, 10);
        await game.Map.AddAsync(partyMember).ConfigureAwait(false);
        await WaitUntilAsync(() => Gates(game).Count() == 3).ConfigureAwait(false);
        await ((ValueTask)typeof(MiniGameContext).GetMethod("StartAsync", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(game, null)!).ConfigureAwait(false);
        await WaitUntilAsync(() => game.RemainingMonsterCount == 1).ConfigureAwait(false);
        return (game, player, partyMember);
    }
}
