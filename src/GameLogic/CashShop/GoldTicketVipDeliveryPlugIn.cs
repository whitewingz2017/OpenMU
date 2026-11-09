// <copyright file="GoldTicketVipDeliveryPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.CashShop;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.GameLogic.Views.CashShop;
using MUnique.OpenMU.GameLogic.Views.Inventory;
using MUnique.OpenMU.PlugIns;

/// <summary>Activates account VIP for the duration of a purchased Gold Channel Ticket.</summary>
[PlugIn]
[Display(Name = nameof(PlugInResources.GoldTicketVipDeliveryPlugIn_Name), Description = nameof(PlugInResources.GoldTicketVipDeliveryPlugIn_Description), ResourceType = typeof(PlugInResources))]
[Guid("a7317d47-c1a0-4422-a623-c4ce10a7b722")]
public sealed class GoldTicketVipDeliveryPlugIn : ICashShopProductDeliveryPlugIn
{
    /// <summary>The Gold Channel Ticket identifier in both Season 6 client catalogs.</summary>
    public static readonly ItemIdentifier GoldTicket = new(124, 13);

    /// <inheritdoc />
    public ItemIdentifier Key => GoldTicket;

    /// <inheritdoc />
    public bool CanDeliver(CashShopProduct product) => product.ItemDefinition is { Group: 13, Number: 124 }
        && product.Duration > TimeSpan.Zero && product.Duration < DateTime.MaxValue - DateTime.UtcNow;

    /// <inheritdoc />
    public ValueTask<CashShopUseResult> DeliverAsync(Player player, CashShopProduct product)
    {
        if (!this.CanDeliver(product) || player.Account is not { } account)
        {
            return ValueTask.FromResult(CashShopUseResult.CannotUse);
        }

        var start = account.VipExpiresAtUtc is { } expires && expires > DateTime.UtcNow ? expires : DateTime.UtcNow;
        if (product.Duration > DateTime.MaxValue - start)
        {
            return ValueTask.FromResult(CashShopUseResult.CannotUse);
        }

        account.VipExpiresAtUtc = start.Add(product.Duration);
        return ValueTask.FromResult(CashShopUseResult.Success);
    }

    /// <inheritdoc />
    public ValueTask DeliveredAsync(Player player, CashShopProduct product)
        => player.ForEachWorldObserverAsync<IRankTitleView>(view => view.AppearanceAsync(player), true);
}
