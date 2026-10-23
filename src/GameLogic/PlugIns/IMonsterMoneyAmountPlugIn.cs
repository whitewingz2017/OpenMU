// <copyright file="IMonsterMoneyAmountPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using System.Runtime.InteropServices;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Changes monster money before party sharing, player bonuses, and ground or direct payout.
/// </summary>
[Guid("7E726CB8-CFC6-465E-B907-6318F7AC7E05")]
[PlugInPoint("Monster money amount", "Plugins which calculate money before it is dropped or paid directly.")]
public interface IMonsterMoneyAmountPlugIn
{
    /// <summary>
    /// Calculates the total money for the kill, before player bonuses.
    /// </summary>
    /// <param name="args">The monster and the amount to modify.</param>
    ValueTask CalculateMoneyAmountAsync(MonsterMoneyAmountArgs args);
}
