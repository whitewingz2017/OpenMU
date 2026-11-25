// <copyright file="DuelBetPayout.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.DuelBetting;

/// <summary>A settlement instruction, including the original stake; it does not move money itself.</summary>
/// <param name="Ticket">The settled ticket.</param>
/// <param name="Amount">The payout in that ticket's currency.</param>
/// <param name="Refund">Whether this returns an unresolvable or uncontested stake.</param>
public sealed record DuelBetPayout(DuelBetTicket Ticket, int Amount, bool Refund);
