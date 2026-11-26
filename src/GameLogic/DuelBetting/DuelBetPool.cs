// <copyright file="DuelBetPool.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.DuelBetting;

/// <summary>The current stakes in one currency.</summary>
/// <param name="Currency">The currency.</param>
/// <param name="RequesterStake">The stake on the first fighter.</param>
/// <param name="OpponentStake">The stake on the second fighter.</param>
public sealed record DuelBetPool(DuelBetCurrency Currency, int RequesterStake, int OpponentStake);
