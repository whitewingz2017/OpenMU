// <copyright file="MarketCurrency.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Market;

using System.Globalization;
using MUnique.OpenMU.GameLogic.PlayerActions.PlayerStore;

/// <summary>
/// The currencies of the Marketplace: zen, WCoinC and the jewels of the <see cref="MarketSettings"/>.
/// A currency is a price type: 0 is zen, <see cref="WCoinC"/> the coin, and a jewel is (group &lt;&lt; 12) | number.
/// </summary>
public static class MarketCurrency
{
    /// <summary>The price type of zen.</summary>
    public const int Zen = 0;

    /// <summary>The price type of WCoinC, the same as in the personal store.</summary>
    public const int WCoinC = StoreCurrency.FirstCoin;

    /// <summary>The highest jewel price: the buyer pays with single jewels of the inventory.</summary>
    public const int MaximumJewelPrice = 250;

    /// <summary>The item numbers of a group in the settings' item ids, group * 512 + number.</summary>
    private const int ItemsPerGroup = 512;

    /// <summary>The bits of the number in a jewel price type.</summary>
    private const int GroupShift = 12;

    /// <summary>The highest item id of the settings.</summary>
    private const int MaximumItemId = 16 * ItemsPerGroup;

    private const int Percent = 100;

    /// <summary>Gets the allowed price types, in the order of the settings.</summary>
    /// <param name="settings">The settings.</param>
    /// <returns>The price types.</returns>
    public static IEnumerable<int> Allowed(MarketSettings settings)
    {
        if (settings.AllowZen)
        {
            yield return Zen;
        }

        if (settings.AllowWCoinC)
        {
            yield return WCoinC;
        }

        foreach (var id in settings.Jewels.Where(id => id is >= 0 and < MaximumItemId).Distinct())
        {
            yield return ((id / ItemsPerGroup) << GroupShift) | (id % ItemsPerGroup);
        }
    }

    /// <summary>Determines whether the price type is a jewel.</summary>
    /// <param name="type">The price type.</param>
    /// <returns><c>true</c> for a jewel.</returns>
    public static bool IsJewel(int type) => type is not Zen and not WCoinC;

    /// <summary>Determines whether a seller may ask <paramref name="amount"/> of <paramref name="type"/>.</summary>
    /// <param name="settings">The settings.</param>
    /// <param name="configuration">The game configuration, for the zen limit.</param>
    /// <param name="type">The price type.</param>
    /// <param name="amount">The price.</param>
    /// <returns><c>true</c> if the price is allowed.</returns>
    public static bool IsValidPrice(MarketSettings settings, GameConfiguration configuration, int type, int amount)
    {
        if (amount <= 0 || !Allowed(settings).Contains(type))
        {
            return false;
        }

        return type switch
        {
            Zen => configuration.MaximumInventoryMoney <= 0 || amount <= configuration.MaximumInventoryMoney,
            WCoinC => true,
            _ => amount <= MaximumJewelPrice,
        };
    }

    /// <summary>Gets the tax of the currency in percent.</summary>
    /// <param name="settings">The settings.</param>
    /// <param name="type">The price type.</param>
    /// <returns>The tax in percent.</returns>
    public static int TaxPercent(MarketSettings settings, int type) => type switch
    {
        Zen => settings.ZenTaxPercent,
        WCoinC => settings.WCoinCTaxPercent,
        _ => settings.JewelTaxPercent,
    };

    /// <summary>Gets the tax of a sale, in the currency of the price, rounded as set.</summary>
    /// <param name="settings">The settings.</param>
    /// <param name="type">The price type.</param>
    /// <param name="price">The price.</param>
    /// <returns>The tax, between 0 and <paramref name="price"/>.</returns>
    public static int Tax(MarketSettings settings, int type, int price)
    {
        var percent = Math.Clamp(TaxPercent(settings, type), 0, Percent);
        var share = (long)price * percent;
        var tax = settings.TaxRounding switch
        {
            MarketTaxRounding.Up => (share + Percent - 1) / Percent,
            MarketTaxRounding.Down => share / Percent,
            _ => (share + (Percent / 2)) / Percent,
        };

        return (int)Math.Clamp(tax, 0, price);
    }

    /// <summary>Gets the item definition of a jewel price type.</summary>
    /// <param name="configuration">The game configuration.</param>
    /// <param name="type">The price type.</param>
    /// <returns>The definition, or <c>null</c>.</returns>
    public static ItemDefinition? JewelDefinition(GameConfiguration configuration, int type)
        => IsJewel(type) ? configuration.Items.FirstOrDefault(d => ((d.Group << GroupShift) | d.Number) == type) : null;

    /// <summary>Gets the display name of a currency.</summary>
    /// <param name="configuration">The game configuration.</param>
    /// <param name="type">The price type.</param>
    /// <returns>The name.</returns>
    public static string Name(GameConfiguration configuration, int type) => type switch
    {
        Zen => "Zen",
        WCoinC => "WCoinC",
        _ => JewelDefinition(configuration, type)?.Name.ToString() ?? type.ToString(CultureInfo.InvariantCulture),
    };

    /// <summary>Gets the plain single jewels of <paramref name="type"/> in the player's inventory.</summary>
    /// <param name="player">The player.</param>
    /// <param name="type">The jewel price type.</param>
    /// <returns>The jewels which can pay.</returns>
    public static IEnumerable<Item> PaymentJewels(Player player, int type)
        => player.Inventory?.Items.Where(i => StoreCurrency.IsPaymentJewel(i, type)) ?? [];

    /// <summary>Gets the player's balance of the currency.</summary>
    /// <param name="player">The player.</param>
    /// <param name="type">The price type.</param>
    /// <returns>The balance.</returns>
    public static long Balance(Player player, int type) => type switch
    {
        Zen => player.Money,
        WCoinC => player.Account?.WCoinC ?? 0,
        _ => PaymentJewels(player, type).Count(),
    };
}
