// <copyright file="IDailyRewardView.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Views.Inventory;

using System.Runtime.InteropServices;

/// <summary>The view of the daily login reward calendar.</summary>
[Guid("734a2570-7852-4ef1-946e-f105fc5fe662")]
public interface IDailyRewardView : IViewPlugIn
{
    /// <summary>Sends the rewards of the current month and the days the account claimed.</summary>
    /// <param name="requestId">The processed request identifier; zero when the player entered the world.</param>
    /// <param name="messageKey">The localized status key, or <c>null</c> for a ready calendar.</param>
    ValueTask ShowAsync(uint requestId, string? messageKey);
}
