// <copyright file="OfflineLevelingRules.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Offline;

using MUnique.OpenMU.GameLogic.Attributes;

/// <summary>When a player may go offline leveling: alive, on a map, outside events, with the MU Helper running.</summary>
public static class OfflineLevelingRules
{
    /// <summary>Checks whether the player may start an offline session now.</summary>
    /// <param name="player">The player.</param>
    /// <returns><see cref="OfflineLevelingResult.None"/>, or why not.</returns>
    public static OfflineLevelingResult Check(Player player)
    {
        if (player.SelectedCharacter is null || player.Account?.LoginName is not { } loginName)
        {
            return OfflineLevelingResult.Invalid;
        }

        if (!player.IsAlive)
        {
            return OfflineLevelingResult.Dead;
        }

        if (player.CurrentMap is null)
        {
            return OfflineLevelingResult.NotOnMap;
        }

        // Going offline disconnects the player, which would let the offline player rejoin the event.
        if (player.CurrentMiniGame is not null)
        {
            return OfflineLevelingResult.InMiniGame;
        }

        if (player.Attributes?[Stats.IsMuHelperActive] <= 0)
        {
            return OfflineLevelingResult.MuHelperOff;
        }

        return player.GameContext.OfflinePlayerManager.IsActive(loginName)
            ? OfflineLevelingResult.AlreadyActive
            : OfflineLevelingResult.None;
    }
}
