// <copyright file="DuelBetOperation.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.DuelBetting;

/// <summary>Operations requested by the betting board or challenge dialog.</summary>
internal enum DuelBetOperation : byte
{
    /// <summary>Refreshes the public board.</summary>
    Load,

    /// <summary>Places a stake.</summary>
    Place,

    /// <summary>Challenges an observed player and opts into betting.</summary>
    Challenge,

    /// <summary>Claims available payouts.</summary>
    Claim,

    /// <summary>Joins the spectator area of a public match.</summary>
    Spectate,

    /// <summary>Accepts or declines the wager offer shown to the invited duelist.</summary>
    RespondToWager,
}
