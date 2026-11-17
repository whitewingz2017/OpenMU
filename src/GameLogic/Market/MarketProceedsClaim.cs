// <copyright file="MarketProceedsClaim.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Market;

using MUnique.OpenMU.GameLogic.Views.Inventory;
using MUnique.OpenMU.GameLogic.Views.Messenger;

/// <summary>
/// Claims the zen, WCoinC or jewels of a Marketplace sale letter: as much as the character can hold;
/// the rest stays in the letter.
/// </summary>
public static class MarketProceedsClaim
{
    /// <summary>Claims the currency of the letter. The caller holds the player's persistence lock.</summary>
    /// <param name="player">The receiver.</param>
    /// <param name="body">The letter, with <see cref="LetterBody.AttachedPriceAmount"/> above zero.</param>
    /// <param name="index">The letter's index.</param>
    /// <returns>The completion.</returns>
    public static async ValueTask ClaimAsync(Player player, LetterBody body, ushort index)
    {
        var type = body.AttachedPriceType;
        var amount = Claimable(player, type, body.AttachedPriceAmount);
        if (amount <= 0)
        {
            await player.ShowLocalizedBlueMessageAsync("Market_ClaimNoRoom").ConfigureAwait(false);
            return;
        }

        if (!await SaveAsync(player, body, type, amount).ConfigureAwait(false))
        {
            await player.ShowLocalizedBlueMessageAsync("StoreClaimFailed").ConfigureAwait(false);
            return;
        }

        var name = MarketCurrency.Name(player.GameContext.Configuration, type);
        var rest = body.AttachedPriceAmount;
        await player.InvokeViewPlugInAsync<IStoreMailAttachmentPlugIn>(v => v.ShowAttachmentAsync(index, rest > 0)).ConfigureAwait(false);
        await player.ShowLocalizedBlueMessageAsync(rest > 0 ? "Market_ClaimPartial" : "Market_Claimed", amount, name, rest).ConfigureAwait(false);
        if (MarketCurrency.IsJewel(type))
        {
            await player.InvokeViewPlugInAsync<IUpdateInventoryListPlugIn>(v => v.UpdateInventoryListAsync()).ConfigureAwait(false);
        }
    }

    /// <summary>Gets how much of the attached amount the character can hold now.</summary>
    private static int Claimable(Player player, int type, int attached)
    {
        long room = type switch
        {
            MarketCurrency.Zen => (player.GameContext.Configuration.MaximumInventoryMoney > 0 ? player.GameContext.Configuration.MaximumInventoryMoney : int.MaxValue) - (long)player.Money,
            MarketCurrency.WCoinC => int.MaxValue - (long)player.Account!.WCoinC,
            _ => MarketCurrency.JewelDefinition(player.GameContext.Configuration, type) is null ? 0 : player.Inventory!.FreeSlots.Count(),
        };

        return (int)Math.Clamp(room, 0, attached);
    }

    private static async ValueTask<bool> SaveAsync(Player player, LetterBody body, int type, int amount)
    {
        var context = player.PersistenceContext;
        context.Attach(player.Account!);
        context.Attach(player.SelectedCharacter!);
        var (revision, attachedType, attachedAmount) = (body.ClaimRevision, body.AttachedPriceType, body.AttachedPriceAmount);
        var jewels = new List<Item>();
        var previousMoney = player.Money;
        var previousCoins = player.Account!.WCoinC;
        var committed = false;
        try
        {
            body.AttachedPriceAmount -= amount;
            body.AttachedPriceType = body.AttachedPriceAmount > 0 ? type : 0;
            body.ClaimRevision = Guid.NewGuid();
            await GiveAsync(player, type, amount, jewels).ConfigureAwait(false);
            committed = await context.SaveChangesAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            player.Logger.LogWarning(ex, "Could not claim the Marketplace proceeds of letter {LetterId}", body.Header?.Id);
        }
        finally
        {
            if (!committed)
            {
                (body.ClaimRevision, body.AttachedPriceType, body.AttachedPriceAmount) = (revision, attachedType, attachedAmount);
                player.Money = previousMoney;
                player.Account!.WCoinC = previousCoins;
                foreach (var jewel in jewels)
                {
                    await player.Inventory!.RemoveItemAsync(jewel).ConfigureAwait(false);
                    await context.DeleteAsync(jewel).ConfigureAwait(false);
                }
            }
        }

        return committed;
    }

    private static async ValueTask GiveAsync(Player player, int type, int amount, List<Item> jewels)
    {
        switch (type)
        {
            case MarketCurrency.Zen:
                if (!player.TryAddMoney(amount))
                {
                    throw new InvalidOperationException("The zen limit changed.");
                }

                break;
            case MarketCurrency.WCoinC:
                player.Account!.WCoinC += amount;
                break;
            default:
                var definition = MarketCurrency.JewelDefinition(player.GameContext.Configuration, type)!;
                for (var i = 0; i < amount; i++)
                {
                    var jewel = player.PersistenceContext.CreateNew<Item>();
                    jewel.Definition = definition;
                    jewel.Durability = definition.Durability > 1 ? 1 : definition.Durability;
                    if (!await player.Inventory!.AddItemAsync(jewel).ConfigureAwait(false))
                    {
                        await player.PersistenceContext.DeleteAsync(jewel).ConfigureAwait(false);
                        throw new InvalidOperationException("The inventory space changed.");
                    }

                    jewels.Add(jewel);
                }

                break;
        }
    }
}
