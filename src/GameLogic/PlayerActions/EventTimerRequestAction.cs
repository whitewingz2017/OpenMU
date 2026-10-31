// <copyright file="EventTimerRequestAction.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions;

using MUnique.OpenMU.GameLogic.PlugIns.PeriodicTasks;
using MUnique.OpenMU.GameLogic.Views;

/// <summary>Returns configured event schedules on the player's game server.</summary>
public sealed class EventTimerRequestAction
{
    /// <summary>Answers a timer request from an entered player.</summary>
    /// <param name="player">The requesting player.</param>
    /// <param name="requestId">The request identifier.</param>
    public async ValueTask RequestAsync(Player player, uint requestId)
    {
        if (player.SelectedCharacter is null || !player.IsConnected)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var context = player.GameContext;
        var events = UpcomingEvents.Get(context.PlugInManager, now, context.ServerTimeZone, TimeSpan.FromDays(7), player.Culture, context)
            .Take(128).ToList();
        await player.InvokeViewPlugInAsync<IEventTimerView>(view => view.ShowAsync(requestId, now, events, UpcomingEvents.GetActive(context, now, player.Culture).Take(128).ToList())).ConfigureAwait(false);
    }
}
