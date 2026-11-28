// <copyright file="DuelBettingView.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView.Duel;

using System.Runtime.InteropServices;
using System.Text.Json;
using MUnique.OpenMU.GameLogic.DuelBetting;
using MUnique.OpenMU.GameLogic.Views.Duel;
using MUnique.OpenMU.Network;
using MUnique.OpenMU.Network.Packets.ServerToClient;
using MUnique.OpenMU.Network.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>Sends a public duel betting snapshot to the custom client.</summary>
[PlugIn]
[Display(Name = nameof(DuelBettingResources.ViewName), Description = nameof(DuelBettingResources.ViewDescription), ResourceType = typeof(DuelBettingResources))]
[Guid("d5015c14-eabf-46f6-8388-88ac6be02a71")]
[MinimumClient(106, 3, ClientLanguage.Invariant)]
public sealed class DuelBettingView(RemotePlayer player) : IDuelBettingView
{
    /// <inheritdoc />
    public ValueTask ShowAsync(uint requestId, bool enabled, string status, IReadOnlyList<DuelBetBoardEntry> duels, int publicSeconds = 0, int privateSeconds = 0, int firstTo = 0, int wagerMinimum = 0, int wagerMaximum = 0)
        => this.SendAsync(new { RequestId = requestId, Enabled = enabled, Status = status, Duels = duels, PublicSeconds = publicSeconds, PrivateSeconds = privateSeconds, FirstTo = firstTo, WagerMinimum = wagerMinimum, WagerMaximum = wagerMaximum });

    /// <inheritdoc />
    public ValueTask ShowInvitationAsync(DuelBetInvitation invitation) => this.SendAsync(new { Invitation = invitation });

    private async ValueTask SendAsync<T>(T value)
    {
        if (player.Connection is not { } connection)
        {
            return;
        }

        var data = JsonSerializer.SerializeToUtf8Bytes(value);
        await connection.SendAsync(() =>
        {
            var size = DuelBettingResponseExtendedRef.GetRequiredSize(data.Length);
            var packet = new DuelBettingResponseExtendedRef(connection.Output.GetSpan(size)[..size]);
            data.CopyTo(packet.Data);
            return size;
        }).ConfigureAwait(false);
    }
}
