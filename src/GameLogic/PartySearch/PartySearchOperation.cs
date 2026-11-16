// <copyright file="PartySearchOperation.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PartySearch;

/// <summary>The operations of a Party Search request.</summary>
public enum PartySearchOperation : byte
{
    /// <summary>Loads the player's own listing and the listed parties.</summary>
    Load = 0,

    /// <summary>Saves or removes the player's own listing.</summary>
    Save = 1,

    /// <summary>Joins a listed party.</summary>
    Join = 2,
}
