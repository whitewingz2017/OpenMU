// <copyright file="JewelAmount.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence;

/// <summary>A quantity in units of single jewels.</summary>
/// <param name="ItemCode">The group times 512 plus item number.</param>
/// <param name="Amount">The quantity.</param>
public sealed record JewelAmount(int ItemCode, long Amount);
