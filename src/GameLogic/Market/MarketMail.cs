// <copyright file="MarketMail.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Market;

using System.Globalization;
using MUnique.OpenMU.Interfaces;
using MUnique.OpenMU.Persistence;

/// <summary>
/// The letters of the Marketplace: bought, cancelled and expired items, and the proceeds of a sale,
/// created in the same transaction as the change of the listing.
/// </summary>
public static class MarketMail
{
    /// <summary>The sender of the letters; the client shows at most 10 characters.</summary>
    public const string SenderName = "Market";

    /// <summary>Creates a letter with an item or an amount of a currency attached.</summary>
    /// <param name="context">The transaction's context.</param>
    /// <param name="receiver">The receiving character's name.</param>
    /// <param name="subject">The subject.</param>
    /// <param name="message">The text.</param>
    /// <param name="item">The attached item, or <c>null</c>.</param>
    /// <param name="priceType">The attached currency, see <see cref="MarketCurrency"/>.</param>
    /// <param name="amount">The attached amount of the currency.</param>
    /// <returns>The letter.</returns>
    /// <exception cref="InvalidOperationException">The receiving character doesn't exist anymore.</exception>
    public static async ValueTask<LetterBody> CreateAsync(IPlayerContext context, string receiver, string subject, string message, Item? item, int priceType, int amount)
    {
        var header = context.CreateNew<LetterHeader>();
        header.SenderName = SenderName;
        header.ReceiverName = receiver;
        header.LetterDate = DateTime.UtcNow;
        header.Subject = subject;
        var body = context.CreateNew<LetterBody>();
        body.Header = header;
        body.SenderAppearance = context.CreateNew<AppearanceData>();
        body.Message = message;
        body.ClaimRevision = Guid.NewGuid();
        body.Attachment = item;
        body.AttachedPriceType = amount > 0 ? priceType : 0;
        body.AttachedPriceAmount = Math.Max(0, amount);
        if (!await context.CanSaveLetterAsync(header).ConfigureAwait(false))
        {
            throw new InvalidOperationException($"The Marketplace letter receiver {receiver} doesn't exist.");
        }

        return body;
    }

    /// <summary>Notifies the receiver of a saved letter, if online.</summary>
    /// <param name="context">The game context.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="letter">The saved letter.</param>
    /// <returns>The completion of the notification.</returns>
    public static async ValueTask ForwardAsync(IGameContext context, ILogger logger, LetterBody letter)
    {
        if (context is not IGameServerContext server)
        {
            return;
        }

        try
        {
            await server.FriendServer.ForwardLetterAsync(letter.Header!).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Marketplace letter {LetterId} was saved but its live notification failed", letter.Header!.Id);
        }
    }

    /// <summary>Gets a localized text of the letters for a receiver who may be offline.</summary>
    /// <param name="culture">The culture.</param>
    /// <param name="key">The key of the <see cref="PlayerMessage"/> resource.</param>
    /// <param name="arguments">The format arguments.</param>
    /// <returns>The text.</returns>
    public static string Text(CultureInfo culture, string key, params object?[] arguments)
        => string.Format(culture, PlayerMessage.ResourceManager.GetString(key, culture) ?? key, arguments);
}
