// <copyright file="CosmeticArmorView.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView.Inventory;

using System.Runtime.InteropServices;
using System.Text.Json;
using MUnique.OpenMU.GameLogic.Cosmetics;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.Views.Inventory;
using MUnique.OpenMU.Network;
using MUnique.OpenMU.Network.Packets.ServerToClient;
using MUnique.OpenMU.PlugIns;

/// <summary>Sends the authoritative five skin slots to their owner.</summary>
[PlugIn]
[Display(Name = "Cosmetic Armor View", Description = "Confirms cosmetic armor item slots to custom clients.")]
[Guid("2ad475e5-281b-4d18-923c-87ea689e43a9")]
public sealed class CosmeticArmorView : ICosmeticArmorView
{
    private readonly RemotePlayer _player;
    public CosmeticArmorView(RemotePlayer player) => this._player = player;

    public async ValueTask ShowAsync(uint requestId, string status)
    {
        if (this._player.Connection is not { } connection)
        {
            return;
        }

        var data = JsonSerializer.SerializeToUtf8Bytes(new
        {
            RequestId = requestId,
            Status = status,
            Statistics = CharacterCombatStatistics.Create(this._player),
            StoredSkins = this._player.SelectedCharacter?.SkinInventory?.Items.OrderBy(i => i.ItemSlot)
                .Select(i => new { Slot = i.ItemSlot, ItemType = (i.Definition!.Group * 512) + i.Definition.Number,
                    Level = i.Level, BonusFlags = ItemSerializerHelper.GetExcellentByte(i) }),
            Skins = CosmeticArmorRules.Equipped(this._player).Select(i => new { Part = i.ItemSlot - CosmeticArmorRules.FirstSlot, ItemType = (i.Definition!.Group * 512) + i.Definition.Number, BonusFlags = ItemSerializerHelper.GetExcellentByte(i) }),
        });
        await connection.SendAsync(() =>
        {
            var size = CosmeticArmorResponseExtendedRef.GetRequiredSize(data.Length);
            var response = new CosmeticArmorResponseExtendedRef(connection.Output.GetSpan(size)[..size]);
            data.CopyTo(response.Data);
            return size;
        }).ConfigureAwait(false);
    }
}
