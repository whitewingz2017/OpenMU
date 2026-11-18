// <copyright file="ItemBonusItem.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.ItemBonuses;

/// <summary>An item's attribute lines.</summary>
public sealed class ItemBonusItem
{
    /// <summary>Gets or sets the item type: group * 512 + number.</summary>
    public int Type { get; set; }

    /// <summary>Gets or sets the lines, one per attribute.</summary>
    public IReadOnlyList<ItemBonusLine> Lines { get; set; } = [];
}
