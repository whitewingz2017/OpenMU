// <copyright file="SkinShopCategory.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Cosmetics;

/// <summary>The persisted skin shop categories, matching the client tabs.</summary>
public enum SkinShopCategory
{
    /// <summary>Full armor sets.</summary>
    Sets = 0,

    /// <summary>Wings and capes.</summary>
    Wings = 1,

    /// <summary>Weapon skins.</summary>
    Weapons = 2,

    /// <summary>Shield skins.</summary>
    Shields = 3,

    /// <summary>Cosmetic pets.</summary>
    Pets = 4,

    /// <summary>Rideable mounts.</summary>
    Mounts = 5,
}
