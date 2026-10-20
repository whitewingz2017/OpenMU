// <copyright file="CashShopProductExtensions.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.CashShop;

/// <summary>
/// Extensions for the products of the cash shop.
/// </summary>
public static class CashShopProductExtensions
{
    /// <summary>
    /// Determines whether the product can be delivered into the inventory when a player uses it.
    /// </summary>
    /// <remarks>
    /// A product can't be delivered without an item definition. Timed inventory products retain
    /// their deadline on the delivered item. Application-only products still require a delivery plugin.
    /// On a game server, <see cref="CashShopProductDelivery.CanDeliver"/> decides, because an
    /// <see cref="PlugIns.ICashShopProductDeliveryPlugIn"/> may deliver the product instead. Without
    /// the plugins, e.g. when initializing the catalog, this is the best approximation.
    /// </remarks>
    /// <param name="product">The product.</param>
    /// <returns><c>true</c>, if the product can be delivered; otherwise, <c>false</c>.</returns>
    public static bool CanBeDelivered(this CashShopProduct product)
    {
        return product.ItemDefinition is { } definition
            && product.Duration >= TimeSpan.Zero
            && (product.Duration == TimeSpan.Zero || definition.ItemSlot is not null)
            // Native Season 6 expiration messages use a signed 32-bit Unix timestamp.
            && product.Duration.TotalSeconds <= int.MaxValue - DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }
}
