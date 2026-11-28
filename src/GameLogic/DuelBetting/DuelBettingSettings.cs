// <copyright file="DuelBettingSettings.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.DuelBetting;

/// <summary>Opt-in duel betting settings; amounts are units of the selected currency.</summary>
public sealed class DuelBettingSettings
{
    /// <summary>Gets or sets each enabled currency and its permitted stake range.</summary>
    public Dictionary<DuelBetCurrency, DuelBetLimit> Limits { get; set; } = new()
    {
        [DuelBetCurrency.Bless] = new(),
        [DuelBetCurrency.Soul] = new(),
        [DuelBetCurrency.Life] = new(),
    };

    /// <summary>Gets or sets the WCoinC range for each fighter's wager and each spectator's bet.</summary>
    public DuelBetLimit WCoinLimit { get; set; } = new();

    /// <summary>Gets or sets the pre-combat betting period in seconds.</summary>
    [Range(5, 120)]
    public int BettingSeconds { get; set; } = 30;

    /// <summary>Gets or sets the fee percentage, deducted only from contested pools.</summary>
    [Range(0, 100)]
    public int FeePercent { get; set; }

    /// <summary>Gets or sets the maximum total stake in one currency pool.</summary>
    [Range(1, int.MaxValue)]
    public int MaximumPool { get; set; } = int.MaxValue;

    /// <summary>Gets or sets the maximum tickets an account may place in a match.</summary>
    [Range(1, 100)]
    public int MaximumTicketsPerAccount { get; set; } = 20;

    /// <summary>Checks ranges and currency identifiers before accepting any stake.</summary>
    /// <returns>Whether the settings are coherent.</returns>
    public bool IsValid()
        => Validator.TryValidateObject(this, new ValidationContext(this), null, true)
            && this.WCoinLimit is { Minimum: > 0 } wCoins && wCoins.Minimum <= wCoins.Maximum
            && wCoins.Maximum <= int.MaxValue / 2
            && this.Limits is { Count: > 0 } && this.Limits.All(pair => Enum.IsDefined(pair.Key) && pair.Key != DuelBetCurrency.WCoinC
                && pair.Value is { } limit && Validator.TryValidateObject(limit, new ValidationContext(limit), null, true)
                && limit.Minimum <= limit.Maximum && limit.Maximum <= this.MaximumPool);
}
