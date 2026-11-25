// <copyright file="DuelBetSide.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.DuelBetting;

/// <summary>The fighter chosen on a bet.</summary>
public enum DuelBetSide
{
    /// <summary>The duel requester.</summary>
    Requester = 0,

    /// <summary>The challenged fighter.</summary>
    Opponent = 1,
}
