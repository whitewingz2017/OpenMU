// <copyright file="ItemBonusFeaturePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.ItemBonuses;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.Persistence;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Shows the base power-up attributes of the custom items in the custom client: in purple in the item tooltip, in
/// the Guide Book and, for worn items, beside the Combat statistics they add to.
/// </summary>
[PlugIn]
[Display(Name = nameof(ItemBonusResources.Name), Description = nameof(ItemBonusResources.Description), ResourceType = typeof(ItemBonusResources))]
[Guid("3f0b7a52-91d4-4c1e-b6a8-5d2e7c9f4a13")]
public sealed class ItemBonusFeaturePlugIn : IFeaturePlugIn, ISupportCustomConfiguration<ItemBonusSettings>, ISupportDefaultCustomConfiguration
{
    /// <inheritdoc />
    public ItemBonusSettings? Configuration { get; set; }

    /// <summary>Gets the items whose attributes show: the custom items and the listed ones, without the hidden ones; none when the plugin is inactive.</summary>
    /// <param name="context">The game context.</param>
    /// <returns>The items.</returns>
    public static IReadOnlyCollection<ItemDefinition> GetItems(IGameContext context)
    {
        if (context.FeaturePlugIns.GetPlugIn<ItemBonusFeaturePlugIn>() is not { } plugIn)
        {
            return Array.Empty<ItemDefinition>();
        }

        var settings = plugIn.Configuration ??= new ItemBonusSettings();

        // The skins last: answers and the Guide Book list as many items as they hold.
        return context.Configuration.Items
            .Where(definition => definition.BasePowerUpAttributes.Count > 0
                && !Contains(settings.HiddenItems, definition)
                && (Contains(settings.Items, definition) || (settings.IncludeCustomItems && IsCustom(definition))))
            .OrderBy(definition => Cosmetics.CosmeticArmorRules.GetSlot(definition) is not null)
            .ThenBy(definition => definition.Group)
            .ThenBy(definition => definition.Number)
            .ToList();
    }

    /// <summary>Determines whether the item is one of the items.</summary>
    /// <param name="items">The items.</param>
    /// <param name="definition">The item's definition.</param>
    /// <returns><c>true</c> if its attributes show.</returns>
    public static bool Contains(IEnumerable<ItemDefinition> items, ItemDefinition? definition)
        => definition is not null && items.Any(item => item.Group == definition.Group && item.Number == definition.Number);

    /// <summary>
    /// Determines whether the item is a custom item, whose attributes the client's classic tooltip doesn't show: an item
    /// added in the admin panel (the initial data gives every item the identifier of its group and number), a custom
    /// Fenrir mount or a skin.
    /// </summary>
    /// <param name="definition">The item's definition.</param>
    /// <returns><c>true</c> for a custom item.</returns>
    public static bool IsCustom(ItemDefinition definition)
        => Cosmetics.FenrirMountRules.IsCustom(definition)
           || Cosmetics.CosmeticArmorRules.GetSlot(definition) is not null
           || IsAddedLater(definition);

    /// <inheritdoc />
    public object CreateDefaultConfig() => new ItemBonusSettings();

    private static bool IsAddedLater(ItemDefinition definition)
    {
        const int ItemDefinitionTypeId = 0x80;
        var id = definition.GetId();
        return id != Guid.Empty && id != new Guid(ItemDefinitionTypeId, (short)definition.Group, (short)definition.Number, 0, 0, 0, 0, 0, 0, 0, 0);
    }
}
