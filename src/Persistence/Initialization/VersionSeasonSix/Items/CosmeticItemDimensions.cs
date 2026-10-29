// <copyright file="CosmeticItemDimensions.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Items;

/// <summary>Normal inventory footprints for cosmetic item categories.</summary>
public static class CosmeticItemDimensions
{
    /// <summary>Gets the normal footprint, preserving cape and pet conventions.</summary>
    /// <param name="group">The item group.</param>
    /// <param name="name">The skin's name.</param>
    /// <returns>The width and height in inventory cells.</returns>
    public static (byte Width, byte Height) Get(byte group, string name)
        => group switch
        {
            0 when name == "Sacred Glove Skin" => (1, 2),
            2 when name == "Battle Scepter Skin" => (1, 3),
            5 when name == "Summoner Book Skin" => (1, 2),
            0 or 3 or 4 => (2, 4),
            1 or 6 or 8 => (2, 3),
            2 or 5 => (1, 4),
            >= 7 and <= 11 => (2, 2),
            12 when name.Contains("cape", StringComparison.OrdinalIgnoreCase) || name.Contains("cloak", StringComparison.OrdinalIgnoreCase) => (2, 3),
            12 => (5, 3),
            _ => (1, 1),
        };
}
