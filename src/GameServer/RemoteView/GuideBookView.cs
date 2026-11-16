// <copyright file="GuideBookView.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView;

using System.Runtime.InteropServices;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.GuideBook;
using MUnique.OpenMU.GameLogic.Views;
using MUnique.OpenMU.Network;
using MUnique.OpenMU.Network.Packets.ServerToClient;
using MUnique.OpenMU.Network.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>Sends the Guide Book to the custom client.</summary>
[PlugIn]
[Display(Name = nameof(GuideBookResources.Name), Description = nameof(GuideBookResources.Description), ResourceType = typeof(GuideBookResources))]
[Guid("f6f6b0d0-1e3e-4f2f-9915-3ae7c2dea605")]
[MinimumClient(106, 3, ClientLanguage.Invariant)]
public sealed class GuideBookView : IGuideBookView
{
    /// <summary>
    /// A refused request has no guide; its absent fields tell the client to keep the guide it has. The text stays
    /// unescaped UTF-8, as <see cref="GuideBookContent"/> measures it against the packet's size.
    /// </summary>
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private readonly RemotePlayer _player;

    /// <summary>Initializes a new instance of the <see cref="GuideBookView"/> class.</summary>
    /// <param name="player">The receiving player.</param>
    public GuideBookView(RemotePlayer player) => this._player = player;

    /// <inheritdoc />
    public async ValueTask ShowAsync(uint requestId, string? messageKey, GuideBookShown shown)
    {
        if (this._player.Connection is not { } connection)
        {
            return;
        }

        var data = JsonSerializer.SerializeToUtf8Bytes(this.CreateAnswer(requestId, messageKey, shown), SerializerOptions);
        await connection.SendAsync(() =>
        {
            var size = GuideBookResponseExtendedRef.GetRequiredSize(data.Length);
            var packet = new GuideBookResponseExtendedRef(connection.Output.GetSpan(size)[..size]);
            data.CopyTo(packet.Data);
            return size;
        }).ConfigureAwait(false);
    }

    private object CreateAnswer(uint requestId, string? messageKey, GuideBookShown shown)
    {
        var context = this._player.GameContext;
        var settings = GuideBookFeaturePlugIn.Settings(context);
        var canEdit = GuideBookActions.IsGameMaster(this._player);
        var content = shown switch
        {
            GuideBookShown.Published => settings?.Published,
            GuideBookShown.Draft => settings?.Draft,
            _ => null,
        };
        return new
        {
            RequestId = requestId,
            Status = messageKey is null ? string.Empty : this._player.GetLocalizedMessage(messageKey),
            CanEdit = canEdit,
            Draft = shown == GuideBookShown.Draft,
            Unpublished = canEdit && settings?.HasUnpublishedChanges() == true,
            Revision = content?.Revision,
            Categories = content?.Categories,
            Pages = content?.Pages,
            Values = content is null ? null : GuideBookValues.Collect(context),
            BossRewards = content is null ? null : GuideBookValues.BossRewards(context),
        };
    }
}
