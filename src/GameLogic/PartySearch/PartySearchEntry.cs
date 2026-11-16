// <copyright file="PartySearchEntry.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PartySearch;

/// <summary>A listed party as one player sees it in the Party Search.</summary>
/// <param name="Leader">The character name of the leader.</param>
/// <param name="Map">The number of the leader's map.</param>
/// <param name="X">The leader's x coordinate.</param>
/// <param name="Y">The leader's y coordinate.</param>
/// <param name="Level">The leader's level.</param>
/// <param name="MinimumLevel">The lowest character level which may join.</param>
/// <param name="Members">The number of party members; one when the leader has no party yet.</param>
/// <param name="MaxMembers">The most members a party has.</param>
/// <param name="Classes">The flags of the classes the party still looks for.</param>
/// <param name="GuildOnly">Whether only members of the leader's guild may join.</param>
/// <param name="SameGuild">Whether the viewer is in the leader's guild.</param>
/// <param name="Password">Whether joining needs a password.</param>
/// <param name="Joinable">Whether the viewer meets the listing's level, class and guild rules and the party has room.</param>
public sealed record PartySearchEntry(
    string Leader,
    short Map,
    byte X,
    byte Y,
    int Level,
    int MinimumLevel,
    int Members,
    int MaxMembers,
    int Classes,
    bool GuildOnly,
    bool SameGuild,
    bool Password,
    bool Joinable);
