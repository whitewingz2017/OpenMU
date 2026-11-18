// <copyright file="ItemBonusFeaturePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.ItemBonuses;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Shows the base power-up attributes of the picked items in the custom client: in purple in the item tooltip and,
/// for worn items, beside the Combat statistics they add to.
/// </summary>
[PlugIn]
[Display(Name = nameof(ItemBonusResources.Name), Description = nameof(ItemBonusResources.Description), ResourceType = typeof(ItemBonusResources))]
[Guid("3f0b7a52-91d4-4c1e-b6a8-5d2e7c9f4a13")]
public sealed class ItemBonusFeaturePlugIn : IFeaturePlugIn, ISupportCustomConfiguration<ItemBonusSettings>, ISupportDefaultCustomConfiguration
{
    /// <inheritdoc />
    public ItemBonusSettings? Configuration { get; set; }

    /// <summary>Gets the items of the active plugin; empty when it is inactive.</summary>
    /// <param name="context">The game context.</param>
    /// <returns>The items.</returns>
    public static IReadOnlyCollection<ItemDefinition> GetItems(IGameContext context)
    {
        var items = context.FeaturePlugIns.GetPlugIn<ItemBonusFeaturePlugIn>()?.Configuration?.Items;
        return items is null ? Array.Empty<ItemDefinition>() : items.ToList();
    }

    /// <summary>Determines whether the item is one of the picked items.</summary>
    /// <param name="items">The picked items.</param>
    /// <param name="definition">The item's definition.</param>
    /// <returns><c>true</c> if its attributes show.</returns>
    public static bool Contains(IEnumerable<ItemDefinition> items, ItemDefinition? definition)
        => definition is not null && items.Any(item => item.Group == definition.Group && item.Number == definition.Number);

    /// <inheritdoc />
    public object CreateDefaultConfig() => new ItemBonusSettings();
}
