// <copyright file="EventTimerView.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView;

using System.Runtime.InteropServices;
using System.Text.Json;
using MUnique.OpenMU.GameLogic.Views;
using MUnique.OpenMU.Network;
using MUnique.OpenMU.Network.Packets.ServerToClient;
using MUnique.OpenMU.PlugIns;

/// <summary>Sends schedules only in response to custom client requests.</summary>
[PlugIn]
[Display(Name = nameof(PlugInResources.EventTimerView_Name), Description = nameof(PlugInResources.EventTimerView_Description), ResourceType = typeof(PlugInResources))]
[Guid("f84adf12-5a57-4dbe-a156-f6f2a2f30ae1")]
public sealed class EventTimerView : IEventTimerView
{
    private readonly RemotePlayer _player;

    /// <summary>Initializes a new instance of the <see cref="EventTimerView"/> class.</summary>
    /// <param name="player">The receiving player.</param>
    public EventTimerView(RemotePlayer player) => this._player = player;

    /// <inheritdoc />
    public async ValueTask ShowAsync(uint requestId, DateTime serverUtc, IReadOnlyList<(string Name, DateTime StartsAtUtc)> events)
    {
        if (this._player.Connection is not { } connection)
        {
            return;
        }

        var data = JsonSerializer.SerializeToUtf8Bytes(new
        {
            RequestId = requestId,
            ServerUtc = serverUtc,
            Rows = events.Select(e => new { e.Name, e.StartsAtUtc }),
        });
        await connection.SendAsync(() =>
        {
            var size = EventTimerResponseExtendedRef.GetRequiredSize(data.Length);
            var response = new EventTimerResponseExtendedRef(connection.Output.GetSpan(size)[..size]);
            data.CopyTo(response.Data);
            return size;
        }).ConfigureAwait(false);
    }
}
