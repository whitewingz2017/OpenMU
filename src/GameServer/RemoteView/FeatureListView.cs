// <copyright file="FeatureListView.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView;

using System.Runtime.InteropServices;
using System.Text.Encodings.Web;
using System.Text.Json;
using MUnique.OpenMU.GameLogic.Features;
using MUnique.OpenMU.GameLogic.Views;
using MUnique.OpenMU.Network;
using MUnique.OpenMU.Network.Packets.ServerToClient;
using MUnique.OpenMU.Network.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>Tells the custom client which of its features are active.</summary>
[PlugIn]
[Display(Name = nameof(FeatureListResources.ViewName), Description = nameof(FeatureListResources.ViewDescription), ResourceType = typeof(FeatureListResources))]
[Guid("4e7b9d21-c36a-4a58-b0f2-8d19e6a3c470")]
[MinimumClient(106, 3, ClientLanguage.Invariant)]
public sealed class FeatureListView : IFeatureListView
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private readonly RemotePlayer _player;

    /// <summary>Initializes a new instance of the <see cref="FeatureListView"/> class.</summary>
    /// <param name="player">The receiving player.</param>
    public FeatureListView(RemotePlayer player) => this._player = player;

    /// <inheritdoc />
    public async ValueTask ShowAsync(FeatureListAnswer answer)
    {
        if (this._player.Connection is not { } connection)
        {
            return;
        }

        var data = JsonSerializer.SerializeToUtf8Bytes(answer, SerializerOptions);
        await connection.SendAsync(() =>
        {
            var size = FeaturesResponseExtendedRef.GetRequiredSize(data.Length);
            var packet = new FeaturesResponseExtendedRef(connection.Output.GetSpan(size)[..size]);
            data.CopyTo(packet.Data);
            return size;
        }).ConfigureAwait(false);
    }
}
