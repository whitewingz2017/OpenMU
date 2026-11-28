// <copyright file="IDuelBettingView.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Views.Duel;

using MUnique.OpenMU.GameLogic.DuelBetting;

/// <summary>Shows the live public duel board and a betting action's outcome.</summary>
public interface IDuelBettingView : IViewPlugIn
{
    /// <summary>Sends a fresh view of the board.</summary>
    /// <param name="requestId">The answered request.</param>
    /// <param name="enabled">Whether new challenges may opt into bets.</param>
    /// <param name="status">The localized answer to a mutation, when present.</param>
    /// <param name="duels">The publicly listed duels.</param>
    /// <param name="publicSeconds">The server's public countdown.</param>
    /// <param name="privateSeconds">The server's regular countdown.</param>
    /// <param name="firstTo">The server's winning score for new matches.</param>
    /// <param name="wagerMinimum">The smallest allowed WCoinC wager.</param>
    /// <param name="wagerMaximum">The largest allowed WCoinC wager.</param>
    /// <param name="setupStatus">Explains why wagers are unavailable.</param>
    /// <returns>The asynchronous completion.</returns>
    ValueTask ShowAsync(uint requestId, bool enabled, string status, IReadOnlyList<DuelBetBoardEntry> duels, int publicSeconds = 0, int privateSeconds = 0, int firstTo = 0, int wagerMinimum = 0, int wagerMaximum = 0, string setupStatus = "");

    /// <summary>Provides presentation details for the following standard invitation packet.</summary>
    /// <param name="invitation">The incoming challenge settings.</param>
    /// <returns>The asynchronous completion.</returns>
    ValueTask ShowInvitationAsync(DuelBetInvitation invitation);
}
