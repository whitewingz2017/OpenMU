// <copyright file="UpcomingEvents.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.PeriodicTasks;

using System.Globalization;
using System.Reflection;
using System.Resources;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Determines the upcoming events, like mini games and invasions, from the schedules of the active plugins.
/// </summary>
public static class UpcomingEvents
{
    /// <summary>
    /// Gets the events which start within the specified time span, ordered by their start time.
    /// </summary>
    /// <param name="plugInManager">The plugin manager with the active plugins.</param>
    /// <param name="utcNow">The current time, in UTC.</param>
    /// <param name="serverTimeZone">The time zone of the server, in which the schedules are defined.</param>
    /// <param name="timeSpan">The time span from now, in which the events have to start.</param>
    /// <param name="culture">The culture of the names.</param>
    /// <param name="gameContext">Optional server context to filter schedules and include pending manual starts.</param>
    /// <returns>The names and start times of the events.</returns>
    public static IReadOnlyList<(string Name, DateTime StartsAtUtc)> Get(
        PlugInManager plugInManager,
        DateTime utcNow,
        TimeZoneInfo serverTimeZone,
        TimeSpan timeSpan,
        CultureInfo culture,
        IGameContext? gameContext = null)
    {
        var events = plugInManager.GetActivePlugInsOf<IPeriodicTaskPlugIn>()
            .OfType<IScheduledPlugIn>()
            .Select(plugIn => (Name: GetName(plugIn.GetType(), culture), StartsAtUtc: gameContext is null ? plugIn.GetNextStartUtc(utcNow, serverTimeZone) : plugIn.GetNextStartUtc(gameContext, utcNow)))
            .Where(e => e.StartsAtUtc is { } start && start - utcNow <= timeSpan)
            .Select(e => (e.Name, e.StartsAtUtc!.Value))
            .OrderBy(e => e.Value)
            .ToList();
        if (gameContext is not null)
        {
            foreach (var plugin in plugInManager.GetActivePlugInsOf<IPeriodicTaskPlugIn>().OfType<Crywolf.CrywolfPlugIn>())
            {
                if (plugin.Configuration is { } definition && gameContext is IGameServerContext server && server.Id == definition.GameServerId)
                {
                    var today = TimeZoneInfo.ConvertTimeFromUtc(utcNow, serverTimeZone).Date;
                    var next = Enumerable.Range(0, 8).Select(day => today.AddDays(day)).Where(date => definition.StartDays.Contains(date.DayOfWeek))
                        .SelectMany(date => definition.StartTimes.Select(time => DateTime.SpecifyKind(date.Add(time.ToTimeSpan()), DateTimeKind.Unspecified)))
                        .Where(local => !serverTimeZone.IsInvalidTime(local)).Select(local => TimeZoneInfo.ConvertTimeToUtc(local, serverTimeZone))
                        .Where(start => start > utcNow && start - utcNow <= timeSpan).OrderBy(start => start).Select(start => (DateTime?)start).FirstOrDefault();
                    if (next is { } start) events.Add((GetName(plugin.GetType(), culture), start));
                }
            }

            foreach (var plugin in plugInManager.GetActivePlugInsOf<IPeriodicTaskPlugIn>().OfType<CastleSiege.CastleSiegePlugIn>())
            {
                if (plugin.GetContext(gameContext) is { } siege && siege.Schedule.GetNextPeriod(CastleSiegeState.Start, utcNow) is { } period
                    && period.StartUtc - utcNow <= timeSpan)
                {
                    events.Add((GetName(plugin.GetType(), culture), period.StartUtc));
                }
            }
        }

        return events.OrderBy(e => e.Item2).ToList();
    }

    /// <summary>Gets active scheduled tasks and running mini games without creating event instances.</summary>
    /// <param name="context">The hosting server.</param>
    /// <param name="utcNow">The reference time.</param>
    /// <param name="culture">The culture for names.</param>
    /// <returns>Active event names and end times.</returns>
    public static IReadOnlyList<(string Name, DateTime EndsAtUtc)> GetActive(IGameContext context, DateTime utcNow, CultureInfo culture)
    {
        var events = context.PlugInManager.GetActivePlugInsOf<IPeriodicTaskPlugIn>().OfType<IScheduledPlugIn>()
            .Select(p => (Name: GetName(p.GetType(), culture), End: p.GetActiveUntilUtc(context)))
            .Where(e => e.End > utcNow).Select(e => (e.Name, EndsAtUtc: e.End!.Value)).ToList();
        foreach (var type in Enum.GetValues<MiniGameType>())
        {
            foreach (var game in context.MiniGames.GetRunningMiniGames(type).Where(g => g.IsEventRunning))
            {
                events.Add(($"{game.Definition.Name} {game.Definition.GameLevel}", utcNow.Add(game.ActiveTimeRemaining)));
            }
        }

        if (Crywolf.CrywolfPlugIn.GetContext(context) is { IsEventRunning: true } crywolf && crywolf.RemainingTime > TimeSpan.Zero)
            events.Add(($"Crywolf ({crywolf.State})", utcNow.Add(crywolf.RemainingTime)));
        if (Raklion.RaklionPlugIn.GetContext(context) is { IsEventRunning: true } raklion && raklion.RemainingTime > TimeSpan.Zero)
            events.Add(($"Raklion ({raklion.State})", utcNow.Add(raklion.RemainingTime)));
        foreach (var plugin in context.PlugInManager.GetActivePlugInsOf<IPeriodicTaskPlugIn>().OfType<CastleSiege.CastleSiegePlugIn>())
        {
            if (plugin.GetContext(context) is { IsEventRunning: true } siege)
                events.Add(("Castle Siege", utcNow.Add(siege.RemainingTime)));
        }

        return events.OrderBy(e => e.EndsAtUtc).ToList();
    }

    private static string GetName(Type plugInType, CultureInfo culture)
    {
        var display = plugInType.GetCustomAttribute<DisplayAttribute>();
        if (display?.Name is { } name
            && display.ResourceType?.GetProperty(nameof(PlugInResources.ResourceManager), BindingFlags.Public | BindingFlags.Static)?.GetValue(null) is ResourceManager resourceManager
            && resourceManager.GetString(name, culture) is { } localizedName)
        {
            return localizedName;
        }

        return display?.GetName() ?? plugInType.Name;
    }
}
