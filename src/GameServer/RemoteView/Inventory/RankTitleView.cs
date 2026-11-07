// <copyright file="RankTitleView.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView.Inventory;

using System.Runtime.InteropServices;
using System.Text.Json;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.RankTitles;
using MUnique.OpenMU.GameLogic.Views;
using MUnique.OpenMU.GameLogic.Views.Inventory;
using MUnique.OpenMU.Network;
using MUnique.OpenMU.Network.Packets.ServerToClient;
using MUnique.OpenMU.Network.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>Sends account WCoinC, rank title ownership and visible equipped titles.</summary>
[PlugIn]
[Display(Name = nameof(RankTitleResources.Name), Description = nameof(RankTitleResources.Description), ResourceType = typeof(RankTitleResources))]
[Guid("1e4fce46-142d-445e-a54c-263cb9eaa7eb")]
[MinimumClient(106, 3, ClientLanguage.Invariant)]
public sealed class RankTitleView : IRankTitleView
{
    private readonly RemotePlayer _player;

    /// <summary>Initializes a new instance of the <see cref="RankTitleView"/> class.</summary>
    /// <param name="player">The receiving player.</param>
    public RankTitleView(RemotePlayer player) => this._player = player;

    /// <inheritdoc />
    public async ValueTask ShowAsync(uint requestId, string? messageKey)
    {
        if (this._player.Connection is not { } connection)
        {
            return;
        }

        var owned = new HashSet<int>();
        if (this._player.SelectedCharacter is { } character)
        {
            RankTitleRules.TryReadOwned(character, out owned);
        }

        var settings = RankTitleFeaturePlugIn.Settings(this._player.GameContext);
        var data = JsonSerializer.SerializeToUtf8Bytes(new
        {
            RequestId = requestId,
            Status = messageKey is null ? "Ready" : this._player.GetLocalizedMessage(messageKey),
            Balance = this._player.Account?.WCoinC ?? 0,
            Active = RankTitleRules.Active(this._player),
            Ranks = (settings?.Titles ?? []).Select(title => new { title.Rank, title.Price, Owned = owned.Contains(title.Rank), title.Name }),
        });
        await connection.SendAsync(() =>
        {
            var size = RankTitleResponseExtendedRef.GetRequiredSize(data.Length);
            var packet = new RankTitleResponseExtendedRef(connection.Output.GetSpan(size)[..size]);
            data.CopyTo(packet.Data);
            return size;
        }).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask AppearanceAsync(Player owner)
    {
        if (this._player.Connection is not { } connection || owner.SelectedCharacter is null)
        {
            return;
        }

        var data = JsonSerializer.SerializeToUtf8Bytes(new
        {
            Ranks = new[] { new { Id = owner.GetId(this._player), Name = owner.Name, Rank = RankTitleRules.Active(owner) } },
        });
        await connection.SendAsync(() =>
        {
            var size = RankTitleAppearanceExtendedRef.GetRequiredSize(data.Length);
            var packet = new RankTitleAppearanceExtendedRef(connection.Output.GetSpan(size)[..size]);
            data.CopyTo(packet.Data);
            return size;
        }).ConfigureAwait(false);
    }
}
