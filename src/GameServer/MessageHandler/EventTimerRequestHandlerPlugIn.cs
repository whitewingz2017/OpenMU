// <copyright file="EventTimerRequestHandlerPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.MessageHandler;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.PlayerActions;
using MUnique.OpenMU.Network.Packets.ClientToServer;
using MUnique.OpenMU.PlugIns;

/// <summary>Handles event schedule requests from the custom client.</summary>
[PlugIn]
[Display(Name = nameof(PlugInResources.EventTimerRequestHandler_Name), Description = nameof(PlugInResources.EventTimerRequestHandler_Description), ResourceType = typeof(PlugInResources))]
[Guid("c5f7a366-d5ce-4f10-ad33-e0f763058d17")]
public sealed class EventTimerRequestHandlerPlugIn : IPacketHandlerPlugIn
{
    private readonly EventTimerRequestAction _action = new();

    /// <inheritdoc />
    public byte Key => EventTimerRequestExtended.Code;

    /// <inheritdoc />
    public bool IsEncryptionExpected => false;

    /// <inheritdoc />
    public async ValueTask HandlePacketAsync(Player player, Memory<byte> packet)
    {
        if (packet.Length != EventTimerRequestExtended.Length || packet.Span[0] != 0xC1
            || packet.Span[1] != packet.Length || packet.Span[2] != this.Key || packet.Span[3] != EventTimerRequestExtended.SubCode)
        {
            return;
        }

        EventTimerRequestExtended request = packet;
        await this._action.RequestAsync(player, request.RequestId).ConfigureAwait(false);
    }
}
