// <copyright file="MarketView.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView;

using System.Runtime.InteropServices;
using System.Text.Encodings.Web;
using System.Text.Json;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic.Market;
using MUnique.OpenMU.GameLogic.Views;
using MUnique.OpenMU.Network;
using MUnique.OpenMU.Network.Packets.ServerToClient;
using MUnique.OpenMU.Network.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>Sends the Marketplace to the custom client.</summary>
[PlugIn]
[Display(Name = nameof(MarketResources.ViewName), Description = nameof(MarketResources.ViewDescription), ResourceType = typeof(MarketResources))]
[Guid("407477f1-2a16-4f76-be49-dc6c0326f242")]
[MinimumClient(106, 3, ClientLanguage.Invariant)]
public sealed class MarketView : IMarketView
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private readonly RemotePlayer _player;

    /// <summary>Initializes a new instance of the <see cref="MarketView"/> class.</summary>
    /// <param name="player">The receiving player.</param>
    public MarketView(RemotePlayer player) => this._player = player;

    /// <summary>Serializes an item as the client's inventory list carries it.</summary>
    /// <param name="player">The player whose client gets the bytes.</param>
    /// <param name="item">The item.</param>
    /// <returns>The bytes, hex.</returns>
    internal static string SerializeItem(RemotePlayer player, Item item)
    {
        var serializer = player.ItemSerializer;
        Span<byte> buffer = stackalloc byte[serializer.NeededSpace];
        var length = serializer.SerializeItem(buffer, item);
        return Convert.ToHexString(buffer[..length]);
    }

    /// <inheritdoc />
    public async ValueTask ShowAsync(MarketAnswer answer)
    {
        if (this._player.Connection is not { } connection)
        {
            return;
        }

        var data = JsonSerializer.SerializeToUtf8Bytes(answer, SerializerOptions);
        await connection.SendAsync(() =>
        {
            var size = MarketResponseExtendedRef.GetRequiredSize(data.Length);
            var packet = new MarketResponseExtendedRef(connection.Output.GetSpan(size)[..size]);
            data.CopyTo(packet.Data);
            return size;
        }).ConfigureAwait(false);
    }
}
