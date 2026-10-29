// <copyright file="StoreMailHandlerPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.MessageHandler.PlayerShop;

using System.Buffers.Binary;
using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.PlayerActions.Messenger;
using MUnique.OpenMU.GameLogic.PlayerActions.PlayerStore;
using MUnique.OpenMU.Network.Packets.ClientToServer;
using MUnique.OpenMU.PlugIns;

/// <summary>Handles bounded combined-price and mail-claim requests.</summary>
[PlugIn]
[Guid("eb166b80-e68a-4240-9256-e585378bf880")]
[Display(Name = "Personal store mail", Description = "Combined prices and claiming purchased items from mail.")]
internal sealed class StoreMailHandlerPlugIn : IPacketHandlerPlugIn
{
    /// <inheritdoc />
    public byte Key => StoreCombinedPriceRequest.Code;

    /// <inheritdoc />
    public bool IsEncryptionExpected => false;

    /// <inheritdoc />
    public async ValueTask HandlePacketAsync(Player player, Memory<byte> packet)
    {
        if (packet.Length < 4 || packet.Span[0] != 0xC1)
        {
            return;
        }

        if (packet.Span[3] == StoreMailBuyRequest.SubCode && packet.Length == StoreMailBuyRequest.Length)
        {
            StoreMailBuyRequest request = packet;
            if (player.CurrentMap?.GetObject(request.PlayerId) is Player seller && seller.Name == request.PlayerName)
            {
                await new BuyRequestAction().BuyItemAsync(player, seller, request.Slot, true).ConfigureAwait(false);
            }
        }
        else if (packet.Span[3] == StoreMailClaimRequest.SubCode && packet.Length == StoreMailClaimRequest.Length)
        {
            StoreMailClaimRequest claim = packet;
            await new ClaimStoreMailAction().ClaimAsync(player, claim.LetterIndex).ConfigureAwait(false);
        }
        else if (packet.Span[3] == StoreCombinedPriceRequest.SubCode && packet.Length == StoreCombinedPriceRequest.Length)
        {
            StoreCombinedPriceRequest request = packet;
            if (request.Count is 0 or > CombinedStorePrice.MaximumParts)
            {
                return;
            }

            var parts = new StorePricePart[request.Count];
            for (var i = 0; i < parts.Length; ++i)
            {
                parts[i] = new StorePricePart(
                    BinaryPrimitives.ReadUInt16LittleEndian(request.Components[(i * 6)..]),
                    BinaryPrimitives.ReadInt32LittleEndian(request.Components[((i * 6) + 2)..]));
            }

            await CombinedStorePrice.SetAsync(player, request.Slot, parts).ConfigureAwait(false);
        }
    }
}
