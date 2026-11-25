// <copyright file="DuelBetTicket.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.DuelBetting;

/// <summary>A confirmed stake; only the future escrow transaction may submit one to a book.</summary>
/// <param name="Id">The immutable request/ticket identity.</param>
/// <param name="AccountId">The bettor's account identity.</param>
/// <param name="Currency">The stake currency.</param>
/// <param name="Side">The chosen fighter.</param>
/// <param name="Amount">The confirmed positive stake.</param>
public sealed record DuelBetTicket(Guid Id, Guid AccountId, DuelBetCurrency Currency, DuelBetSide Side, int Amount);
