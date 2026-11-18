// <copyright file="ItemBonusSettings.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.ItemBonuses;

using MUnique.OpenMU.DataModel.Configuration.Items;

/// <summary>The items whose attributes the custom client shows in purple, edited on the plugin's Configure page.</summary>
public class ItemBonusSettings
{
    /// <summary>Gets or sets the items whose base power-up attributes show in their tooltip and in Combat statistics.</summary>
    [Display(Name = "Items", Description = "Their base power-up attributes show in purple in the item tooltip and, while worn, in Combat statistics. Classic items show their stats from the client's item data; add only items whose attributes the tooltip doesn't show.")]
    public ICollection<ItemDefinition> Items { get; set; } = new List<ItemDefinition>();
}
