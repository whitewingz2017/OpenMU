// <copyright file="OfflineLevelingCosts.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Offline;

using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.MuHelper;

/// <summary>
/// The prices of offline leveling: the leveling itself in the chosen currency, and the protection from players
/// in the same currency (a zen session without a zen price for it pays it in WCoinC). Zen without an hourly
/// price costs what the MU Helper costs: its level-based cost stages, paid every pay interval.
/// </summary>
public static class OfflineLevelingCosts
{
    /// <summary>Determines whether the plan is allowed by the settings.</summary>
    /// <param name="settings">The settings.</param>
    /// <param name="plan">The plan.</param>
    /// <returns><c>true</c> if allowed.</returns>
    public static bool IsAllowed(OfflineLevelingSettings settings, OfflineLevelingPlan plan)
        => IsAllowed(settings, plan.Currency)
           && plan.Hours >= 0 && plan.Hours <= settings.MaximumHours
           && (!plan.Protection || settings.AllowProtection);

    /// <summary>Determines whether the currency is allowed by the settings.</summary>
    /// <param name="settings">The settings.</param>
    /// <param name="currency">The currency.</param>
    /// <returns><c>true</c> if allowed.</returns>
    public static bool IsAllowed(OfflineLevelingSettings settings, OfflineLevelingCurrency currency) => currency switch
    {
        OfflineLevelingCurrency.Zen => settings.AllowZen,
        OfflineLevelingCurrency.WCoinC => settings.AllowWCoinC,
        OfflineLevelingCurrency.GoblinPoints => settings.AllowGoblinPoints,
        _ => false,
    };

    /// <summary>Determines whether zen is paid as the MU Helper pays it, by level and pay interval.</summary>
    /// <param name="settings">The settings.</param>
    /// <param name="currency">The currency.</param>
    /// <returns><c>true</c> for the MU Helper's level-based zen.</returns>
    public static bool IsLevelBasedZen(OfflineLevelingSettings settings, OfflineLevelingCurrency currency)
        => currency == OfflineLevelingCurrency.Zen && settings.ZenPerHour <= 0;

    /// <summary>Gets the price of an hour of leveling.</summary>
    /// <param name="player">The player, for the level-based zen.</param>
    /// <param name="settings">The settings.</param>
    /// <param name="currency">The currency.</param>
    /// <returns>The price.</returns>
    public static OfflineLevelingCostPart LevelingPerHour(Player player, OfflineLevelingSettings settings, OfflineLevelingCurrency currency) => currency switch
    {
        OfflineLevelingCurrency.Zen when IsLevelBasedZen(settings, currency) => new(currency, LevelBasedZen(player, TimeSpan.FromHours(1))),
        OfflineLevelingCurrency.Zen => new(currency, settings.ZenPerHour),
        OfflineLevelingCurrency.WCoinC => new(currency, settings.WCoinCPerHour),
        _ => new(currency, settings.GoblinPointsPerHour),
    };

    /// <summary>Gets the price of an hour of protection, in the currency it is billed in.</summary>
    /// <param name="settings">The settings.</param>
    /// <param name="currency">The currency of the leveling.</param>
    /// <returns>The price.</returns>
    public static OfflineLevelingCostPart ProtectionPerHour(OfflineLevelingSettings settings, OfflineLevelingCurrency currency) => currency switch
    {
        OfflineLevelingCurrency.Zen when settings.ProtectionZenPerHour > 0 => new(currency, settings.ProtectionZenPerHour),
        OfflineLevelingCurrency.Zen or OfflineLevelingCurrency.WCoinC => new(OfflineLevelingCurrency.WCoinC, settings.ProtectionWCoinCPerHour),
        _ => new(currency, settings.ProtectionGoblinPointsPerHour),
    };

    /// <summary>
    /// Gets what the session costs at its start: the prepaid hours, or the first hour of a session paid every
    /// hour. Level-based zen pays the MU Helper's start cost and, for prepaid hours, every pay interval of them.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="settings">The settings.</param>
    /// <param name="plan">The plan.</param>
    /// <returns>The cost, one part per currency.</returns>
    public static IReadOnlyList<OfflineLevelingCostPart> StartCost(Player player, OfflineLevelingSettings settings, OfflineLevelingPlan plan)
    {
        var hours = Math.Max(1, plan.Hours);
        var parts = new List<OfflineLevelingCostPart>();
        if (IsLevelBasedZen(settings, plan.Currency))
        {
            var prepaid = plan.Hours > 0 ? LevelBasedZen(player, TimeSpan.FromHours(plan.Hours)) : 0;
            parts.Add(new(OfflineLevelingCurrency.Zen, LevelBasedStartZen(player) + prepaid));
        }
        else
        {
            parts.Add(Times(LevelingPerHour(player, settings, plan.Currency), hours));
        }

        if (plan.Protection)
        {
            parts.Add(Times(ProtectionPerHour(settings, plan.Currency), hours));
        }

        return Merge(parts);
    }

