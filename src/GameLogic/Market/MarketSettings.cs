// <copyright file="MarketSettings.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Market;

/// <summary>The settings of the Marketplace, edited on the plugin's Configure page.</summary>
public class MarketSettings
{
    /// <summary>Gets or sets a value indicating whether items can be sold for zen.</summary>
    [Display(Name = "Allow Zen prices")]
    public bool AllowZen { get; set; } = true;

    /// <summary>Gets or sets the tax of zen sales in percent.</summary>
    [Display(Name = "Zen tax (%)", Description = "Taken from the seller's proceeds.")]
    [Range(0, 100)]
    public int ZenTaxPercent { get; set; } = 10;

    /// <summary>Gets or sets a value indicating whether items can be sold for WCoinC.</summary>
    [Display(Name = "Allow WCoinC prices")]
    public bool AllowWCoinC { get; set; } = true;

    /// <summary>Gets or sets the tax of WCoinC sales in percent.</summary>
    [Display(Name = "WCoinC tax (%)", Description = "Taken from the seller's proceeds.")]
    [Range(0, 100)]
    public int WCoinCTaxPercent { get; set; } = 10;

    /// <summary>Gets or sets the jewels which are currencies (group * 512 + number).</summary>
    [Display(Name = "Jewel currencies", Description = "Item numbers as group * 512 + number. Buyers pay with plain single jewels of the inventory.")]
    public int[] Jewels { get; set; } = [7181, 7182, 6159, 7184, 7190];

    /// <summary>Gets or sets the tax of jewel sales in percent.</summary>
    [Display(Name = "Jewel tax (%)", Description = "Taken in jewels from the seller's proceeds, e.g. 10% of 10 Jewels of Bless is 1.")]
    [Range(0, 100)]
    public int JewelTaxPercent { get; set; } = 10;

    /// <summary>Gets or sets how a tax which isn't a whole amount is rounded.</summary>
    [Display(Name = "Tax rounding", Description = "E.g. 10% of 5 jewels is 0.5: Nearest and Up take 1, Down takes 0.")]
    public MarketTaxRounding TaxRounding { get; set; } = MarketTaxRounding.Nearest;

    /// <summary>Gets or sets how long a listing stays on the market.</summary>
    [Display(Name = "Listing duration (hours)", Description = "Unsold items go back to the seller by mail.")]
    [Range(1, 720)]
    public int ListingHours { get; set; } = 48;

    /// <summary>Gets or sets the most active listings of a character.</summary>
    [Display(Name = "Listings per character")]
    [Range(1, 100)]
    public int MaximumListings { get; set; } = 10;

    /// <summary>Gets or sets the lowest character level which may sell and buy.</summary>
    [Display(Name = "Minimum character level")]
    [Range(1, 1000)]
    public int MinimumLevel { get; set; } = 1;

    /// <summary>Gets or sets how long ended listings stay in the history.</summary>
    [Display(Name = "History (days)")]
    [Range(1, 365)]
    public int HistoryDays { get; set; } = 14;
}
