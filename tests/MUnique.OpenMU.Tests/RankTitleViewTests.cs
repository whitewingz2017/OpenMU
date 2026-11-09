// <copyright file="RankTitleViewTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using System.Text.Json;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic.RankTitles;
using MUnique.OpenMU.GameLogic.Views;
using MUnique.OpenMU.GameServer.RemoteView.Inventory;

/// <summary>Checks the actual shop response consumed by desktop and mobile clients.</summary>
[TestFixture]
public class RankTitleViewTests
{
    /// <summary>The FD 08 response contains account WCoinC, the matching request and all title prices.</summary>
    [TestCase(0)]
    [TestCase(1234)]
    public async Task ShopResponseUsesAccountBalanceAsync(int balance)
    {
        var (player, output) = CastleSiegeRemoteViewTestHelper.CreatePlayer();
        using (output)
        {
            player.GameContext.FeaturePlugIns.AddPlugIn(new RankTitleFeaturePlugIn(), true);
            await player.SetAccountAsync(new Account { WCoinC = balance }).ConfigureAwait(false);
            await new RankTitleView(player).ShowAsync(42, null).ConfigureAwait(false);
            var bytes = output.ToArray();
            Assert.That(bytes[0], Is.EqualTo(0xC2));
            Assert.That((bytes[1] << 8) | bytes[2], Is.EqualTo(bytes.Length));
            Assert.That(bytes[3..5], Is.EqualTo(new byte[] { 0xFD, 0x08 }));
            using var json = JsonDocument.Parse(bytes.AsMemory(5));
            var response = json.RootElement;
            Assert.That(response.GetProperty("RequestId").GetUInt32(), Is.EqualTo(42));
            Assert.That(response.GetProperty("Balance").GetInt32(), Is.EqualTo(balance));
            Assert.That(response.GetProperty("Status").GetString(), Is.EqualTo("Ready"));
            Assert.That(response.GetProperty("Active").GetInt32(), Is.Zero);
            var ranks = response.GetProperty("Ranks").EnumerateArray().ToArray();
            Assert.That(ranks.Select(rank => rank.GetProperty("Rank").GetInt32()), Is.EqualTo(Enumerable.Range(1, 18).Where(rank => rank != 10)));
            Assert.That(ranks.All(rank => rank.GetProperty("Price").GetInt32() == (RankTitleRules.IsEarned(rank.GetProperty("Rank").GetInt32()) ? 0 : 100)), Is.True);
            Assert.That(ranks.All(rank => !rank.GetProperty("Owned").GetBoolean()), Is.True);
        }
    }

    /// <summary>Visible title updates identify the owner and hide titles the character does not own.</summary>
    [TestCase(true)]
    [TestCase(false)]
    public async Task AppearanceRequiresOwnershipAsync(bool owned)
    {
        var owner = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        owner.GameContext.FeaturePlugIns.AddPlugIn(new RankTitleFeaturePlugIn(), true);
        owner.SelectedCharacter!.OwnedRankTitlesJson = owned ? "[2]" : "[]";
        owner.SelectedCharacter.ActiveRankTitle = 2;
        var (observer, output) = CastleSiegeRemoteViewTestHelper.CreatePlayer();
        using (output)
        {
            await new RankTitleView(observer).AppearanceAsync(owner).ConfigureAwait(false);
            var bytes = output.ToArray();
            Assert.That(bytes[3..5], Is.EqualTo(new byte[] { 0xFD, 0x07 }));
            using var json = JsonDocument.Parse(bytes.AsMemory(5));
            var title = json.RootElement.GetProperty("Ranks")[0];
            Assert.That(title.GetProperty("Id").GetUInt16(), Is.EqualTo(owner.GetId(observer)));
            Assert.That(title.GetProperty("Name").GetString(), Is.EqualTo(owner.Name));
            Assert.That(title.GetProperty("Rank").GetInt32(), Is.EqualTo(owned ? 2 : 0));
        }
    }
}
