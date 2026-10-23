// <copyright file="DiscordStatusFormatter.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Discord;

using System.Globalization;
using MUnique.OpenMU.Discord.Properties;

/// <summary>
/// Formats the status of the game servers, for the status message and the presence of the bot.
/// </summary>
public sealed class DiscordStatusFormatter
{
    /// <summary>
    /// The color of the status message, when at least one game server is online (green).
    /// </summary>
    internal const int OnlineColor = 0x2ECC71;

    /// <summary>
    /// The color of the status message, when no game server is online (red).
    /// </summary>
    internal const int OfflineColor = 0xE74C3C;

    private readonly CultureInfo _culture;

    /// <summary>
    /// Initializes a new instance of the <see cref="DiscordStatusFormatter"/> class.
    /// </summary>
    /// <param name="culture">The culture of the texts.</param>
    public DiscordStatusFormatter(CultureInfo culture)
    {
        this._culture = culture;
    }

    /// <summary>
    /// Gets the title of the status, by which the bot finds its status message again.
    /// </summary>
    public string StatusTitle => this.Text(nameof(Resources.Status_Title));

    /// <summary>
    /// Creates the status message.
    /// </summary>
    /// <param name="servers">The status of the game servers.</param>
    /// <param name="utcNow">The current time, which is shown as time of the last update.</param>
    /// <param name="economy">The jewel totals, when available.</param>
    /// <param name="timers">The current event countdowns.</param>
    /// <param name="statisticsUnavailable">Whether statistics could not be read.</param>
    /// <returns>The status message.</returns>
    public DiscordEmbed CreateStatus(IReadOnlyList<GameServerStatus> servers, DateTime utcNow, DiscordEconomyStatus? economy = null, IReadOnlyList<DiscordEventTimer>? timers = null, bool statisticsUnavailable = false)
    {
        var description = servers.Count == 0
            ? this.Text(nameof(Resources.Status_NoServers))
            : string.Join('\n', servers.Select(server => server.IsOnline
                ? this.Text(nameof(Resources.Status_Online), DiscordMessageFormatter.Escape(server.Name), server.CurrentPlayers, server.MaximumPlayers)
                : this.Text(nameof(Resources.Status_Offline), DiscordMessageFormatter.Escape(server.Name))));
        if (economy is not null)
        {
            description += "\n\n**" + this.Text("Status_ServerJewels") + "**\n" + this.FormatJewels(economy.Produced);
            description += "\n\n**" + this.Text("Status_PlayerJewels") + "**\n" + this.FormatJewels(economy.Held);
        }
        else if (statisticsUnavailable)
        {
            description += "\n\n" + this.Text("Status_StatisticsUnavailable");
        }

        if (timers is { Count: > 0 })
        {
            description += "\n\n**" + this.Text("Status_EventTimers") + "**\n";
            foreach (var timer in timers)
            {
                var line = this.FormatTimer(timer, utcNow) + "\n";
                if (description.Length + line.Length > DiscordEmbed.MaximumDescriptionLength)
                {
                    break;
                }

                description += line;
            }
        }

        var color = servers.Any(server => server.IsOnline) ? OnlineColor : OfflineColor;
        return new DiscordEmbed(this.StatusTitle, description.TrimEnd(), color, utcNow, economy is null ? null : this.Text("Status_TrackingNote")).Limit();
    }

    /// <summary>
    /// Creates the text of the presence of the bot.
    /// </summary>
    /// <param name="servers">The status of the game servers.</param>
    /// <returns>The text.</returns>
    public string CreatePresence(IReadOnlyList<GameServerStatus> servers)
    {
        return this.Text(nameof(Resources.Presence), servers.Where(server => server.IsOnline).Sum(server => server.CurrentPlayers));
    }

    private string FormatJewels(IReadOnlyList<MUnique.OpenMU.Persistence.JewelAmount> amounts)
        => string.Join('\n', amounts.Take(32).Select(amount => $"{this.JewelName(amount.ItemCode)}: {amount.Amount.ToString("N0", this._culture)}"));

    private string JewelName(int code)
        => Resources.ResourceManager.GetString($"Status_Jewel_{code}", this._culture) ?? code.ToString(this._culture);

    private string FormatTimer(DiscordEventTimer timer, DateTime utcNow)
    {
        var name = Resources.ResourceManager.GetString($"Status_Timer_{timer.Name}", this._culture) ?? timer.Name;
        var state = this.Text("Status_EventActive");
        if (timer.DeadlineUtc is { } deadline && timer.Phase != DiscordEventPhase.Active)
        {
            var seconds = Math.Max(0, (long)Math.Ceiling((deadline - utcNow).TotalSeconds));
            var countdown = $"{seconds / 3600:00}:{(seconds / 60) % 60:00}:{seconds % 60:00}";
            state = this.Text(
                timer.Phase == DiscordEventPhase.Entrance ? "Status_EventCloses" : "Status_EventOpens",
                countdown,
                DiscordMessageFormatter.RelativeTime(deadline));
        }

        return this.Text("Status_TimerLine", DiscordMessageFormatter.Escape(name), timer.ServerId, state);
    }

    private string Text(string resourceKey, params object[] args)
    {
        var text = Resources.ResourceManager.GetString(resourceKey, this._culture) ?? resourceKey;
        return string.Format(this._culture, text, args);
    }
}
