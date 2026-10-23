// <copyright file="DiscordLiveJewelStatusTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests.Discord;

using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MUnique.OpenMU.Discord;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.Interfaces;
using MUnique.OpenMU.Persistence;
using MUnique.OpenMU.PlugIns;
using Basic = MUnique.OpenMU.Persistence.BasicModel;

/// <summary>Checks replacement of stale persisted holdings by current online holdings.</summary>
[TestFixture]
public class DiscordLiveJewelStatusTests
{
    /// <summary>Includes offline holdings once and replaces even empty online snapshots.</summary>
    /// <param name="liveCount">The current inventory count.</param>
    [TestCase(0)]
    [TestCase(2)]
    public async Task OnlineSnapshotsReplaceSavedHoldingsAsync(int liveCount)
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        var inventory = new Basic.ItemStorage { Id = Guid.NewGuid() };
        player.SelectedCharacter!.Inventory = inventory;
        var definition = new Basic.ItemDefinition { Group = 14, Number = 13 };
        for (var i = 0; i < liveCount; i++)
        {
            inventory.Items.Add(new Basic.Item { Definition = definition });
        }

        player.Account!.JewelBankBalances = "{\"7181:0\":20}";
        var saved = new JewelStatisticsSnapshot(
            [new(inventory.Id, false, [new(7181, 10)]), new(player.Account!.GetId(), true, [new(7181, 30)]), new(Guid.NewGuid(), false, [new(7181, 5)])],
            [new(7181, 100)]);
        var context = new Mock<IPlayerContext>();
        context.Setup(value => value.GetJewelStatisticsAsync(It.IsAny<int[]>())).ReturnsAsync(saved);
        var persistence = new Mock<IPersistenceContextProvider>();
        persistence.Setup(value => value.CreateNewPlayerContext(It.IsAny<MUnique.OpenMU.DataModel.Configuration.GameConfiguration>())).Returns(context.Object);
        var gameContext = new Mock<IGameServerContext>();
        gameContext.Setup(value => value.GetPlayersAsync()).ReturnsAsync(new List<Player> { player });
        var server = new Mock<IGameServer>();
        server.As<IGameServerContextProvider>().SetupGet(value => value.Context).Returns(gameContext.Object);
        var servers = new Mock<IServerProvider>();
        servers.SetupGet(value => value.Servers).Returns(new List<IManageableServer> { server.Object });
        var provider = new DiscordGameDataProvider(servers.Object, persistence.Object, Mock.Of<IFriendServer>(), Mock.Of<IGuildServer>(), new PlugInManager([], NullLoggerFactory.Instance, null, null), () => TimeZoneInfo.Utc);
        var status = await provider.GetEconomyStatusAsync().ConfigureAwait(false);
        Assert.That(status!.Held.Single(value => value.ItemCode == 7181).Amount, Is.EqualTo(25 + liveCount));
        Assert.That(status.Produced.Single(value => value.ItemCode == 7181).Amount, Is.EqualTo(100));
    }
}
