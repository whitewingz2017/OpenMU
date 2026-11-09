// <copyright file="PartyManagerTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using Microsoft.Extensions.Logging.Abstractions;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.Persistence.InMemory;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Tests for <see cref="PartyManager"/>.
/// </summary>
[TestFixture]
public class PartyManagerTest
{
    private const byte MaxPartySize = 5;

    private PartyManager _partyManager = null!;

    /// <summary>
    /// Sets up a fresh <see cref="PartyManager"/> before each test.
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        this._partyManager = new PartyManager(MaxPartySize, new NullLogger<Party>());
    }

    /// <summary>
    /// Tests that <see cref="PartyManager.CreateParty"/> returns a party with the configured max size.
    /// </summary>
    [Test]
    public void CreateParty_RespectsMaxPartySize()
    {
        var party = this._partyManager.CreateParty();
        Assert.That(party.MaxPartySize, Is.EqualTo(MaxPartySize));
    }

    /// <summary>
    /// Tests that a member added to the party is present in the party list.
    /// </summary>
    [Test]
    public async ValueTask CreateParty_AddedMemberIsInPartyList()
    {
        var member = await this.CreatePartyMemberAsync().ConfigureAwait(false);
        var party = this._partyManager.CreateParty();

        await party.AddAsync(member).ConfigureAwait(false);

        Assert.That(party.PartyList, Contains.Item(member));
    }

    /// <summary>
    /// Tests that <see cref="PartyManager.OnMemberReconnectedAsync"/> does nothing
    /// when the member has no cached party.
    /// </summary>
    [Test]
    public async ValueTask OnMemberReconnected_DoesNothingWhenNoCachedParty()
    {
        var member = await this.CreatePartyMemberAsync().ConfigureAwait(false);

        await this._partyManager.OnMemberReconnectedAsync(member).ConfigureAwait(false);

        Assert.That(member.Party, Is.Null);
    }

    /// <summary>
    /// Tests that when a member is kicked the party list no longer contains them.
    /// </summary>
    [Test]
    public async ValueTask KickedMember_IsRemovedFromPartyList()
    {
        var (party, _, member2) = await this.CreatePartyWithTwoMembersAsync().ConfigureAwait(false);

        await party.KickPlayerAsync(GetPartyMemberIndex(party, member2)).ConfigureAwait(false);

        Assert.That(party.PartyList, Is.Not.Contains(member2));
        Assert.That(member2.Party, Is.Null);
    }

    /// <summary>
    /// Tests that <see cref="Party.LeaveTemporarilyAsync"/> replaces a live member
    /// with an <see cref="OfflinePartyMember"/> snapshot.
    /// </summary>
    [Test]
    public async ValueTask LeaveTemporarily_ReplacesWithOfflineSnapshot()
    {
        var (party, member1, _) = await this.CreatePartyWithTwoMembersAsync().ConfigureAwait(false);

        await party.LeaveTemporarilyAsync(member1).ConfigureAwait(false);

        Assert.That(party.PartyList[0], Is.InstanceOf<OfflinePartyMember>());
        Assert.That(party.PartyList[0].Name, Is.EqualTo(member1.Name));
    }

    /// <summary>
    /// Tests that <see cref="PartyManager.OnMemberReconnectedAsync"/> restores the live member
    /// from an <see cref="OfflinePartyMember"/> snapshot.
    /// </summary>
    [Test]
    public async ValueTask OnMemberReconnected_RestoresLiveMember()
    {
        var (party, member1, _) = await this.CreatePartyWithTwoMembersAsync().ConfigureAwait(false);
        var originalName = member1.Name;

        // Member disconnects - replaced with offline snapshot
        await party.LeaveTemporarilyAsync(member1).ConfigureAwait(false);

        // Member reconnects - restored from snapshot
        await this._partyManager.OnMemberReconnectedAsync(member1).ConfigureAwait(false);

        Assert.That(party.PartyList[0], Is.SameAs(member1));
        Assert.That(party.PartyList[0].Name, Is.EqualTo(originalName));
    }

    /// <summary>
    /// Tests that after reconnect, the live member's Party reference is restored.
    /// </summary>
    [Test]
    public async ValueTask OnMemberReconnected_RestoresPartyReference()
    {
        var (party, member1, _) = await this.CreatePartyWithTwoMembersAsync().ConfigureAwait(false);

        await party.LeaveTemporarilyAsync(member1).ConfigureAwait(false);
        await this._partyManager.OnMemberReconnectedAsync(member1).ConfigureAwait(false);

        Assert.That(member1.Party, Is.SameAs(party));
    }

    /// <summary>
    /// Tests that the offline snapshot preserves the disconnected member's name.
    /// </summary>
    [Test]
    public async ValueTask LeaveTemporarily_SnapshotPreservesName()
    {
        var (party, member1, _) = await this.CreatePartyWithTwoMembersAsync().ConfigureAwait(false);

        await party.LeaveTemporarilyAsync(member1).ConfigureAwait(false);

        Assert.That(party.PartyList[0].Name, Is.EqualTo(member1.Name));
    }

    /// <summary>
    /// Tests that <see cref="Party.ReplaceMemberAsync"/> preserves the party master status
    /// when replacing the master.
    /// </summary>
    [Test]
    public async ValueTask ReplaceMember_PreservesMasterStatus()
    {
        var (party, member1, _) = await this.CreatePartyWithTwoMembersAsync().ConfigureAwait(false);

        // member1 is master (first member added)
        var snapshot = new OfflinePartyMember(member1);
        await party.ReplaceMemberAsync(member1, snapshot).ConfigureAwait(false);

        Assert.That(party.PartyMaster, Is.SameAs(snapshot));
    }

    /// <summary>
    /// Tests that <see cref="Party.ReplaceMemberAsync"/> clears the old member's party reference.
    /// </summary>
    [Test]
    public async ValueTask ReplaceMember_ClearsOldMemberPartyReference()
    {
        var (party, member1, _) = await this.CreatePartyWithTwoMembersAsync().ConfigureAwait(false);

        var snapshot = new OfflinePartyMember(member1);
        await party.ReplaceMemberAsync(member1, snapshot).ConfigureAwait(false);

        Assert.That(member1.Party, Is.Null);
    }

    /// <summary>
    /// A different character releases an offline reservation only for its own account.
    /// </summary>
    [TestCase(true)]
    [TestCase(false)]
    public async ValueTask CharacterSwitchRemovesOnlyOwnOfflineReservationAsync(bool sameAccount)
    {
        var (party, oldCharacter, _) = await this.CreatePartyWithTwoMembersAsync().ConfigureAwait(false);
        oldCharacter.Account!.LoginName = "switch-account";
        var oldName = oldCharacter.Name;
        await party.LeaveTemporarilyAsync(oldCharacter).ConfigureAwait(false);
        var otherCharacter = await this.CreatePartyMemberAsync().ConfigureAwait(false);
        otherCharacter.Account!.LoginName = sameAccount ? "switch-account" : "another-account";
        otherCharacter.SelectedCharacter!.Name = "OtherChar";

        await this._partyManager.OnMemberReconnectedAsync(otherCharacter).ConfigureAwait(false);

        Assert.That(party.PartyList.Any(m => m.Name == oldName), Is.EqualTo(!sameAccount));
        Assert.That(otherCharacter.Party, Is.Null);
    }

    /// <summary>
    /// A disconnected member retains its slot only within the reconnect timeout.
    /// </summary>
    [TestCase(-1, false)]
    [TestCase(0, true)]
    [TestCase(1, true)]
    public async ValueTask OfflineReservationExpiresAfterTimeoutAsync(int secondsAfterTimeout, bool expired)
    {
        var (party, member, remaining) = await this.CreatePartyWithTwoMembersAsync().ConfigureAwait(false);
        await party.LeaveTemporarilyAsync(member).ConfigureAwait(false);
        var snapshot = (OfflinePartyMember)party.PartyList[0];
        var timeout = new PartyAutoRejoinPlugInConfiguration().ReconnectTimeout;
        Assert.That(timeout, Is.EqualTo(TimeSpan.FromMinutes(15)));

        await this._partyManager.RemoveExpiredMembersAsync(timeout, snapshot.DisconnectedAtUtc + timeout + TimeSpan.FromSeconds(secondsAfterTimeout)).ConfigureAwait(false);

        Assert.That(party.PartyList.Contains(snapshot), Is.EqualTo(!expired));
        Assert.That(remaining.Party, expired ? Is.Null : Is.SameAs(party));
        await this._partyManager.OnMemberReconnectedAsync(member).ConfigureAwait(false);
        Assert.That(member.Party, expired ? Is.Null : Is.SameAs(party));
    }

    /// <summary>
    /// Cleanup does not remove a member who has already reconnected.
    /// </summary>
    [Test]
    public async ValueTask ReconnectedMemberSurvivesReservationCleanupAsync()
    {
        var (party, member, _) = await this.CreatePartyWithTwoMembersAsync().ConfigureAwait(false);
        await party.LeaveTemporarilyAsync(member).ConfigureAwait(false);
        var snapshot = (OfflinePartyMember)party.PartyList[0];
        await this._partyManager.OnMemberReconnectedAsync(member).ConfigureAwait(false);
        var timeout = new PartyAutoRejoinPlugInConfiguration().ReconnectTimeout;

        await this._partyManager.RemoveExpiredMembersAsync(timeout, snapshot.DisconnectedAtUtc + timeout).ConfigureAwait(false);

        Assert.That(party.PartyList, Contains.Item(member));
        Assert.That(member.Party, Is.SameAs(party));
    }

    /// <summary>
    /// Expiring a disconnected leader leaves the other members together with a new leader.
    /// </summary>
    [Test]
    public async ValueTask ExpiredLeaderIsRemovedWithoutDisbandingRemainingMembersAsync()
    {
        var (party, leader, remaining) = await this.CreatePartyWithTwoMembersAsync().ConfigureAwait(false);
        var third = await this.CreatePartyMemberAsync().ConfigureAwait(false);
        await party.AddAsync(third).ConfigureAwait(false);
        await party.LeaveTemporarilyAsync(leader).ConfigureAwait(false);
        var snapshot = (OfflinePartyMember)party.PartyList[0];
        var timeout = new PartyAutoRejoinPlugInConfiguration().ReconnectTimeout;

        await this._partyManager.RemoveExpiredMembersAsync(timeout, snapshot.DisconnectedAtUtc + timeout).ConfigureAwait(false);

        Assert.That(party.PartyList, Has.Count.EqualTo(2));
        Assert.That(party.PartyMaster, Is.SameAs(remaining));
        Assert.That(remaining.Party, Is.SameAs(party));
        Assert.That(third.Party, Is.SameAs(party));
    }

    private async ValueTask<(Party Party, Player Member1, Player Member2)> CreatePartyWithTwoMembersAsync()
    {
        var member1 = await this.CreatePartyMemberAsync().ConfigureAwait(false);
        var member2 = await this.CreatePartyMemberAsync().ConfigureAwait(false);
        var party = this._partyManager.CreateParty();
        await party.AddAsync(member1).ConfigureAwait(false);
        await party.AddAsync(member2).ConfigureAwait(false);
        return (party, member1, member2);
    }

    private static byte GetPartyMemberIndex(Party party, IPartyMember member)
    {
        for (byte index = 0; index < party.PartyList.Count; index++)
        {
            if (party.PartyList[index] == member)
            {
                return index;
            }
        }

        throw new ArgumentException("The member is not part of the party.", nameof(member));
    }

    private async ValueTask<Player> CreatePartyMemberAsync()
    {
        var result = await PlayerTestHelper.CreatePlayerAsync(GameContextTestHelper.CreateGameContext()).ConfigureAwait(false);
        await result.PlayerState.TryAdvanceToAsync(PlayerState.EnteredWorld).ConfigureAwait(false);
        return result;
    }
}
