// <copyright file="JewelBankRow.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.JewelBank;

/// <summary>A server-confirmed jewel bank balance.</summary>
/// <param name="Item">The protocol item identifier.</param>
/// <param name="Name">The configured item name.</param>
/// <param name="Balance">The stored single-jewel count.</param>
/// <param name="Inventory">The depositable count, including bundles.</param>
/// <param name="Limit">The maximum bank balance.</param>
public sealed record JewelBankRow(int Item, string Name, int Balance, int Inventory, int Limit);
