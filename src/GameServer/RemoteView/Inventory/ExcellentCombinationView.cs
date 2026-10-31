// <copyright file="ExcellentCombinationView.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView.Inventory;
using System.Runtime.InteropServices;
using System.Text.Json;
using MUnique.OpenMU.GameLogic.Views.Inventory;
using MUnique.OpenMU.Network;
using MUnique.OpenMU.Network.Packets.ServerToClient;
using MUnique.OpenMU.PlugIns;
[PlugIn]
[Display(Name = "Excellent Combination View", Description = "Confirms excellent combination quotes and results.")]
[Guid("bde0db9e-a435-498b-9713-467d82e816f3")]
public sealed class ExcellentCombinationView : IExcellentCombinationView
{
    private readonly RemotePlayer _player;
    public ExcellentCombinationView(RemotePlayer player) => this._player = player;
    public async ValueTask ShowAsync(uint request, string status, bool valid, bool success, int cost, MUnique.OpenMU.DataModel.Entities.Item? result)
    {
        if (this._player.Connection is not { } connection) return;
        var data = JsonSerializer.SerializeToUtf8Bytes(new { RequestId = request, Status = status, Valid = valid, Success = success, Cost = cost, ExcellentFlags = result is null ? 0 : ItemSerializerHelper.GetExcellentByte(result) });
        await connection.SendAsync(() => {
            var size = ExcellentCombinationResponseExtendedRef.GetRequiredSize(data.Length);
            var response = new ExcellentCombinationResponseExtendedRef(connection.Output.GetSpan(size)[..size]);
            data.CopyTo(response.Data);
            return size;
        }).ConfigureAwait(false);
    }
}
