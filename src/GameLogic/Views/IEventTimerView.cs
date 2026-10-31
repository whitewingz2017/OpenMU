// <copyright file="IEventTimerView.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Views;

/// <summary>Shows the current server's scheduled events to a requesting client.</summary>
public interface IEventTimerView : IViewPlugIn
{
    /// <summary>Sends a schedule snapshot with its server reference time.</summary>
    /// <param name="requestId">The request to answer.</param>
    /// <param name="serverUtc">The server reference time.</param>
    /// <param name="events">The event names and next start times.</param>
    /// <param name="active">The active event names and end times.</param>
    ValueTask ShowAsync(uint requestId, DateTime serverUtc, IReadOnlyList<(string Name, DateTime StartsAtUtc)> events,
        IReadOnlyList<(string Name, DateTime EndsAtUtc)> active);
}
