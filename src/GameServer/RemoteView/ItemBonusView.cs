// <copyright file="ItemBonusView.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView;

using System.Runtime.InteropServices;
using System.Text.Encodings.Web;
using System.Text.Json;
using MUnique.OpenMU.GameLogic.ItemBonuses;
using MUnique.OpenMU.GameLogic.Views;
using MUnique.OpenMU.Network;
using MUnique.OpenMU.Network.Packets.ServerToClient;
using MUnique.OpenMU.Network.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>Sends the picked items' attribute lines to the custom client.</summary>
[PlugIn]
[Display(Name = nameof(ItemBonusResources.ViewName), Description = nameof(ItemBonusResources.ViewDescription), ResourceType = typeof(ItemBonusResources))]
[Guid("d14c6a83-2f5e-4b07-a9c1-6e3b8f0d2a95")]
[MinimumClient(106, 3, ClientLanguage.Invariant)]
public sealed class ItemBonusView : IItemBonusView
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private readonly RemotePlayer _player;

    /// <summary>Initializes a new instance of the <see cref="ItemBonusView"/> class.</summary>
    /// <param name="player">The receiving player.</param>
    public ItemBonusView(RemotePlayer player) => this._player = player;

    /// <inheritdoc />
    public async ValueTask ShowAsync(ItemBonusAnswer answer)
    {
        if (this._player.Connection is not { } connection)
        {
            return;
        }

        var data = JsonSerializer.SerializeToUtf8Bytes(answer, SerializerOptions);
        await connection.SendAsync(() =>
        {
            var size = ItemBonusesResponseExtendedRef.GetRequiredSize(data.Length);
            var packet = new ItemBonusesResponseExtendedRef(connection.Output.GetSpan(size)[..size]);
            data.CopyTo(packet.Data);
            return size;
        }).ConfigureAwait(false);
    }
}
