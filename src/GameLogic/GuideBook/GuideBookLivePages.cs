// <copyright file="GuideBookLivePages.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.GuideBook;

using System.Globalization;
using MUnique.OpenMU.GameLogic.ItemBonuses;
using MUnique.OpenMU.GameLogic.Offline;
using MUnique.OpenMU.GameLogic.PlugIns.InvasionEvents;

/// <summary>
/// Writes the live settings which take whole lines into the published pages: "{offline.prices}",
/// "{offline.duration}" and "{offline.protection}" of the offline leveling, "{items.attributes}" of the items with
/// extra attributes, "{invasion.golden}", "{invasion.reddragon}" and "{invasion.whitewizard}" of the invasions. The client fills in the one-word "{key}" values itself (<see cref="GuideBookValues"/>).
/// </summary>
public static class GuideBookLivePages
{
    /// <summary>The most bytes an item list takes, so the guide still fits its packet.</summary>
    internal const int MaximumItemListLength = 4000;

    // What the guide's JSON takes besides the pages' texts: names, keys, the escaping of new lines.
    private const int LengthOfTheRest = 8000;

    private const string OfflinePrices = "{offline.prices}";
    private const string OfflineDuration = "{offline.duration}";
    private const string OfflineProtection = "{offline.protection}";
    private const string ItemAttributes = "{items.attributes}";
    private const string GoldenInvasion = "{invasion.golden}";
    private const string RedDragonInvasion = "{invasion.reddragon}";
    private const string WhiteWizardInvasion = "{invasion.whitewizard}";

    /// <summary>Gets the pages with the live settings written in; the stored pages stay as they are.</summary>
    /// <param name="context">The game context.</param>
    /// <param name="pages">The pages.</param>
    /// <returns>The pages to send.</returns>
    public static IReadOnlyList<GuideBookPage> Expand(IGameContext context, ICollection<GuideBookPage> pages)
    {
        var replacements = new Lazy<IReadOnlyDictionary<string, string>>(() => Collect(context));

        // The client refuses a whole guide with a page or the guide too long: such a page stays as it is.
        var room = GuideBookContent.MaximumJsonBytes
            - Encoding.UTF8.GetByteCount(string.Concat(pages.Select(page => page.Id + page.CategoryId + page.Title + page.Body)))
            - LengthOfTheRest;
        var expanded = new List<GuideBookPage>(pages.Count);
        foreach (var page in pages)
        {
            if (!replacements.Value.Keys.Any(key => page.Body.Contains(key, StringComparison.Ordinal)))
            {
                expanded.Add(page);
                continue;
            }

            var body = replacements.Value.Aggregate(page.Body, (text, entry) => text.Replace(entry.Key, entry.Value, StringComparison.Ordinal));
            var bodyBytes = Encoding.UTF8.GetByteCount(body);
            var growth = bodyBytes - Encoding.UTF8.GetByteCount(page.Body);
            if (bodyBytes > GuideBookContent.MaximumBodyBytes || growth > room)
            {
                expanded.Add(page);
                continue;
            }

            room -= growth;
            expanded.Add(new GuideBookPage { Id = page.Id, CategoryId = page.CategoryId, Title = page.Title, Body = body });
        }

        return expanded;
    }

