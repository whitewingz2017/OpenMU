// <copyright file="PartySearchRequestData.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PartySearch;

/// <summary>The UTF-8 JSON a Party Search request carries: the listing to save, or the party to join.</summary>
public sealed class PartySearchRequestData
{
    /// <summary>Gets or sets a value indicating whether the party is listed; <c>false</c> removes the listing.</summary>
    public bool Active { get; set; }

    /// <summary>Gets or sets the lowest character level which may join.</summary>
    public int MinimumLevel { get; set; }

    /// <summary>Gets or sets a value indicating whether only members of the leader's guild may join.</summary>
    public bool GuildOnly { get; set; }

    /// <summary>Gets or sets a value indicating whether the party takes one character of each class only.</summary>
    public bool OnePerClass { get; set; }

    /// <summary>Gets or sets the flags of the wanted classes; zero means every class.</summary>
    public int Classes { get; set; }

    /// <summary>Gets or sets the password: of the listing to save, or given to join.</summary>
    public string? Password { get; set; }

    /// <summary>Gets or sets the character name of the leader whose party to join.</summary>
    public string? Leader { get; set; }
}
