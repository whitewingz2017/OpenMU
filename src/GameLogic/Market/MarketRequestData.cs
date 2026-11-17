// <copyright file="MarketRequestData.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Market;

/// <summary>
/// The UTF-8 JSON a Marketplace request carries: the filter of the page to answer with, and the
/// listing to create, buy or cancel.
/// </summary>
public sealed class MarketRequestData
{
    /// <summary>Gets or sets the <see cref="MarketCategory"/> of the page.</summary>
    public int Category { get; set; }

    /// <summary>Gets or sets the text the item names of the page contain.</summary>
    public string? Search { get; set; }

    /// <summary>Gets or sets the price type of the page; -1 for every currency.</summary>
    public int Currency { get; set; } = -1;

    /// <summary>Gets or sets the <see cref="MarketSort"/> of the page.</summary>
    public int Sort { get; set; }

    /// <summary>Gets or sets the zero-based page.</summary>
    public int Page { get; set; }

    /// <summary>Gets or sets the inventory slot of the item to list.</summary>
    public int Slot { get; set; }

    /// <summary>Gets or sets the price type of the item to list.</summary>
    public int PriceType { get; set; }

    /// <summary>Gets or sets the price of the item to list.</summary>
    public int Price { get; set; }

    /// <summary>Gets or sets the listing to buy or cancel.</summary>
    public Guid Listing { get; set; }
}
