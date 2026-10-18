// <copyright file="InventoryExtensionConsumeHandlerPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions.ItemConsumeActions;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.CashShop;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Consumes a Magic Backpack held in the inventory to grant an inventory extension.
/// </summary>
/// <remarks>
/// As with cash-shop delivery, selecting the character again rebuilds the inventory
/// with its new capacity. The item is not consumed when the configured limit is reached.
/// </remarks>
[Guid("C882B6E1-D3A1-4CF5-A95D-C10FAE8269B4")]
[PlugIn]
[Display(Name = nameof(PlugInResources.InventoryExtensionDeliveryPlugIn_Name), Description = nameof(PlugInResources.InventoryExtensionDeliveryPlugIn_Description), ResourceType = typeof(PlugInResources))]
public class InventoryExtensionConsumeHandlerPlugIn : BaseConsumeHandlerPlugIn, ISupportCustomConfiguration<InventoryExtensionDeliveryConfiguration>, ISupportDefaultCustomConfiguration
{
    /// <inheritdoc />
    public override ItemIdentifier Key => InventoryExtensionDeliveryPlugIn.MagicBackpack;

    /// <inheritdoc />
    public InventoryExtensionDeliveryConfiguration? Configuration { get; set; }

    /// <inheritdoc />
    public override async ValueTask<bool> ConsumeItemAsync(Player player, Item item, Item? targetItem, FruitUsage fruitUsage)
    {
        if (player.SelectedCharacter is not { } character
            || !await base.ConsumeItemAsync(player, item, targetItem, fruitUsage).ConfigureAwait(false))
        {
            return false;
        }

        character.InventoryExtensions++;
        await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.CashShopInventoryExtended)).ConfigureAwait(false);
        return true;
    }

    /// <inheritdoc />
    public object CreateDefaultConfig() => new InventoryExtensionDeliveryConfiguration();

    /// <inheritdoc />
    protected override bool CheckPreconditions(Player player, Item item)
    {
        var maximum = Math.Min(this.Configuration?.MaximumExtensions ?? InventoryConstants.MaximumNumberOfExtensions, InventoryConstants.MaximumNumberOfExtensions);
        return player.SelectedCharacter is { } character
            && character.InventoryExtensions < maximum
            && base.CheckPreconditions(player, item);
    }
}
