// <copyright file="JewelBankView.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView.Inventory;

using System.Runtime.InteropServices;
using System.Text.Json;
using MUnique.OpenMU.GameLogic.JewelBank;
using MUnique.OpenMU.GameLogic.Views.Inventory;
using MUnique.OpenMU.Network;
using MUnique.OpenMU.Network.Packets.ServerToClient;
using MUnique.OpenMU.PlugIns;

/// <summary>Sends confirmed bank snapshots to custom clients which requested them.</summary>
[PlugIn]
[Display(Name = "Jewel Bank View", Description = "Returns jewel balances after bank transactions.")]
[Guid("48b8dda9-4169-4619-9e4d-b41242695f94")]
public sealed class JewelBankView : IJewelBankView
{
    private readonly RemotePlayer _player;

    /// <summary>Initializes a new instance of the <see cref="JewelBankView"/> class.</summary>
    /// <param name="player">The player receiving snapshots.</param>
    public JewelBankView(RemotePlayer player) => this._player = player;

    /// <inheritdoc />
    public async ValueTask ShowAsync(uint request, string status, IReadOnlyList<JewelBankRow> rows)
    {
        if (this._player.Connection is not { } connection)
        {
            return;
        }

        var data = JsonSerializer.SerializeToUtf8Bytes(new { RequestId = request, Status = status, Rows = rows });
        await connection.SendAsync(() =>
        {
            var size = JewelBankResponseExtendedRef.GetRequiredSize(data.Length);
            var response = new JewelBankResponseExtendedRef(connection.Output.GetSpan(size)[..size]);
            data.CopyTo(response.Data);
            return size;
        }).ConfigureAwait(false);
    }
}
