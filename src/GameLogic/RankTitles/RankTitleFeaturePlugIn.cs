// <copyright file="RankTitleFeaturePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.RankTitles;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.GameLogic.Views.Inventory;
using MUnique.OpenMU.PlugIns;
using Nito.AsyncEx;

/// <summary>Enables configured titles and sends the owner's title on entering the world.</summary>
[PlugIn]
[Display(Name = nameof(RankTitleResources.Name), Description = nameof(RankTitleResources.Description), ResourceType = typeof(RankTitleResources))]
[Guid("99681727-502e-4513-9073-fc2982264c1a")]
public sealed class RankTitleFeaturePlugIn : IFeaturePlugIn, IPlayerStateChangedPlugIn,
    IAttackableGotKilledPlugIn, IPeriodicTaskPlugIn,
    ISupportCustomConfiguration<RankTitleSettings>, ISupportDefaultCustomConfiguration, IDisabledByDefault
{
    /// <summary>The VIP badge identifier shared with both client catalogs.</summary>
    public const int VipRank = 1;

    /// <summary>The duel standings badge identifier shared with both client catalogs.</summary>
    public const int PvpKingRank = 7;

    /// <summary>The weekly kill standings badge identifier shared with both client catalogs.</summary>
    public const int DestroyerRank = 9;

    /// <summary>Keeps the complete JSON catalog inside one C2 packet.</summary>
    internal const int MaximumCatalogTitles = 128;

    /// <summary>Bounds persisted title ownership as the configured catalog evolves.</summary>
    internal const int MaximumOwnedTitles = 1024;

    /// <summary>Fits the client's 256-byte UTF-8 title name limit.</summary>
    internal const int MaximumTitleNameLength = 64;

    /// <summary>Limits database polling while keeping earned badges synchronized across game servers.</summary>
    private const int StandingsRefreshSeconds = 5;

    private readonly ConditionalWeakTable<IGameContext, Standings> _standings = new();

    /// <inheritdoc />
    public RankTitleSettings? Configuration { get; set; }

    /// <summary>Gets validated settings when the feature is enabled.</summary>
    /// <param name="context">The game context.</param>
    /// <returns>The active settings, or null when disabled or invalid.</returns>
    public static RankTitleSettings? Settings(IGameContext context)
    {
        if (context.FeaturePlugIns.GetPlugIn<RankTitleFeaturePlugIn>() is not { } feature)
        {
            return null;
        }

        var settings = feature.Configuration ??= new RankTitleSettings();
        return settings.IsValid() ? settings : null;
    }

    /// <summary>Gets Monday midnight in the server's time zone as a UTC instant.</summary>
    /// <param name="utcNow">The current UTC instant.</param>
    /// <param name="timeZone">The server time zone, or the local zone when omitted.</param>
    public static DateTime WeekStartUtc(DateTime utcNow, TimeZoneInfo? timeZone = null)
    {
        timeZone ??= TimeZoneInfo.Local;
        var local = TimeZoneInfo.ConvertTimeFromUtc(utcNow, timeZone);
        var monday = local.Date.AddDays(-(((int)local.DayOfWeek + 6) % 7));
        return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(monday, DateTimeKind.Unspecified), timeZone);
    }

    /// <inheritdoc />
    public object CreateDefaultConfig() => new RankTitleSettings();

    /// <inheritdoc />
    public async ValueTask PlayerStateChangedAsync(Player player, State previousState, State currentState)
    {
        if (currentState == PlayerState.EnteredWorld)
        {
            await this.RefreshLeadersAsync(player.GameContext).ConfigureAwait(false);
            await player.ForEachWorldObserverAsync<IRankTitleView>(view => view.AppearanceAsync(player), true).ConfigureAwait(false);
        }
    }

    /// <summary>Checks a current competitive entitlement without keeping obsolete weekly titles.</summary>
    /// <param name="player">The player whose eligibility is checked.</param>
    /// <param name="rank">The competitive title identifier.</param>
    public bool HasCompetitiveTitle(Player player, int rank)
    {
        var snapshot = this._standings.GetValue(player.GameContext, _ => new()).Snapshot;
        return rank == PvpKingRank ? snapshot.DuelLeader == player.Name
            : rank == DestroyerRank && snapshot.WeekStartUtc == WeekStartUtc(DateTime.UtcNow) && snapshot.KillLeader == player.Name;
    }

    /// <summary>Refreshes shared database standings, including offline players and other game servers.</summary>
    /// <param name="context">The game context whose standings are refreshed.</param>
    /// <param name="force">Whether to refresh immediately after a score changes.</param>
    public async ValueTask RefreshLeadersAsync(IGameContext context, bool force = false)
    {
        var standings = this._standings.GetValue(context, _ => new());
        using var l = await standings.Lock.LockAsync().ConfigureAwait(false);
        var now = DateTime.UtcNow;
        var week = WeekStartUtc(now);
        if (!force && now < standings.NextRefreshUtc)
        {
            return;
        }

        standings.NextRefreshUtc = now.AddSeconds(StandingsRefreshSeconds);
        try
        {
            using var persistence = context.PersistenceContextProvider.CreateNewPlayerContext(context.Configuration);
            var (duel, kills) = await persistence.GetCompetitiveRankLeadersAsync(week).ConfigureAwait(false);
            standings.Snapshot = new(week, duel, kills);
        }
        catch (Exception exception)
        {
            context.LoggerFactory.CreateLogger<RankTitleFeaturePlugIn>().LogError(exception, "Refreshing competitive rank standings failed; the next periodic check will retry.");
        }
    }

    /// <inheritdoc />
    public async ValueTask AttackableGotKilledAsync(IAttackable killed, IAttacker? killer)
    {
        if (killed is Player victim && RankTitleCombat.PlayerKiller(killer) is { } player
            && RankTitleCombat.Qualifies(player, victim) && Settings(player.GameContext) is not null)
        {
            await RankTitleCombat.RecordKillAsync(player).ConfigureAwait(false);
            await RankTitleCombat.SendDeathMailAsync(victim, player).ConfigureAwait(false);
            await this.RefreshLeadersAsync(player.GameContext, true).ConfigureAwait(false);
            await this.BroadcastAsync(player.GameContext).ConfigureAwait(false);
        }
    }

    /// <summary>Records a completed duel once; cancelled duels never call this method.</summary>
    /// <param name="winner">The winning player.</param>
    /// <param name="loser">The losing player.</param>
    public async ValueTask RecordDuelWinAsync(Player winner, Player loser)
    {
        if (RankTitleCombat.Qualifies(winner, loser) && Settings(winner.GameContext) is not null)
        {
            await RankTitleCombat.RecordDuelWinAsync(winner).ConfigureAwait(false);
            await this.RefreshLeadersAsync(winner.GameContext, true).ConfigureAwait(false);
            await this.BroadcastAsync(winner.GameContext).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async ValueTask ExecuteTaskAsync(GameContext gameContext)
    {
        var standings = this._standings.GetValue(gameContext, _ => new());
        if (DateTime.UtcNow < standings.NextRefreshUtc)
        {
            return;
        }

        await this.RefreshLeadersAsync(gameContext).ConfigureAwait(false);
        await this.BroadcastAsync(gameContext).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void ForceStart()
    {
        foreach (var entry in this._standings)
        {
            entry.Value.NextRefreshUtc = default;
        }
    }

    private async ValueTask BroadcastAsync(IGameContext context)
    {
        var standings = this._standings.GetValue(context, _ => new());
        foreach (var player in await context.GetPlayersAsync().ConfigureAwait(false))
        {
            if (player.PlayerState.CurrentState == PlayerState.EnteredWorld)
            {
                var shown = standings.Shown.GetValue(player, _ => new());
                var rank = RankTitleRules.Active(player);
                if (shown.Change(rank))
                {
                    await player.ForEachWorldObserverAsync<IRankTitleView>(view => view.AppearanceAsync(player), true).ConfigureAwait(false);
                }
            }
        }
    }

    private sealed class Standings
    {
        private volatile Snapshot _snapshot = new(default, null, null);

        public AsyncLock Lock { get; } = new();

        public ConditionalWeakTable<Player, ShownRank> Shown { get; } = new();

        public DateTime NextRefreshUtc { get; set; }

        public Snapshot Snapshot
        {
            get => this._snapshot;
            set => this._snapshot = value;
        }
    }

    private sealed record Snapshot(DateTime WeekStartUtc, string? DuelLeader, string? KillLeader);

    private sealed class ShownRank
    {
        private int _rank = -1;

        public bool Change(int rank) => Interlocked.Exchange(ref this._rank, rank) != rank;
    }
}
