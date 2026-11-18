// <copyright file="ItemBonusAnswer.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.ItemBonuses;

/// <summary>The attribute lines of the picked items, for the custom client's tooltips.</summary>
public sealed class ItemBonusAnswer
{
    /// <summary>Gets or sets the request identifier.</summary>
    public uint RequestId { get; set; }

    /// <summary>Gets or sets the items.</summary>
    public IReadOnlyList<ItemBonusItem> Items { get; set; } = [];
}
