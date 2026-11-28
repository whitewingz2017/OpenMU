// <copyright file="DuelBetRequest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.DuelBetting;

/// <summary>The payload of a challenge or stake request.</summary>
internal sealed class DuelBetRequest
{
    /// <summary>Gets the selected match identity.</summary>
    public Guid Duel { get; init; }

    /// <summary>Gets the unique ticket identity, retained on retry.</summary>
    public Guid Ticket { get; init; }

    /// <summary>Gets the fighter receiving the stake.</summary>
    public DuelBetSide? Side { get; init; }

    /// <summary>Gets the staked jewel.</summary>
    public DuelBetCurrency? Currency { get; init; }

    /// <summary>Gets the amount staked.</summary>
    public int Amount { get; init; }

    /// <summary>Gets the observed player identity for the challenge.</summary>
    public ushort TargetId { get; init; }

    /// <summary>Gets the observed player's name.</summary>
    public string? TargetName { get; init; }

    /// <summary>Gets the wager offer being answered.</summary>
    public Guid OfferId { get; init; }

    /// <summary>Gets the opponent's explicit response to the wager terms.</summary>
    public bool? Accepted { get; init; }
}
