// <copyright file="StoreCurrency.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions.PlayerStore;

using MUnique.OpenMU.DataModel;

/// <summary>Shared extended store price encoding and validation.</summary>
public static class StoreCurrency
{
    /// <summary>Wire codes reserved by the custom protocol for WCoin C, WCoin P and Goblin Points.</summary>
    public const int FirstCoin = 0xFFF0;

    /// <summary>Gets advertised wire types from operator settings.</summary>
    public static ushort[] AllowedTypes(StoreCurrencySettings? settings)
        => settings is null ? [] : settings.Jewels.Where(id => id >= 0 && id < 8192)
            .Select(id => (ushort)(((id / 512) << 12) | (id % 512)))
            .Concat(settings.AllowCashShopCoins ? new ushort[] { FirstCoin, FirstCoin + 1, FirstCoin + 2 } : [])
            .Distinct().Take(125).ToArray();

    /// <summary>Checks currencies and positive amounts against protocol limits.</summary>
    public static bool IsValid(int type, int amount, StoreCurrencySettings? settings)
        => amount > 0 && (type == 0 || (type >= 0 && type <= ushort.MaxValue && AllowedTypes(settings).Contains((ushort)type)
            && (IsCoin(type) || amount <= ushort.MaxValue)));

    /// <summary>Checks whether the type denotes an account coin.</summary>
    public static bool IsCoin(int type) => type >= FirstCoin && type <= FirstCoin + 2;

    /// <summary>Gets the account coin matching a wire type.</summary>
    public static CashShopCoinType CoinType(int type) => type switch
    {
        FirstCoin => CashShopCoinType.WCoinC,
        FirstCoin + 1 => CashShopCoinType.WCoinP,
        FirstCoin + 2 => CashShopCoinType.GoblinPoints,
        _ => throw new ArgumentOutOfRangeException(nameof(type)),
    };

    /// <summary>Checks that an inventory payment jewel is a plain level-zero single.</summary>
    public static bool IsPaymentJewel(Item item, int type)
        => !IsCoin(type) && item.ItemSlot > InventoryConstants.LastEquippableItemSlotIndex
            && item.Definition is { } definition && ((definition.Group << 12) | definition.Number) == type
            && (!item.IsStackable() || item.Durability == 1) && definition.IsTradable
            && item.Level == 0 && item.ItemOptions.Count == 0 && item.ItemSetGroups.Count == 0
            && !item.HasSkill && item.SocketCount == 0 && !item.StorePrice.HasValue && item.ExpiresAtUtc is null;
}
