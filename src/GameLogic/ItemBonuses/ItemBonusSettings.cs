// <copyright file="ItemBonusSettings.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.ItemBonuses;

using MUnique.OpenMU.DataModel.Configuration.Items;

/// <summary>Which items show their attributes in purple in the custom client, edited on the plugin's Configure page.</summary>
public class ItemBonusSettings
{
    /// <summary>Gets or sets a value indicating whether every custom item with attributes shows them.</summary>
    [Display(Name = "Include custom items", Description = "Every custom item shows its attributes: the items added in the admin panel, the custom Fenrir mounts (e.g. Mount Bear) and the skins. Classic items show their stats from the client's item data.")]
    public bool IncludeCustomItems { get; set; } = true;

    /// <summary>Gets or sets more items whose attributes show, e.g. a classic item whose attributes were changed.</summary>
    [Display(Name = "Also show", Description = "Items which show their attributes too, though they aren't custom items.")]
    public ICollection<ItemDefinition> Items { get; set; } = new List<ItemDefinition>();

    /// <summary>Gets or sets the items whose attributes never show.</summary>
    [Display(Name = "Never show", Description = "Items which never show their attributes, e.g. a custom item whose tooltip already shows them.")]
    public ICollection<ItemDefinition> HiddenItems { get; set; } = new List<ItemDefinition>();
}
