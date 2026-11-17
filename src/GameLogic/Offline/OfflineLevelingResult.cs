// <copyright file="OfflineLevelingResult.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Offline;

/// <summary>The result of an offline leveling request, as the window explains it.</summary>
public enum OfflineLevelingResult
{
    /// <summary>The offer was loaded.</summary>
    None = 0,

    /// <summary>The session starts; the client closes.</summary>
    Started = 1,

    /// <summary>The paid offline leveling is disabled.</summary>
    Disabled = 10,

    /// <summary>The request couldn't be read.</summary>
    Invalid = 11,

    /// <summary>The character is dead.</summary>
    Dead = 12,

    /// <summary>The character isn't on a map.</summary>
    NotOnMap = 13,

    /// <summary>The character is in an event.</summary>
    InMiniGame = 14,

    /// <summary>The MU Helper isn't running.</summary>
    MuHelperOff = 15,

    /// <summary>An offline session of the account runs already.</summary>
    AlreadyActive = 16,

    /// <summary>The currency, the hours or the protection isn't allowed.</summary>
    NotAllowed = 17,

    /// <summary>The player can't pay the first hour, or the prepaid hours.</summary>
    NotEnough = 18,

    /// <summary>The session couldn't be started.</summary>
    Failed = 19,
}
