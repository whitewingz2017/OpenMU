// <copyright file="StoreMailAttachmentPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView.Messenger;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.Views.Messenger;
using MUnique.OpenMU.Network;
using MUnique.OpenMU.Network.Packets.ServerToClient;
using MUnique.OpenMU.Network.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>Marks server-owned mail attachments for the TwinMU claim button.</summary>
[PlugIn]
[MinimumClient(106, 3, ClientLanguage.Invariant)]
[Guid("00a71c8a-937d-4bdc-bfac-10b27d22fac8")]
public sealed class StoreMailAttachmentPlugIn(RemotePlayer player) : IStoreMailAttachmentPlugIn
{
    /// <inheritdoc />
    public ValueTask ShowAttachmentAsync(ushort index, bool available)
        => player.Connection is { } connection ? connection.SendStoreMailAttachmentResponseAsync(index, available) : ValueTask.CompletedTask;
}
