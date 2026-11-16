// <copyright file="PartySearchListing.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PartySearch;

/// <summary>A party which its leader listed in the Party Search.</summary>
public sealed class PartySearchListing
{
    /// <summary>The flags of all seven classes; a class is its bit, from dark wizard (0) to rage fighter (6).</summary>
    public const byte AllClasses = 0x7F;

    /// <summary>Gets the character name of the leader.</summary>
    public string LeaderName { get; init; } = string.Empty;

    /// <summary>Gets the lowest character level which may join.</summary>
    public int MinimumLevel { get; init; }

    /// <summary>Gets a value indicating whether only members of the leader's guild may join.</summary>
    public bool GuildOnly { get; init; }

    /// <summary>Gets a value indicating whether the party takes one character of each class only.</summary>
    public bool OnePerClass { get; init; }

    /// <summary>Gets the flags of the wanted classes (<see cref="AllClasses"/>).</summary>
    public byte Classes { get; init; } = AllClasses;

    /// <summary>Gets the password a player has to give to join, or an empty string.</summary>
    public string Password { get; init; } = string.Empty;
}
