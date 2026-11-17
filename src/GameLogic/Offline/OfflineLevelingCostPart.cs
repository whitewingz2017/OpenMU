// <copyright file="OfflineLevelingCostPart.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Offline;

/// <summary>An amount of a currency an offline leveling session costs.</summary>
/// <param name="Currency">The currency.</param>
/// <param name="Amount">The amount.</param>
public readonly record struct OfflineLevelingCostPart(OfflineLevelingCurrency Currency, long Amount);
