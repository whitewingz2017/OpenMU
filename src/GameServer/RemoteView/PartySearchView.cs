// <copyright file="PartySearchView.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView;

using System.Runtime.InteropServices;
using System.Text.Encodings.Web;
using System.Text.Json;
using MUnique.OpenMU.GameLogic.PartySearch;
using MUnique.OpenMU.GameLogic.Views;
using MUnique.OpenMU.Network;
using MUnique.OpenMU.Network.Packets.ServerToClient;
using MUnique.OpenMU.Network.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>Sends the Party Search to the custom client.</summary>
[PlugIn]
[Display(Name = nameof(PartySearchResources.Name), Description = nameof(PartySearchResources.Description), ResourceType = typeof(PartySearchResources))]
[Guid("f25f0ad8-7369-4422-9c68-b09b0eee69c3")]
[MinimumClient(106, 3, ClientLanguage.Invariant)]
public sealed class PartySearchView : IPartySearchView
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private readonly RemotePlayer _player;

    /// <summary>Initializes a new instance of the <see cref="PartySearchView"/> class.</summary>
    /// <param name="player">The receiving player.</param>
    public PartySearchView(RemotePlayer player) => this._player = player;

    /// <inheritdoc />
    public async ValueTask ShowAsync(uint requestId, PartySearchResult result)
    {
        if (this._player.Connection is not { } connection)
        {
            return;
        }

        var data = JsonSerializer.SerializeToUtf8Bytes(this.CreateAnswer(requestId, result), SerializerOptions);
        await connection.SendAsync(() =>
        {
            var size = PartySearchResponseExtendedRef.GetRequiredSize(data.Length);
            var packet = new PartySearchResponseExtendedRef(connection.Output.GetSpan(size)[..size]);
            data.CopyTo(packet.Data);
            return size;
        }).ConfigureAwait(false);
    }

    private object CreateAnswer(uint requestId, PartySearchResult result)
    {
        var own = PartySearchListings.OwnListing(this._player);
        var party = this._player.Party;
        return new
        {
            RequestId = requestId,
            Result = (int)result,
            CanList = party is null || Equals(party.PartyMaster, this._player),
            Settings = own is null
                ? null
                : new
                {
                    own.MinimumLevel,
                    own.GuildOnly,
                    own.OnePerClass,
                    Classes = (int)own.Classes,
                    own.Password,
                },
            Parties = PartySearchListings.Entries(this._player),
        };
    }
}
