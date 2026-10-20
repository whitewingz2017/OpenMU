// <copyright file="ItemExpirationViewPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView.Inventory;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.Views.Inventory;
using MUnique.OpenMU.Network.Packets.ServerToClient;
using MUnique.OpenMU.Network.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Implements Season 6 period-item count and absolute Unix expiration packets.
/// </summary>
[PlugIn]
[Display(Name = nameof(PlugInResources.ItemExpirationViewPlugIn_Name), Description = nameof(PlugInResources.ItemExpirationViewPlugIn_Description), ResourceType = typeof(PlugInResources))]
[Guid("a0486f1c-f45a-44fc-967f-a74aba04a487")]
[MinimumClient(6, 0, ClientLanguage.Invariant)]
public class ItemExpirationViewPlugIn : IItemExpirationViewPlugIn
{
    private readonly RemotePlayer _player;

    /// <summary>
    /// Initializes a new instance of the <see cref="ItemExpirationViewPlugIn"/> class.
    /// </summary>
    /// <param name="player">The player.</param>
    public ItemExpirationViewPlugIn(RemotePlayer player) => this._player = player;

    /// <inheritdoc />
    public async ValueTask ShowItemExpirationsAsync()
    {
        var items = this._player.Inventory?.Items.Where(i => i.ExpiresAtUtc is not null).ToList() ?? [];
        await this._player.Connection.SendCashShopPeriodItemCountAsync((byte)items.Count).ConfigureAwait(false);
        foreach (var item in items)
        {
            await this.ShowItemExpirationAsync(item).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async ValueTask ShowItemExpirationAsync(Item item)
    {
        if (item.ExpiresAtUtc is not { } expiration || item.Definition is not { } definition)
        {
            return;
        }

        await this._player.Connection.SendCashShopPeriodItemAsync(
            (ushort)((definition.Group * 512) + definition.Number),
            item.ItemSlot,
            (uint)new DateTimeOffset(DateTime.SpecifyKind(expiration, DateTimeKind.Utc)).ToUnixTimeSeconds()).ConfigureAwait(false);
    }
}
