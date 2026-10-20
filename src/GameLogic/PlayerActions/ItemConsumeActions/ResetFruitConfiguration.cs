// <copyright file="ResetFruitConfiguration.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions.ItemConsumeActions;

/// <summary>
/// Defines reset-fruit capacity; Season 6 displays ten points per durability unit.
/// </summary>
public class ResetFruitConfiguration
{
    /// <summary>
    /// Gets or sets the number of invested points refunded per durability unit.
    /// </summary>
    public int PointsPerUnit { get; set; } = 10;
}
