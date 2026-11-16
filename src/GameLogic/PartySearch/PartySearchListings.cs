// <copyright file="PartySearchListings.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PartySearch;

using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.Offline;

/// <summary>
/// The rules of the Party Search: which listings still stand, which classes a party still looks for, and who
/// may join it.
/// </summary>
public static class PartySearchListings
{
    /// <summary>The classes of the client's class flags: the character class number divided by this.</summary>
    private const int ClassNumbersPerClass = 4;

    /// <summary>The number of classes with a flag.</summary>
    private const int FlaggedClasses = 7;

    /// <summary>Gets the player's own listing.</summary>
    /// <param name="player">The player.</param>
    /// <returns>The listing, or <c>null</c> if the player has none.</returns>
    public static PartySearchListing? OwnListing(Player player)
        => PartySearchFeaturePlugIn.Listings(player.GameContext)?.Find(player.Name);

    /// <summary>Gets the listed parties as <paramref name="viewer"/> sees them.</summary>
    /// <param name="viewer">The player who looks at the list.</param>
    /// <returns>The listed parties.</returns>
    public static IReadOnlyList<PartySearchEntry> Entries(Player viewer)
    {
        if (PartySearchFeaturePlugIn.Listings(viewer.GameContext) is not { } registry)
        {
            return [];
        }

        var entries = new List<PartySearchEntry>();
        foreach (var listing in registry.Snapshot())
        {
            if (LeaderOf(viewer.GameContext, registry, listing) is not { } leader)
            {
                continue;
            }

            entries.Add(new PartySearchEntry(
                leader.Name,
                leader.CurrentMap?.Definition.Number ?? (short)0,
                leader.Position.X,
                leader.Position.Y,
                LevelOf(leader),
                listing.MinimumLevel,
                MemberCount(leader),
                MaximumMembers(leader),
                WantedClasses(listing, leader),
                listing.GuildOnly,
                SameGuild(viewer, leader),
                listing.Password.Length > 0,
                Requirements(viewer, leader, listing) == PartySearchResult.None));
        }

        return entries;
    }

    /// <summary>Determines whether a whispered message is the password of the receiver's listing.</summary>
    /// <param name="receiver">The player who receives the whisper.</param>
    /// <param name="message">The whispered message.</param>
    /// <returns><c>true</c>, if the receiver listed a party with this password.</returns>
    public static bool IsPassword(Player receiver, string message)
        => OwnListing(receiver) is { Password.Length: > 0 } listing
           && string.Equals(message.Trim(), listing.Password, StringComparison.Ordinal);

    /// <summary>Gets the leader of a listing if the listing still stands, and removes it otherwise.</summary>
    /// <param name="context">The game context.</param>
    /// <param name="registry">The listed parties.</param>
    /// <param name="listing">The listing.</param>
    /// <returns>The leader, or <c>null</c> if the listing ended.</returns>
    internal static Player? LeaderOf(IGameContext context, PartySearchRegistry registry, PartySearchListing listing)
    {
        var leader = context.GetPlayerByCharacterName(listing.LeaderName);
        var leads = leader is { SelectedCharacter: not null } and not OfflinePlayer
                    && (leader.Party is null || Equals(leader.Party.PartyMaster, leader));
        if (leads)
        {
            return leader;
        }

        registry.Remove(listing.LeaderName);
        return null;
    }

    /// <summary>Checks whether <paramref name="player"/> may join the listed party; the password aside.</summary>
    /// <param name="player">The player who wants to join.</param>
    /// <param name="leader">The leader.</param>
    /// <param name="listing">The leader's listing.</param>
    /// <returns><see cref="PartySearchResult.None"/>, or why the player can't join.</returns>
    internal static PartySearchResult Requirements(Player player, Player leader, PartySearchListing listing)
    {
        if (Equals(player, leader))
        {
            return PartySearchResult.OwnListing;
        }

        if (player.Party is not null)
        {
            return PartySearchResult.AlreadyInParty;
        }

        if (LevelOf(player) < listing.MinimumLevel)
        {
            return PartySearchResult.LevelTooLow;
        }

        if (listing.GuildOnly && !SameGuild(player, leader))
        {
            return PartySearchResult.GuildOnly;
        }

        if ((WantedClasses(listing, leader) & ClassFlag(player)) == 0)
        {
            return PartySearchResult.ClassNotWanted;
        }

        if (MemberCount(leader) >= MaximumMembers(leader))
        {
            return PartySearchResult.PartyFull;
        }

        if (player.CurrentMiniGame?.Definition.AllowParty is false || leader.CurrentMiniGame?.Definition.AllowParty is false)
        {
            return PartySearchResult.NotPossibleHere;
        }

        return PartySearchResult.None;
    }

    /// <summary>Gets the flag of a player's class: dark wizard (bit 0) to rage fighter (bit 6).</summary>
    /// <param name="player">The player.</param>
    /// <returns>The flag, or zero for a class without one.</returns>
    internal static int ClassFlag(Player player)
    {
        var index = (player.SelectedCharacter?.CharacterClass?.Number ?? byte.MaxValue) / ClassNumbersPerClass;
        return index < FlaggedClasses ? 1 << index : 0;
    }

    private static int LevelOf(Player player) => (int)(player.Attributes?[Stats.Level] ?? 0);

    private static int MemberCount(Player leader) => leader.Party?.PartyList.Count ?? 1;

    private static int MaximumMembers(Player leader)
        => leader.Party?.MaxPartySize ?? leader.GameContext.Configuration.MaximumPartySize;

    private static bool SameGuild(Player player, Player leader)
        => player.GuildStatus?.GuildId is { } guild && guild == leader.GuildStatus?.GuildId;

    // With one per class, the classes in the party are no longer wanted.
    private static int WantedClasses(PartySearchListing listing, Player leader)
    {
        int classes = listing.Classes == 0 ? PartySearchListing.AllClasses : listing.Classes;
        if (!listing.OnePerClass)
        {
            return classes;
        }

        IEnumerable<IPartyMember> members = leader.Party?.PartyList ?? new IPartyMember[] { leader };
        foreach (var member in members.OfType<Player>())
        {
            classes &= ~ClassFlag(member);
        }

        return classes;
    }
}
