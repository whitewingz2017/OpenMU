// <copyright file="GuideBookValues.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.GuideBook;

using System.Globalization;
using System.Text.Json;
using MUnique.OpenMU.GameLogic.PlayerActions.Items;
using MUnique.OpenMU.GameLogic.PlugIns.BossKillRewards;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// The live settings of other features which the Guide Book quotes, so its pages stay right when an
/// administrator changes them: values for the "{key}" placeholders of the pages, and the boss table.
/// </summary>
public static class GuideBookValues
{
    /// <summary>
    /// The most Resonance Cores a boss drops in a day: the <see cref="ResonanceCoreDropPlugIn"/> drops one
    /// core, and a boss only once per day.
    /// </summary>
    internal const int CoresPerBossAndDay = 1;

    /// <summary>The most bosses the client reads; it refuses an answer with a longer table.</summary>
    internal const int MaximumBossRewards = 64;

    /// <summary>
    /// Collects the values: "exc.base", "exc.bonus", "exc.max" and "exc.zen" of the excellent combination,
    /// "core.boss" and "core.monster" of the Resonance Core drops.
    /// </summary>
    /// <param name="context">The game context.</param>
    /// <returns>The values by key.</returns>
    public static IReadOnlyDictionary<string, string> Collect(IGameContext context)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        var combination = ReadConfiguration<ExcellentCombinationConfiguration>(context, typeof(ExcellentCombinationFeaturePlugIn))
            ?? new ExcellentCombinationConfiguration();
        values["exc.base"] = Number(combination.BaseRate);
        values["exc.bonus"] = Number(combination.BonusPerCore);
        values["exc.max"] = Number(combination.MaximumRate);
        values["exc.zen"] = Number(combination.ZenCost);

        var cores = ReadConfiguration<ResonanceCoreDropConfiguration>(context, typeof(ResonanceCoreDropPlugIn))
            ?? new ResonanceCoreDropConfiguration();
        values["core.boss"] = Percent(cores.BossDropChance);
        values["core.monster"] = Percent(cores.MonsterDropChance);
        return values;
    }

    /// <summary>
    /// Collects the bosses with a reward: the WCoin and Goblin Points of the <see cref="BossKillRewardPlugIn"/>
    /// and the Resonance Cores of the <see cref="ResonanceCoreDropPlugIn"/>, as far as these are active.
    /// </summary>
    /// <param name="context">The game context.</param>
    /// <returns>The bosses, in the order of their configuration.</returns>
    public static IReadOnlyList<GuideBookBossReward> BossRewards(IGameContext context)
    {
        // Without a configuration, the boss kill reward plugin gives nothing, while the core drop works with its defaults.
        List<BossKillReward> coins = IsActive(context, typeof(BossKillRewardPlugIn))
            ? ReadConfiguration<BossKillRewardConfiguration>(context, typeof(BossKillRewardPlugIn))?.Rewards
                .Where(reward => reward.Enabled && reward is not { WCoinC: <= 0, WCoinP: <= 0, GoblinPoints: <= 0 })
                .ToList() ?? new List<BossKillReward>()
            : new List<BossKillReward>();
        var cores = IsActive(context, typeof(ResonanceCoreDropPlugIn))
            ? ReadConfiguration<ResonanceCoreDropConfiguration>(context, typeof(ResonanceCoreDropPlugIn)) ?? new ResonanceCoreDropConfiguration()
            : null;
        IList<short> coreBosses = cores is { BossDropChance: > 0 } ? cores.BossMonsterNumbers : new List<short>();
        return coins.Select(reward => reward.MonsterNumber)
            .Concat(coreBosses)
            .Distinct()
            .Select(number =>
            {
                var reward = coins.FirstOrDefault(r => r.MonsterNumber == number);
                var dropsCores = coreBosses.Contains(number);
                return new GuideBookBossReward(
                    number,
                    MonsterName(context, number),
                    reward?.WCoinC ?? 0,
                    reward?.WCoinP ?? 0,
                    reward?.GoblinPoints ?? 0,
                    dropsCores ? cores!.BossDropChance : 0,
                    dropsCores ? CoresPerBossAndDay : 0);
            })
            .Take(MaximumBossRewards)
            .ToList();
    }

    private static string Number(int value) => value.ToString("N0", CultureInfo.InvariantCulture);

    private static string Percent(double value) => value.ToString("0.######", CultureInfo.InvariantCulture);

    private static string MonsterName(IGameContext context, short number)
        => context.Configuration.Monsters.FirstOrDefault(monster => monster.Number == number)?.Designation.ValueInNeutralLanguage
           ?? number.ToString(CultureInfo.InvariantCulture);

    /// <summary>Determines whether the plugin is active.</summary>
    /// <param name="context">The game context.</param>
    /// <param name="plugInType">The plugin's type.</param>
    /// <returns><c>true</c> if it is active.</returns>
    internal static bool IsActive(IGameContext context, Type plugInType)
        => context.Configuration.PlugInConfigurations.FirstOrDefault(c => c.TypeId == plugInType.GUID) is { IsActive: true };

    /// <summary>
    /// Reads the configuration of another plugin from its stored configuration, because that plugin isn't
    /// necessarily a feature plugin which could be asked directly.
    /// </summary>
    /// <typeparam name="T">The configuration's type.</typeparam>
    /// <param name="context">The game context.</param>
    /// <param name="plugInType">The plugin's type.</param>
    /// <returns>The configuration, or <c>null</c> if there is none or it isn't readable.</returns>
    internal static T? ReadConfiguration<T>(IGameContext context, Type plugInType)
        where T : class
    {
        var entity = context.Configuration.PlugInConfigurations.FirstOrDefault(c => c.TypeId == plugInType.GUID);
        if (entity is null)
        {
            return null;
        }

        try
        {
            return entity.GetConfiguration<T>(context.PlugInManager.CustomConfigReferenceHandler);
        }
        catch (JsonException ex)
        {
            context.LoggerFactory.CreateLogger(typeof(GuideBookValues)).LogWarning(ex, "The configuration of {plugIn} is not readable.", plugInType.Name);
            return null;
        }
    }
}
