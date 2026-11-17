// <copyright file="MarketCategory.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Market;

/// <summary>The item categories of the Marketplace, as the client's category list shows them.</summary>
public enum MarketCategory
{
    /// <summary>Every item; a filter only.</summary>
    All = 0,

    /// <summary>Swords, axes, maces, spears, bows and staffs (groups 0 to 5).</summary>
    Weapons = 1,

    /// <summary>Shields, helms, armors, pants, gloves and boots (groups 6 to 11).</summary>
    Armor = 2,

    /// <summary>Items of the wings slot, including capes.</summary>
    Wings = 3,

    /// <summary>Jewels: the jewel currencies and items named jewel.</summary>
    Jewels = 4,

    /// <summary>Every other item.</summary>
    Materials = 5,
}
