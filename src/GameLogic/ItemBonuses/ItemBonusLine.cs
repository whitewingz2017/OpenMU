// <copyright file="ItemBonusLine.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.ItemBonuses;

/// <summary>An attribute's line.</summary>
public sealed class ItemBonusLine
{
    /// <summary>Gets or sets the text at an item level without a level bonus.</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>Gets or sets the text at each item level which has a level bonus, by level.</summary>
    public IReadOnlyDictionary<string, string> ByLevel { get; set; } = new Dictionary<string, string>();
}
