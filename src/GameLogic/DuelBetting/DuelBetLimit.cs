// <copyright file="DuelBetLimit.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.DuelBetting;

/// <summary>The configurable stake range for one currency.</summary>
public sealed class DuelBetLimit
{
    /// <summary>Gets or sets the smallest stake.</summary>
    [Range(1, int.MaxValue)]
    public int Minimum { get; set; } = 1;

    /// <summary>Gets or sets the largest stake.</summary>
    [Range(1, int.MaxValue)]
    public int Maximum { get; set; } = 250;
}
