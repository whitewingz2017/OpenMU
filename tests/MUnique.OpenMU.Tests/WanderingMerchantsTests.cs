// <copyright file="WanderingMerchantsTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.PlugIns.WanderingMerchants;
using BasicModel = MUnique.OpenMU.Persistence.BasicModel;

/// <summary>Tests that one broken wandering spawn cannot prevent Marlon from appearing.</summary>
[TestFixture]
public class WanderingMerchantsTests
{
    /// <summary>A failed spawn is not published and does not prevent the next merchant from spawning.</summary>
    [Test]
    public async Task InvalidMerchantDoesNotBlockMarlonAsync()
    {
        var definition = new BasicModel.GameMapDefinition { Number = 0, TerrainData = new byte[ushort.MaxValue + 3] };
        var map = new GameMap(definition, TimeSpan.FromMinutes(1), 8);
        var context = new Mock<IGameContext>();
        context.SetupGet(c => c.Configuration).Returns(new BasicModel.GameConfiguration());
        context.SetupGet(c => c.LoggerFactory).Returns(NullLoggerFactory.Instance);
        context.Setup(c => c.GetMapAsync(0, true)).ReturnsAsync(map);
        var state = new WanderingMerchantsState(context.Object);
        var invalid = CreateMerchant(definition, 247, NpcObjectKind.Monster, 10);
        var marlon = CreateMerchant(definition, 229, NpcObjectKind.PassiveNpc, 20);
        map.Terrain.SafezoneMap[10, 10] = true;
        state.Merchants.Add(invalid);
        state.Merchants.Add(marlon);
        var plugin = new TestPlugIn { Configuration = new WanderingMerchantsConfiguration { MinimumSpawnDuration = TimeSpan.FromHours(1), MaximumSpawnDuration = TimeSpan.FromHours(2) } };
        try
        {
            await plugin.RunAsync(state).ConfigureAwait(false);
            Assert.That(invalid.Merchant, Is.Null);
            Assert.That(invalid.NextWanderingAt, Is.LessThan(DateTime.UtcNow));
            Assert.That(marlon.Merchant, Is.Not.Null);
            Assert.That(map.GetObject(marlon.Merchant!.Id), Is.SameAs(marlon.Merchant));
            Assert.That(marlon.NextWanderingAt, Is.GreaterThan(DateTime.UtcNow));
        }
        finally
        {
            await map.TearDownAsync().ConfigureAwait(false);
        }
    }

    private static MerchantSpawnState CreateMerchant(GameMapDefinition map, short number, NpcObjectKind kind, byte coordinate)
    {
        var monster = new MonsterDefinition { Number = number, ObjectKind = kind };
        var spawn = new MonsterSpawnArea { GameMap = map, MonsterDefinition = monster, SpawnTrigger = SpawnTrigger.Wandering, X1 = coordinate, X2 = coordinate, Y1 = coordinate, Y2 = coordinate };
        return new MerchantSpawnState(monster, new List<MonsterSpawnArea> { spawn }) { NextWanderingAt = DateTime.UtcNow.AddSeconds(-1) };
    }

    private sealed class TestPlugIn : WanderingMerchantsPlugIn
    {
        public ValueTask RunAsync(WanderingMerchantsState state) => this.OnStartedAsync(state);
    }
}
