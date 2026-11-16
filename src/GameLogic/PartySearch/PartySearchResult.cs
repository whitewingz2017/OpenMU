// <copyright file="PartySearchResult.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PartySearch;

/// <summary>The result of a Party Search request, as the client shows it.</summary>
public enum PartySearchResult
{
    /// <summary>Nothing happened; the list was loaded.</summary>
    None = 0,

    /// <summary>The player's party is listed with the saved settings.</summary>
    Listed = 1,

    /// <summary>The player's listing was removed.</summary>
    Removed = 2,

    /// <summary>The player joined the party.</summary>
    Joined = 3,

    /// <summary>The Party Search is disabled on this server.</summary>
    Disabled = 10,

    /// <summary>The request couldn't be read or its settings are out of range.</summary>
    Invalid = 11,

    /// <summary>Only the party master can list a party.</summary>
    NotPartyMaster = 12,

    /// <summary>The list holds no more parties.</summary>
    ListFull = 13,

    /// <summary>The party isn't listed (anymore).</summary>
    NotListed = 14,

    /// <summary>The player tried to join the own listing.</summary>
    OwnListing = 15,

    /// <summary>The player is in a party already.</summary>
    AlreadyInParty = 16,

    /// <summary>The password doesn't match.</summary>
    WrongPassword = 17,

    /// <summary>The player's level is below the listing's minimum.</summary>
    LevelTooLow = 18,

    /// <summary>The listing doesn't look for the player's class.</summary>
    ClassNotWanted = 19,

    /// <summary>The listing only takes members of the leader's guild.</summary>
    GuildOnly = 20,

    /// <summary>The party is full.</summary>
    PartyFull = 21,

    /// <summary>A party isn't possible where the player or the leader is, e.g. in an event.</summary>
    NotPossibleHere = 22,

    /// <summary>Another plugin denied the party, e.g. because of the gens.</summary>
    Denied = 23,

    /// <summary>A wrong password was given a moment ago.</summary>
    TryLater = 24,
}
