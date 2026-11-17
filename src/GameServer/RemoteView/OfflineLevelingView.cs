// <copyright file="OfflineLevelingView.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView;

using System.Runtime.InteropServices;
using System.Text.Encodings.Web;
using System.Text.Json;
using MUnique.OpenMU.GameLogic.Offline;
using MUnique.OpenMU.GameLogic.Views;
using MUnique.OpenMU.Network;
using MUnique.OpenMU.Network.Packets.ServerToClient;
using MUnique.OpenMU.Network.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>Sends the offline leveling window's offer to the custom client.</summary>
[PlugIn]
[Display(Name = nameof(OfflineLevelingResources.ViewName), Description = nameof(OfflineLevelingResources.ViewDescription), ResourceType = typeof(OfflineLevelingResources))]
[Guid("82eb818b-6419-45ca-bd6c-6fb3226332df")]
[MinimumClient(106, 3, ClientLanguage.Invariant)]
public sealed class OfflineLevelingView : IOfflineLevelingView
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private readonly RemotePlayer _player;

    /// <summary>Initializes a new instance of the <see cref="OfflineLevelingView"/> class.</summary>
    /// <param name="player">The receiving player.</param>
    public OfflineLevelingView(RemotePlayer player) => this._player = player;

    /// <inheritdoc />
    public async ValueTask ShowAsync(OfflineLevelingAnswer answer)
    {
        if (this._player.Connection is not { } connection)
        {
            return;
        }

        var data = JsonSerializer.SerializeToUtf8Bytes(answer, SerializerOptions);
        await connection.SendAsync(() =>
        {
            var size = OfflineLevelingResponseExtendedRef.GetRequiredSize(data.Length);
            var packet = new OfflineLevelingResponseExtendedRef(connection.Output.GetSpan(size)[..size]);
            data.CopyTo(packet.Data);
            return size;
        }).ConfigureAwait(false);
    }
}
