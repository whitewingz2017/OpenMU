// <copyright file="MuunInactiveReason.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Muun;

/// <summary>
/// Why a stored Muun currently grants no bonus. The numeric values are sent to the custom client, so they are a contract.
/// </summary>
public enum MuunInactiveReason
{
    /// <summary>The Muun is active.</summary>
    None = 0,

    /// <summary>The Muun is in a collection cell, not equipped.</summary>
    NotEquipped = 1,

    /// <summary>The Muun has no life left.</summary>
    NoLife = 2,

    /// <summary>The Muun item level exceeds the species maximum.</summary>
    LevelTooHigh = 3,

    /// <summary>The Muun has expired.</summary>
    Expired = 4,

    /// <summary>The character level is outside the configured range.</summary>
    CharacterLevel = 5,

    /// <summary>The character is not on one of the configured maps.</summary>
    Map = 6,

    /// <summary>The current UTC week day is not configured.</summary>
    Day = 7,

    /// <summary>The current UTC hour is outside the configured daily window.</summary>
    Hour = 8,
}
