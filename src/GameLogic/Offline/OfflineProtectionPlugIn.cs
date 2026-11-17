// <copyright file="OfflineProtectionPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Offline;

using System.ComponentModel;
using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// The protection of a paid offline session: players (and their pets and summons) can't damage the protected
/// character, and it doesn't damage players either. Monsters still hit it.
/// </summary>
[PlugIn]
[Display(Name = nameof(OfflineLevelingResources.ProtectionName), Description = nameof(OfflineLevelingResources.ProtectionDescription), ResourceType = typeof(OfflineLevelingResources))]
[Guid("b3f8e8ca-137d-49c6-b7f7-bf1c69c29375")]
public sealed class OfflineProtectionPlugIn : IPlayerAttackRestrictionPlugIn
{
    /// <inheritdoc />
    public void CheckAttack(Player attacker, Player defender, CancelEventArgs eventArgs)
    {
        if (defender is OfflinePlayer { IsProtectedFromPlayers: true } || attacker is OfflinePlayer { IsProtectedFromPlayers: true })
        {
            eventArgs.Cancel = true;
        }
    }
}
