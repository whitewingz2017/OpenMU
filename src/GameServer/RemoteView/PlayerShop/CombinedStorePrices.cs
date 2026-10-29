// <copyright file="CombinedStorePrices.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView.PlayerShop;

using System.Buffers.Binary;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.PlayerActions.PlayerStore;
using MUnique.OpenMU.Network;
using MUnique.OpenMU.Network.Packets.ServerToClient;
using MUnique.OpenMU.Network.PlugIns;

/// <summary>Sends the full price after the legacy inventory or shop snapshot.</summary>
internal static class CombinedStorePrices
{
    public static async ValueTask SendAsync(RemotePlayer viewer, Player owner)
    {
        if (viewer.ClientVersion < new ClientVersion(106, 3, ClientLanguage.Invariant) || viewer.Connection is not { } connection || owner.ShopStorage is null)
        {
            return;
        }

        foreach (var item in owner.ShopStorage.Items.Where(i => !string.IsNullOrEmpty(i.StorePricesJson)))
        {
            var parts = CombinedStorePrice.Read(item);
            int Write()
            {
                var memory = connection.Output.GetMemory(StoreCombinedPriceResponse.Length)[..StoreCombinedPriceResponse.Length];
                var span = memory.Span;
                span.Clear();
                var packet = new StoreCombinedPriceResponse(memory) { OwnStore = ReferenceEquals(viewer, owner), Slot = item.ItemSlot, Count = (byte)parts.Length };
                for (var i = 0; i < parts.Length; ++i)
                {
                    BinaryPrimitives.WriteUInt16LittleEndian(packet.Components[(i * 6)..], (ushort)parts[i].Type);
                    BinaryPrimitives.WriteInt32LittleEndian(packet.Components[((i * 6) + 2)..], parts[i].Amount);
                }

                return span.Length;
            }

            await connection.SendAsync(Write).ConfigureAwait(false);
        }
    }
}
