// <copyright file="OfflineLevelingOperation.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Offline;

/// <summary>The operations of an offline leveling request.</summary>
public enum OfflineLevelingOperation : byte
{
    /// <summary>Loads the offer.</summary>
    Load = 0,

    /// <summary>Starts the session with the request's <see cref="OfflineLevelingPlan"/>.</summary>
    Start = 1,
}
