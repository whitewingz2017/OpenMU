// <copyright file="BloodCastleQuestItemCleanupPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.MiniGames;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Removes the archangel quest item (the blood castle weapon) from a character which enters the
/// world outside of a running blood castle, e.g. after a disconnect during the event. The event
/// itself only drops or removes it while the player is still on its map.
/// </summary>
[PlugIn]
[Display(Name = "Blood Castle quest item cleanup", Description = "Removes a left-over archangel weapon when a character enters the world.")]
[Guid("3c8e5b7a-1f4d-4a2e-9b6c-8d0e2f7a5c91")]
public sealed class BloodCastleQuestItemCleanupPlugIn : IPlayerStateChangedPlugIn
{
    /// <inheritdoc />
    public async ValueTask PlayerStateChangedAsync(Player player, State previousState, State currentState)
    {
        if (currentState != PlayerState.EnteredWorld || player.CurrentMiniGame is BloodCastleContext)
        {
            return;
        }

        var leftOvers = player.Inventory?.Items.Where(i => i.IsArchangelQuestItem()).ToList() ?? [];
        foreach (var item in leftOvers)
        {
            await player.DestroyInventoryItemAsync(item).ConfigureAwait(false);
        }
    }
}
