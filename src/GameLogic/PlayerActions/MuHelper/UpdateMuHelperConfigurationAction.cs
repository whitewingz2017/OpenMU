// <copyright file="UpdateMuHelperConfigurationAction.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions.MuHelper;

/// <summary>
/// Action to update the mu helper configuration.
/// </summary>
public class UpdateMuHelperConfigurationAction
{
    /// <summary>
    /// Toggle mu bot status.
    /// </summary>
    /// <param name="player">the player.</param>
    /// <param name="data">mu bot data to be saved.</param>
    public async ValueTask SaveDataAsync(Player player, Memory<byte> data)
    {
        if (player.SelectedCharacter is null)
        {
            return;
        }

        await GameLogic.MuHelper.PvpCounterattackPayment.SaveAsync(player, data).ConfigureAwait(false);
    }
}
