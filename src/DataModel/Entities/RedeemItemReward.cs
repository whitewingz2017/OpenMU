// <copyright file="RedeemItemReward.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.DataModel.Entities;

/// <summary>One plain item reward. ItemCode is group * 512 + number.</summary>
public class RedeemItemReward
{
    /// <summary>Gets or sets the item identifier.</summary>
    public int ItemCode { get; set; }
    /// <summary>Gets or sets the item level.</summary>
    public byte Level { get; set; }
    /// <summary>Gets or sets the number of separate items.</summary>
    public int Count { get; set; } = 1;
}
