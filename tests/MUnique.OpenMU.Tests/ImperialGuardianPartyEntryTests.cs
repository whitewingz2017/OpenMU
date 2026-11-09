// <copyright file="ImperialGuardianPartyEntryTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.MiniGames.ImperialGuardian;
using MUnique.OpenMU.GameLogic.PlayerActions.MiniGames;
using Item = MUnique.OpenMU.Persistence.BasicModel.Item;
using ItemDefinition = MUnique.OpenMU.Persistence.BasicModel.ItemDefinition;
using MiniGameDefinition = MUnique.OpenMU.Persistence.BasicModel.MiniGameDefinition;

/// <summary>Tests automatic ticketed party entry from any member.</summary>
[TestFixture]
public class ImperialGuardianPartyEntryTests
{
    /// <summary>Any member brings eligible humans along without consuming duplicate tickets.</summary>
    /// <param name="leaderEnters">Whether the party master initiates entry.</param>
    /// <param name="concurrent">Whether both eligible members request entry at once.</param>
    [TestCase(true, false)]
    [TestCase(false, false)]
    [TestCase(false, true)]
    public async ValueTask TicketedMembersEnterSameRunAsync(bool leaderEnters, bool concurrent)
    {
        var leader = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        var configuration = leader.GameContext.Configuration;
        Mock.Get(configuration.Maps.Single()).SetupGet(map => map.EnterGates).Returns(new List<EnterGate>());
        var day = EnterImperialGuardianAction.GetDay(leader.GameContext, new ImperialGuardianEventDefinition());
        var definition = new MiniGameDefinition
        {
            Type = MiniGameType.ImperialGuardian, GameLevel = day,
            Entrance = new ExitGate { Map = configuration.Maps.Single(), X1 = 10, X2 = 10, Y1 = 10, Y2 = 10 },
            MapCreationPolicy = MiniGameMapCreationPolicy.OnePerParty, AllowParty = true,
            MaximumPlayerCount = 5, MinimumCharacterLevel = 100, MaximumCharacterLevel = 400,
            MinimumSpecialCharacterLevel = 100, MaximumSpecialCharacterLevel = 400,
            EnterDuration = TimeSpan.FromMinutes(5), GameDuration = TimeSpan.FromMinutes(10),
        };
        Mock.Get(configuration).SetupGet(c => c.MiniGameDefinitions).Returns(new List<DataModel.Configuration.MiniGameDefinition> { definition });
        var partyManager = new PartyManager(5, NullLogger<Party>.Instance);
        await using var party = partyManager.CreateParty();
        var members = new List<Player> { leader };
        for (var index = 1; index < 5; index++)
        {
            members.Add(await PlayerTestHelper.CreatePlayerAsync(leader.GameContext).ConfigureAwait(false));
        }

        for (var index = 0; index < members.Count; index++)
        {
            var member = members[index];
            member.SelectedCharacter!.Name = $"Member{index}";
            member.Attributes![Stats.Level] = index == 4 ? 1 : 200;
            member.IsAlive = true;
            await party.AddAsync(member).ConfigureAwait(false);
            if (index != 2)
            {
                var ticket = new Item
                {
                    Definition = new ItemDefinition { Group = 14, Number = day == 7 ? (short)109 : (short)102, Width = 1, Height = 1 },
                    Durability = 2,
                };
                await member.Inventory!.AddItemAsync(20, ticket).ConfigureAwait(false);
            }
        }

        await members[3].PlayerState.TryAdvanceToAsync(PlayerState.TradeRequested).ConfigureAwait(false);
        try
        {
            var action = new EnterImperialGuardianAction();
            if (concurrent)
            {
                await Task.WhenAll(action.TryEnterAsync(leader).AsTask(), action.TryEnterAsync(members[1]).AsTask()).ConfigureAwait(false);
            }
            else
            {
                await action.TryEnterAsync(leaderEnters ? leader : members[1]).ConfigureAwait(false);
            }

            var deadline = DateTime.UtcNow.AddSeconds(5);
            while (members.Take(2).Any(member => member.CurrentMiniGame is null || member.Inventory!.Items.Single().Durability != 1))
            {
                Assert.That(DateTime.UtcNow, Is.LessThan(deadline), "Ticketed members should automatically enter.");
                await Task.Delay(20).ConfigureAwait(false);
            }

            Assert.That(leader.CurrentMiniGame, Is.InstanceOf<ImperialGuardianContext>());
            Assert.That(members[1].CurrentMiniGame, Is.SameAs(leader.CurrentMiniGame));
            Assert.That(members.Skip(2).Select(member => member.CurrentMiniGame), Is.All.Null);
            Assert.That(members[3].Inventory!.Items.Single().Durability, Is.EqualTo(2), "Busy members keep their ticket.");
            Assert.That(members[4].Inventory!.Items.Single().Durability, Is.EqualTo(2), "Under-level members keep their ticket.");
            await action.TryEnterAsync(leader).ConfigureAwait(false);
            Assert.That(leader.Inventory!.Items.Single().Durability, Is.EqualTo(1));
        }
        finally
        {
            foreach (var game in leader.GameContext.MiniGames.GetRunningMiniGames(MiniGameType.ImperialGuardian))
            {
                await game.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
}
