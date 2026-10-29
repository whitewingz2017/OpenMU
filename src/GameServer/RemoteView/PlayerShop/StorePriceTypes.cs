// <copyright file="StorePriceTypes.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView.PlayerShop;

using MUnique.OpenMU.GameLogic.PlayerActions.PlayerStore;
using MUnique.OpenMU.Network;
using MUnique.OpenMU.Network.Packets.ServerToClient;
using MUnique.OpenMU.Network.PlugIns;

/// <summary>Advertises supported prices to extended clients.</summary>
internal static class StorePriceTypes
{
    /// <summary>Sends allowed currency types.</summary>
    public static async ValueTask SendAsync(RemotePlayer player)
    {
        if (player.ClientVersion < new ClientVersion(106, 3, ClientLanguage.Invariant) || player.Connection is not { } connection)
        {
            return;
        }

        var settings = StoreCurrencyFeaturePlugIn.GetSettings(player.GameContext);
        var types = StoreCurrency.AllowedTypes(settings).Concat(settings is null ? Array.Empty<ushort>() : new ushort[] { CombinedStorePrice.Marker }).ToArray();
        await connection.SendAsync(() =>
        {
            var size = PlayerShopPriceTypesExtendedRef.GetRequiredSize(types.Length);
            var packet = new PlayerShopPriceTypesExtendedRef(connection.Output.GetSpan(size)[..size]) { Count = (byte)types.Length };
            for (var i = 0; i < types.Length; i++)
            {
                var entry = packet[i];
                entry.PriceItemType = types[i];
            }
            return size;
        }).ConfigureAwait(false);
    }
}
