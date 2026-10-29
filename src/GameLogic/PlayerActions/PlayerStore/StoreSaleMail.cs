// <copyright file="StoreSaleMail.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions.PlayerStore;

using MUnique.OpenMU.Interfaces;
using MUnique.OpenMU.Persistence;

/// <summary>Creates the two durable receipts in the same transaction as payment and delivery.</summary>
internal static class StoreSaleMail
{
    public static async ValueTask<LetterBody> CreateAsync(IPlayerContext context, Player receiver, Player other,
        Item item, StorePricePart[] price, bool purchase)
    {
        var header = context.CreateNew<LetterHeader>();
        header.SenderName = other.Name;
        header.ReceiverName = receiver.Name;
        header.LetterDate = DateTime.UtcNow;
        header.Subject = receiver.GetLocalizedMessage(purchase ? "StorePurchaseReceipt" : "StoreSaleReceipt");
        var body = context.CreateNew<LetterBody>();
        body.Header = header;
        body.SenderAppearance = context.CreateNew<AppearanceData>();
        body.SenderAppearance.CharacterClass = other.AppearanceData.CharacterClass;
        var payment = string.Join(" + ", price.Select(p => $"{p.Amount:N0} {CurrencyName(receiver, p.Type)}"));
        body.Message = string.Format(
            receiver.GetLocalizedMessage("StoreReceiptDetails"),
            purchase ? other.Name : receiver.Name, purchase ? receiver.Name : other.Name,
            item.ToString(receiver.Culture), payment);
        body.ClaimRevision = Guid.NewGuid();
        if (purchase)
        {
            body.Attachment = item;
        }

        if (!await context.CanSaveLetterAsync(header).ConfigureAwait(false))
        {
            throw new InvalidOperationException("Store receipt recipient no longer exists.");
        }

        return body;
    }

    private static string CurrencyName(Player player, int type) => type == 0 ? "Zen" : StoreCurrency.IsCoin(type)
        ? StoreCurrency.CoinType(type).ToString()
        : player.GameContext.Configuration.Items.FirstOrDefault(i => ((i.Group << 12) | i.Number) == type)?.Name ?? type.ToString();

    public static async ValueTask ForwardAsync(Player player, LetterBody letter)
    {
        if (player.GameContext is not IGameServerContext server) return;
        try
        {
            await server.FriendServer.ForwardLetterAsync(letter.Header!).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            player.Logger.LogError(ex, "Store receipt {LetterId} was saved but its live notification failed", letter.Header!.Id);
        }
    }
}
