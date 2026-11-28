// <copyright file="DuelBetRejection.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.DuelBetting;

/// <summary>The result of validating a confirmed spectator ticket.</summary>
public enum DuelBetRejection
{
    /// <summary>The ticket was accepted.</summary>
    None,

    /// <summary>The betting deadline passed or the match is locked.</summary>
    Closed,

    /// <summary>A duelist's account cannot bet on its own match.</summary>
    OwnMatch,

    /// <summary>The currency is unknown or disabled.</summary>
    CurrencyDisabled,

    /// <summary>The amount is outside its currency's configured range.</summary>
    InvalidAmount,

    /// <summary>The ticket identity was already used.</summary>
    DuplicateTicket,

    /// <summary>The ticket, account, or fighter identity is invalid.</summary>
    InvalidIdentity,

    /// <summary>The configured pool or account ticket cap would be exceeded.</summary>
    LimitReached,

    /// <summary>The account already chose the other fighter.</summary>
    OppositeSide,

    /// <summary>The Jewel Bank has insufficient funds.</summary>
    NotEnoughJewels,

    /// <summary>The transaction could not be completed.</summary>
    TryAgain,

    /// <summary>The account has insufficient WCoinC.</summary>
    NotEnoughWCoins,
}
