// <copyright file="CosmeticItemDimensions.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Items;

/// <summary>Normal inventory footprints for cosmetic item categories.</summary>
public static class CosmeticItemDimensions
{
    /// <summary>Gets the normal footprint, using four cells for every skin.</summary>
    /// <param name="group">The item group.</param>
    /// <param name="name">The skin's name.</param>
    /// <returns>The width and height in inventory cells.</returns>
    public static (byte Width, byte Height) Get(byte group, string name)
        => (2, 2);
}
