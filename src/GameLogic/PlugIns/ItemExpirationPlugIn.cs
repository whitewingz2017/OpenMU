// <copyright file="ItemExpirationPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using System.ComponentModel;
using System.Runtime.InteropServices;
using MUnique.OpenMU.Pathfinding;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Enforces wall-clock item lifetimes independently of whether the cash shop is open.
/// </summary>
[PlugIn]
[Display(Name = nameof(PlugInResources.ItemExpirationPlugIn_Name), Description = nameof(PlugInResources.ItemExpirationPlugIn_Description), ResourceType = typeof(PlugInResources))]
[Guid("83e6898d-76b6-4982-a0d4-48827ae92226")]
public class ItemExpirationPlugIn : IPeriodicTaskPlugIn, IPlayerStateChangedPlugIn, IItemMovingPlugIn, IItemConsumingPlugIn, IItemDropPlugIn
{
    /// <inheritdoc />
    public async ValueTask ExecuteTaskAsync(GameContext gameContext)
    {
        foreach (var player in await gameContext.GetPlayersAsync().ConfigureAwait(false))
        {
            if (player.Inventory is null || player.PlayerState.CurrentState == PlayerState.Disconnected)
            {
                continue;
            }

            try
            {
                await ItemExpiration.RemoveExpiredAsync(player, DateTime.UtcNow).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                player.Logger.LogError(exception, "Removing expired items failed for {Player}.", player);
            }
        }
    }

    /// <inheritdoc />
    public void ForceStart()
    {
        // The game context already invokes this task every second.
    }

    /// <inheritdoc />
    public ValueTask PlayerStateChangedAsync(Player player, State previousState, State currentState)
        => player.Inventory is null || currentState == PlayerState.Disconnected
            ? ValueTask.CompletedTask : ItemExpiration.RemoveExpiredAsync(player, DateTime.UtcNow);

    /// <inheritdoc />
    public void ItemMoving(Player player, Item item, Storages targetStorage, int slot, CancelEventArgs eventArgs)
    {
        if (item.ExpiresAtUtc is { } deadline &&
            (deadline <= DateTime.UtcNow || targetStorage is not (Storages.Inventory or Storages.Vault)))
        {
            // A temporary item must not become a permanent crafting output or enter
            // another account through a trade while its owner's clock is expiring.
            eventArgs.Cancel = true;
        }
    }

    /// <inheritdoc />
    public void ItemConsuming(Player player, Item item, Item? targetItem, CancelEventArgs eventArgs)
    {
        if (item.ExpiresAtUtc <= DateTime.UtcNow || targetItem?.ExpiresAtUtc <= DateTime.UtcNow)
        {
            eventArgs.Cancel = true;
        }
    }

    /// <inheritdoc />
    public ValueTask HandleItemDropAsync(Player player, Item item, Point target, IItemDropPlugIn.ItemDropArguments dropArgs)
    {
        if (item.ExpiresAtUtc is not null)
        {
            dropArgs.Cancel = true;
        }

        return ValueTask.CompletedTask;
    }
}
