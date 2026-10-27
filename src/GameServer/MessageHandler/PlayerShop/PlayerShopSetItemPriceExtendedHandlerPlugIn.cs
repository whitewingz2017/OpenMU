// <copyright file="PlayerShopSetItemPriceExtendedHandlerPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.MessageHandler.PlayerShop;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.PlayerActions.PlayerStore;
using MUnique.OpenMU.Network.Packets.ClientToServer;
using MUnique.OpenMU.Network.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>Handles authorized jewel and coin prices.</summary>
[PlugIn]
[Display(Name = "Personal Store Extended Price Handler", Description = "Sets inventory jewel or cash-shop coin prices.")]
[Guid("6a2a56b4-d230-43f4-86ed-94a0d8c49b28")]
[BelongsToGroup(StoreHandlerGroupPlugIn.GroupKey)]
[MinimumClient(106, 3, ClientLanguage.Invariant)]
internal class PlayerShopSetItemPriceExtendedHandlerPlugIn : ISubPacketHandlerPlugIn
{
    private readonly SetItemPriceAction _action = new();
    /// <inheritdoc />
    public byte Key => PlayerShopSetItemPriceExtended.SubCode;
    /// <inheritdoc />
    public bool IsEncryptionExpected => true;
    /// <inheritdoc />
    public async ValueTask HandlePacketAsync(Player player, Memory<byte> packet)
    {
        if (packet.Length != PlayerShopSetItemPriceExtended.Length)
        {
            return;
        }

        PlayerShopSetItemPriceExtended request = packet;
        var itemPayment = request.RequiredItemAmount > 0;
        var type = itemPayment || StoreCurrency.IsCoin(request.PriceItemType) ? request.PriceItemType : 0;
        if ((itemPayment && (type == 0 || request.MoneyPrice != 0 || StoreCurrency.IsCoin(type)))
            || (!itemPayment && request.PriceItemType != 0 && !StoreCurrency.IsCoin(request.PriceItemType))
            || request.MoneyPrice > int.MaxValue)
        {
            return;
        }
        var amount = itemPayment ? request.RequiredItemAmount : (int)request.MoneyPrice;
        await this._action.SetPriceAsync(player, request.ItemSlot, amount, type).ConfigureAwait(false);
    }
}
