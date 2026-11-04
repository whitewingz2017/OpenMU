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

/// <summary>Sends combination quotes and committed results to the mobile client.</summary>
[PlugIn]
[Display(Name = "Excellent Combination View", Description = "Confirms excellent combination quotes and results.")]
[Guid("bde0db9e-a435-498b-9713-467d82e816f3")]
public sealed class ExcellentCombinationView : IExcellentCombinationView
{
    private readonly RemotePlayer _player;

    /// <summary>Initializes a new instance of the <see cref="ExcellentCombinationView"/> class.</summary>
    /// <param name="player">The remote player.</param>
    public ExcellentCombinationView(RemotePlayer player) => this._player = player;

    /// <inheritdoc />
    public async ValueTask ShowAsync(uint request, string status, bool valid, bool success, bool completed, GameLogic.PlayerActions.Items.ExcellentCombinationConfiguration settings, int availableCores, int selectedCores, MUnique.OpenMU.DataModel.Entities.Item? result)
    {
        if (this._player.Connection is not { } connection)
        {
            return;
        }

        var data = JsonSerializer.SerializeToUtf8Bytes(new { RequestId = request, Status = status, Valid = valid, Success = success, Completed = completed, Cost = settings.ZenCost, BaseRate = settings.BaseRate, BonusPerCore = settings.BonusPerCore, MaximumRate = settings.MaximumRate, AvailableCores = availableCores, SelectedCores = selectedCores, ExcellentFlags = result is null ? 0 : ItemSerializerHelper.GetExcellentByte(result) });
        await connection.SendAsync(() =>
        {
            var size = ExcellentCombinationResponseExtendedRef.GetRequiredSize(data.Length);
            var response = new ExcellentCombinationResponseExtendedRef(connection.Output.GetSpan(size)[..size]);
            data.CopyTo(response.Data);
            return size;
        }).ConfigureAwait(false);
    }
}
