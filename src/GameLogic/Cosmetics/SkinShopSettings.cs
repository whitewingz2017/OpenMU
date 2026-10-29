// <copyright file="SkinShopSettings.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Cosmetics;

/// <summary>WCoinC prices per skin category. Zero disables purchases in that category.</summary>
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

    /// <summary>Gets the configured price for a cosmetic definition.</summary>
    /// <param name="definition">The skin definition.</param>
    public int Price(MUnique.OpenMU.DataModel.Configuration.Items.ItemDefinition definition)
        => FenrirMountRules.IsCustom(definition) ? this.MountPrice : definition.Group switch { 8 => this.ArmorPrice, 6 => this.ShieldPrice, 12 => this.WingPrice, 13 => this.PetPrice, _ => this.WeaponPrice };
}
