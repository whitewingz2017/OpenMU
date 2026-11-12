// <copyright file="SkinShopSettings.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Cosmetics;

using MUnique.OpenMU.DataModel.Configuration.Items;

/// <summary>Category defaults and individual skin availability and WCoinC prices.</summary>
public sealed class SkinShopSettings
{
    /// <summary>Gets or sets the price of a full armor set skin.</summary>
    [Range(0, int.MaxValue)]
    public int ArmorPrice { get; set; } = 100;

    /// <summary>Gets or sets the price of a weapon skin.</summary>
    [Range(0, int.MaxValue)]
    public int WeaponPrice { get; set; } = 100;

    /// <summary>Gets or sets the price of a shield skin.</summary>
    [Range(0, int.MaxValue)]
    public int ShieldPrice { get; set; } = 100;

    /// <summary>Gets or sets the price of a wing or cape skin.</summary>
    [Range(0, int.MaxValue)]
    public int WingPrice { get; set; } = 100;

    /// <summary>Gets or sets the price of a pet skin.</summary>
    [Range(0, int.MaxValue)]
    public int PetPrice { get; set; } = 100;

    /// <summary>Gets or sets the price of a standalone Fenrir mount.</summary>
    [Range(0, int.MaxValue)]
    public int MountPrice { get; set; } = 100;

    /// <summary>Gets or sets categories which are preview only.</summary>
    public HashSet<SkinShopCategory> DisabledCategories { get; set; } = [];

    /// <summary>Gets or sets individual skin prices, keyed by group times 512 plus item number.</summary>
    public Dictionary<int, SkinShopItemSettings> Items { get; set; } = [];

    /// <summary>Gets the category of a cosmetic definition.</summary>
    /// <param name="definition">The skin definition.</param>
    /// <returns>The category shown in the client.</returns>
    public static SkinShopCategory Category(ItemDefinition definition)
        => FenrirMountRules.IsCustom(definition) ? SkinShopCategory.Mounts : definition.Group switch
        {
            8 => SkinShopCategory.Sets,
            6 => SkinShopCategory.Shields,
            12 => SkinShopCategory.Wings,
            13 => SkinShopCategory.Pets,
            _ => SkinShopCategory.Weapons,
        };

    /// <summary>Gets the configured default price for a category.</summary>
    /// <param name="category">The skin category.</param>
    /// <returns>The category price, or zero if unknown.</returns>
    public int DefaultPrice(SkinShopCategory category) => category switch
    {
        SkinShopCategory.Sets => this.ArmorPrice,
        SkinShopCategory.Shields => this.ShieldPrice,
        SkinShopCategory.Wings => this.WingPrice,
        SkinShopCategory.Pets => this.PetPrice,
        SkinShopCategory.Mounts => this.MountPrice,
        SkinShopCategory.Weapons => this.WeaponPrice,
        _ => 0,
    };

    /// <summary>Gets the sale price; disabled skins and categories return zero.</summary>
    /// <param name="definition">The skin definition.</param>
    /// <returns>The authoritative WCoinC price.</returns>
    public int Price(ItemDefinition definition)
    {
        var category = Category(definition);
        var defaultPrice = this.DefaultPrice(category);
        if (defaultPrice <= 0 || this.DisabledCategories?.Contains(category) == true)
        {
            return 0;
        }

        if (this.Items?.TryGetValue((definition.Group * 512) + definition.Number, out var item) == true)
        {
            return item is { Enabled: true, Price: > 0 } ? item.Price : 0;
        }

        return defaultPrice;
    }
}
