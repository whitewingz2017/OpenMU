// <copyright file="RedeemCodeHandlerPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.MessageHandler.RedeemCodes;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.RedeemCodes;
using MUnique.OpenMU.GameLogic.Views;
using MUnique.OpenMU.Network.Packets.ClientToServer;
using MUnique.OpenMU.PlugIns;

/// <summary>Accepts redeem requests only from an authenticated in-game player.</summary>
[PlugIn]
[Display(Name = "Redeem Codes", Description = "Redeems gift codes configured under Admin Panel / Redeem Codes.")]
[Guid("e419f3d8-3349-42ab-aef8-f21bfe4d8283")]
internal sealed class RedeemCodeHandlerPlugIn : IPacketHandlerPlugIn
{
    private readonly RedeemCodeActions _actions = new();
    /// <inheritdoc />
    public byte Key => RedeemCodeRequestExtended.Code;
    /// <inheritdoc />
    public bool IsEncryptionExpected => false;
    /// <inheritdoc />
    public async ValueTask HandlePacketAsync(Player player, Memory<byte> packet)
    {
        if (packet.Length != RedeemCodeRequestExtended.Length || packet.Span[0] != 0xC1 || packet.Span[3] != RedeemCodeRequestExtended.SubCode) return;
        RedeemCodeRequestExtended request = packet;
        var result = await this._actions.RedeemAsync(player, request.CodeText).ConfigureAwait(false);
        await player.InvokeViewPlugInAsync<IShowMessagePlugIn>(v => v.ShowMessageAsync(result, MUnique.OpenMU.Interfaces.MessageType.BlueNormal)).ConfigureAwait(false);
    }
}
