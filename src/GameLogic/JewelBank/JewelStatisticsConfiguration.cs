// <copyright file="JewelStatisticsConfiguration.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.JewelBank;

/// <summary>The jewel types tracked by monster drop statistics.</summary>
public sealed class JewelStatisticsConfiguration
{
    /// <summary>Gets or sets single jewel codes, expressed as group times 512 plus number.</summary>
    public int[] ItemCodes { get; set; } = [7181, 7182, 6159, 7184, 7190, 7199, 7210];
}
