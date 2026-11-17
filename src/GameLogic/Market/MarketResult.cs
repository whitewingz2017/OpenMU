// <copyright file="MarketResult.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Market;

/// <summary>The result of a Marketplace request, as the client explains it.</summary>
public enum MarketResult
{
    /// <summary>The market was loaded.</summary>
    None = 0,

    /// <summary>The item is listed.</summary>
    Listed = 1,

    /// <summary>The item was bought; it's in the mail.</summary>
    Bought = 2,

    /// <summary>The listing was cancelled; the item is in the mail.</summary>
    Cancelled = 3,

    /// <summary>The Marketplace is disabled on this server.</summary>
    Disabled = 10,

    /// <summary>The request couldn't be read, or a value is out of range.</summary>
    Invalid = 11,

    /// <summary>The item can't be sold on the market.</summary>
    ItemNotAllowed = 12,

    /// <summary>The currency isn't allowed, or the price is out of range.</summary>
    PriceNotAllowed = 13,

    /// <summary>The character has the most listings allowed.</summary>
    ListingLimit = 14,

    /// <summary>The character's level is below the market's minimum.</summary>
    LevelTooLow = 15,

    /// <summary>The buyer lacks the price.</summary>
    NotEnoughCurrency = 16,

    /// <summary>The listing was sold, cancelled or expired meanwhile.</summary>
    NotAvailable = 17,

    /// <summary>The listing belongs to the player's own account.</summary>
    OwnListing = 18,

    /// <summary>The listing belongs to another character.</summary>
    NotYourListing = 19,

    /// <summary>The player is busy, e.g. trading, with a shop open or talking to an NPC.</summary>
    Busy = 20,

    /// <summary>The mail couldn't be sent, e.g. because the mailbox is full.</summary>
    MailFailed = 21,

    /// <summary>The server couldn't save the change; nothing changed.</summary>
    TryAgain = 22,
}
