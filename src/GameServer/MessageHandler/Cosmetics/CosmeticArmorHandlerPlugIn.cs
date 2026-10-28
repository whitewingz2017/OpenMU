// <copyright file="CosmeticArmorHandlerPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.MessageHandler.Cosmetics;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.Cosmetics;
using MUnique.OpenMU.Network.Packets.ClientToServer;
using MUnique.OpenMU.PlugIns;

/// <summary>Handles opt-in cosmetic armor requests from custom clients.</summary>
[PlugIn]
[Display(Name = "Cosmetic Armor", Description = "Equips tradable cosmetic armor without combat bonuses.")]
[Guid("ad3f89d8-120a-4142-ac35-e6a833d04377")]
internal sealed class CosmeticArmorHandlerPlugIn : IPacketHandlerPlugIn
{
    private readonly CosmeticArmorActions _actions = new();
    public byte Key => CosmeticArmorRequestExtended.Code;
    public bool IsEncryptionExpected => false;

    public async ValueTask HandlePacketAsync(Player player, Memory<byte> packet)
    {
        if (packet.Length != CosmeticArmorRequestExtended.Length || packet.Span[0] != 0xC1
            || packet.Span[3] != CosmeticArmorRequestExtended.SubCode)
        {
            return;
        }

        CosmeticArmorRequestExtended request = packet;
        await this._actions.ExecuteAsync(player, request.RequestId, request.Operation, request.InventorySlot, request.Part).ConfigureAwait(false);
    }
}