    private static IReadOnlyDictionary<string, string> Collect(IGameContext context)
    {
        var offline = OfflineLevelingFeaturePlugIn.GetSettings(context);
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [OfflinePrices] = Prices(offline),
            [OfflineDuration] = Duration(offline),
            [OfflineProtection] = Protection(offline),
            [ItemAttributes] = Items(context),
            [GoldenInvasion] = Invasion(context, typeof(GoldenInvasionPlugIn), InvasionConfigurationDefaults.Golden),
            [RedDragonInvasion] = Invasion(context, typeof(RedDragonInvasionPlugIn), InvasionConfigurationDefaults.RedDragon),
            [WhiteWizardInvasion] = Invasion(context, typeof(WhiteWizardInvasionPlugIn), InvasionConfigurationDefaults.WhiteWizard),
        };
    }

    private static string Prices(OfflineLevelingSettings? settings)
    {
        if (settings is null)
        {
            return "- **Zen**, as the MU Helper charges it while you play: **/offlevel** starts at once.";
        }

        var lines = new List<string>();
        if (settings.AllowZen)
        {
            lines.Add(settings.ZenPerHour > 0
                ? $"- **Zen**: {Number(settings.ZenPerHour)} per hour"
                : "- **Zen**: by your level, as the MU Helper charges it");
        }

        if (settings.AllowWCoinC)
        {
            lines.Add($"- **WCoinC**: {Number(settings.WCoinCPerHour)} per hour");
        }

        if (settings.AllowGoblinPoints)
        {
            lines.Add($"- **Goblin Points**: {Number(settings.GoblinPointsPerHour)} per hour");
        }

        return lines.Count == 0 ? "- Offline leveling can't be paid on this server." : string.Join('\n', lines);
    }

    private static string Duration(OfflineLevelingSettings? settings)
    {
        if (settings is null)
        {
            return "- It runs until you log in, or until you can't pay the MU Helper's zen.";
        }

        return "- **Until I stop (pay each hour)**: the first hour is paid at the start, then one every hour. The session ends when you can't pay the next hour.\n"
            + $"- **Prepay hours**: pay up to **{Number(settings.MaximumHours)}** hours at the start. The session ends when they are over.";
    }

    private static string Protection(OfflineLevelingSettings? settings)
    {
        if (settings is not { AllowProtection: true })
        {
            return "Not available on this server.";
        }

        var lines = new List<string>
        {
            "Players, their pets and summons can't damage your character, and it doesn't damage players. Monsters still can.",
            "It is paid every hour with the leveling; when an hour can't be paid, the protection ends and the leveling goes on.",
        };
        foreach (var currency in Enum.GetValues<OfflineLevelingCurrency>().Where(currency => OfflineLevelingCosts.IsAllowed(settings, currency)))
        {
            var price = OfflineLevelingCosts.ProtectionPerHour(settings, currency);
            lines.Add($"- With **{Name(currency)}**: {Number(price.Amount)} {Name(price.Currency)} per hour");
        }

        return string.Join('\n', lines);
    }

    private static string Items(IGameContext context)
    {
        var text = new StringBuilder();
        foreach (var definition in ItemBonusFeaturePlugIn.GetItems(context))
        {
            var lines = ItemBonusActions.Lines(definition);
            if (lines.Count == 0)
            {
                continue;
            }

            var item = new StringBuilder();
            item.Append("## ").Append(definition.Name.ValueInNeutralLanguage).Append('\n');
            foreach (var line in lines)
            {
                item.Append("- ").Append(line.Text);
                if (line.ByLevel.Count > 0)
                {
                    item.Append(", more at higher item levels");
                }

                item.Append('\n');
            }

            if (text.Length + item.Length > MaximumItemListLength)
            {
                text.Append("> More items have extra attributes: see their tooltip.\n");
                break;
            }

            text.Append(item).Append('\n');
        }

        return text.Length == 0 ? "> No item has extra attributes yet." : text.ToString().TrimEnd('\n');
    }

    // How long and how often the invasion comes, and its monsters with their maps.
    private static string Invasion(IGameContext context, Type plugInType, PeriodicInvasionConfiguration defaults)
    {
        if (!GuideBookValues.IsActive(context, plugInType))
        {
            return "> This invasion is off on this server.";
        }

        var configuration = GuideBookValues.ReadConfiguration<PeriodicInvasionConfiguration>(context, plugInType) ?? defaults;
        var text = new StringBuilder();
        text.Append("- Lasts **").Append(Duration(configuration.TaskDuration)).Append("**");
        if (configuration.Timetable.Count > 0)
        {
            text.Append(", ").Append(Number(configuration.Timetable.Count)).Append(configuration.Timetable.Count == 1 ? " time" : " times").Append(" a day");
        }

        text.Append('\n').Append("\n## Monsters\n");
        foreach (var spawn in configuration.Mobs)
        {
            var maps = spawn.MapIds.Select(map => MapName(context, map)).ToList();
            var where = maps.Count == 0 ? "with the invasion"
                : maps.Count == 1 ? maps[0]
                : spawn.IsSpawnOnAllMaps ? string.Join(", ", maps)
                : string.Join(", ", maps.Take(maps.Count - 1)) + " or " + maps[^1];
            text.Append("- **").Append(Number(spawn.Count)).Append(' ').Append(MonsterName(context, spawn.MonsterId)).Append("**: ").Append(where).Append('\n');
        }

        return text.ToString().TrimEnd('\n');
    }

    private static string Duration(TimeSpan duration)
        => duration.TotalHours >= 1 && duration.Minutes == 0
            ? Number((long)duration.TotalHours) + (duration.TotalHours == 1 ? " hour" : " hours")
            : Number((long)Math.Max(1, duration.TotalMinutes)) + " minutes";

    private static string MapName(IGameContext context, ushort number)
        => context.Configuration.Maps.FirstOrDefault(map => map.Number == number)?.Name.ValueInNeutralLanguage
           ?? number.ToString(CultureInfo.InvariantCulture);

    private static string MonsterName(IGameContext context, ushort number)
        => context.Configuration.Monsters.FirstOrDefault(monster => monster.Number == number)?.Designation.ValueInNeutralLanguage
           ?? number.ToString(CultureInfo.InvariantCulture);

    private static string Name(OfflineLevelingCurrency currency) => currency switch
    {
        OfflineLevelingCurrency.Zen => "Zen",
        OfflineLevelingCurrency.WCoinC => "WCoinC",
        _ => "Goblin Points",
    };

    private static string Number(long value) => value.ToString("N0", CultureInfo.InvariantCulture);
}
