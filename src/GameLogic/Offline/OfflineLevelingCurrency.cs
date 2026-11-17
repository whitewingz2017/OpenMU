// <copyright file="OfflineLevelingCurrency.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Offline;

/// <summary>The currencies an offline leveling session can be paid with.</summary>
public enum OfflineLevelingCurrency
{
    /// <summary>The character's zen.</summary>
    Zen = 0,

    /// <summary>The account's WCoinC.</summary>
    WCoinC = 1,

    /// <summary>The account's Goblin Points.</summary>
    GoblinPoints = 2,
}
