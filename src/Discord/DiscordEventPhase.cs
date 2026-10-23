// <copyright file="DiscordEventPhase.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Discord;

/// <summary>The live phase of a scheduled mini game.</summary>
public enum DiscordEventPhase
{
    /// <summary>Waiting for the scheduled opening.</summary>
    Opening,

    /// <summary>The entrance is open.</summary>
    Entrance,

    /// <summary>Entry is closed and the event is running.</summary>
    Active,
}
