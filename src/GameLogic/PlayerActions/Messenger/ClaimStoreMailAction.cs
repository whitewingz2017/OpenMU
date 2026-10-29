// <copyright file="ClaimStoreMailAction.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions.Messenger;

using MUnique.OpenMU.GameLogic.Views.Inventory;
using MUnique.OpenMU.GameLogic.Views.Messenger;

/// <summary>Transfers a letter attachment exactly once to its recipient's inventory.</summary>
public sealed class ClaimStoreMailAction
{
    /// <summary>Claims an attachment with player persistence serialization and optimistic database protection.</summary>
    /// <param name="player">The player.</param>
    /// <param name="index">The index.</param>
    public ValueTask ClaimAsync(Player player, ushort index) => player.RunPersistenceExclusiveAsync(() => ClaimCoreAsync(player, index));

    private static async ValueTask ClaimCoreAsync(Player player, ushort index)
    {
        if (player.SelectedCharacter is not { } character || player.Inventory is null
            || index >= character.Letters.Count || player.PlayerState.CurrentState != PlayerState.EnteredWorld
            || player.ShopStorage?.StoreOpen == true || player.OpenedNpc is not null
            || player.TemporaryStorage?.Items.Any() == true || player.IsCashShopOpen)
        {
            return;
        }

        var header = character.Letters[index];
        var context = player.PersistenceContext;
        var body = await context.GetLetterBodyByHeaderIdAsync(header.Id).ConfigureAwait(false);
        if (body?.Attachment is not { } item)
        {
            await player.ShowLocalizedBlueMessageAsync("StoreClaimUnavailable").ConfigureAwait(false);
            await player.InvokeViewPlugInAsync<IStoreMailAttachmentPlugIn>(v => v.ShowAttachmentAsync(index, false)).ConfigureAwait(false);
            return;
        }

        var slot = player.Inventory.CheckInvSpace(item);
        if (slot is null)
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.InventoryNotEnoughSpace)).ConfigureAwait(false);
            return;
        }
        context.Attach(player.Account!);
        context.Attach(character);
        var revision = body.ClaimRevision;
        var added = false;
        var committed = false;
        try
        {
            body.Attachment = null;
            body.ClaimRevision = Guid.NewGuid();
            added = await player.Inventory.AddItemAsync((byte)slot, item).ConfigureAwait(false);
            if (!added || !await context.SaveChangesAsync().ConfigureAwait(false))
            {
                throw new InvalidOperationException("Attachment claim was not saved.");
            }

            committed = true;
        }
        catch (Exception ex)
        {
            player.Logger.LogWarning(ex, "Could not claim store mail {LetterId}", header.Id);
            await player.ShowLocalizedBlueMessageAsync("StoreClaimFailed").ConfigureAwait(false);
        }
        finally
        {
            if (!committed)
            {
                if (added)
                {
                    await player.Inventory.RemoveItemAsync(item).ConfigureAwait(false);
                }

                body.Attachment = item;
                body.ClaimRevision = revision;
            }
        }

        if (!committed)
        {
            return;
        }


        await player.InvokeViewPlugInAsync<IStoreMailAttachmentPlugIn>(v => v.ShowAttachmentAsync(index, false)).ConfigureAwait(false);
        await player.InvokeViewPlugInAsync<IUpdateInventoryListPlugIn>(v => v.UpdateInventoryListAsync()).ConfigureAwait(false);
        await player.ShowLocalizedBlueMessageAsync("StoreClaimSuccess").ConfigureAwait(false);
    }
}