    /// <summary>Gets the player's balance of a currency.</summary>
    /// <param name="player">The player.</param>
    /// <param name="currency">The currency.</param>
    /// <returns>The balance.</returns>
    public static long Balance(Player player, OfflineLevelingCurrency currency) => currency switch
    {
        OfflineLevelingCurrency.Zen => player.Money,
        OfflineLevelingCurrency.WCoinC => player.Account?.WCoinC ?? 0,
        _ => player.Account?.GoblinPoints ?? 0,
    };

    /// <summary>Determines whether the player can pay every part.</summary>
    /// <param name="player">The player.</param>
    /// <param name="parts">The cost.</param>
    /// <returns><c>true</c> if the player can pay.</returns>
    public static bool CanPay(Player player, IEnumerable<OfflineLevelingCostPart> parts)
        => parts.All(part => part.Amount <= int.MaxValue && Balance(player, part.Currency) >= part.Amount);

    /// <summary>Takes every part, or none if the player can't pay one.</summary>
    /// <param name="player">The player.</param>
    /// <param name="parts">The cost.</param>
    /// <returns><c>true</c> if the cost was paid.</returns>
    public static bool TryPay(Player player, IReadOnlyList<OfflineLevelingCostPart> parts)
    {
        if (!CanPay(player, parts) || player.Account is not { } account)
        {
            return false;
        }

        foreach (var part in parts.Where(part => part.Amount > 0))
        {
            var amount = (int)part.Amount;
            switch (part.Currency)
            {
                case OfflineLevelingCurrency.Zen:
                    player.TryRemoveMoney(amount);
                    break;
                case OfflineLevelingCurrency.WCoinC:
                    account.WCoinC -= amount;
                    break;
                default:
                    account.GoblinPoints -= amount;
                    break;
            }
        }

        return true;
    }

    /// <summary>
    /// Gets the MU Helper's zen for a session of the given length: every pay interval at the cost stage it falls
    /// in, as <see cref="ZenConsumptionHandler"/> charges it.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="duration">The length of the session.</param>
    /// <returns>The zen.</returns>
    public static long LevelBasedZen(Player player, TimeSpan duration)
    {
        var configuration = HelperConfiguration(player);
        if (configuration.CostPerStage.Count == 0 || configuration.StageInterval <= TimeSpan.Zero || configuration.PayInterval <= TimeSpan.Zero)
        {
            return 0;
        }

        long zen = 0;
        var payments = (int)(duration / configuration.PayInterval);
        for (var payment = 1; payment <= payments; payment++)
        {
            var stage = Math.Clamp((int)(payment * configuration.PayInterval / configuration.StageInterval), 0, configuration.CostPerStage.Count - 1);
            zen += (long)configuration.CostPerStage[stage] * TotalLevel(player);
        }

        return zen;
    }

    /// <summary>Gets the MU Helper's zen at the start: the first cost stage times the level.</summary>
    /// <param name="player">The player.</param>
    /// <returns>The zen.</returns>
    public static long LevelBasedStartZen(Player player)
        => Math.Max(0, (long)HelperConfiguration(player).CostPerStage.FirstOrDefault() * TotalLevel(player));

    private static MuHelperConfiguration HelperConfiguration(Player player)
        => player.GameContext.FeaturePlugIns.GetPlugIn<MuHelperFeaturePlugIn>()?.Configuration ?? new MuHelperConfiguration();

    private static int TotalLevel(Player player) => player.Level + (int)(player.Attributes?[Stats.MasterLevel] ?? 0);

    private static OfflineLevelingCostPart Times(OfflineLevelingCostPart part, int hours) => part with { Amount = part.Amount * hours };

    private static IReadOnlyList<OfflineLevelingCostPart> Merge(IEnumerable<OfflineLevelingCostPart> parts)
        => parts.GroupBy(part => part.Currency).Select(group => new OfflineLevelingCostPart(group.Key, group.Sum(part => part.Amount))).ToList();
}
