// <copyright file="DuelRoom.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic;

using System.Threading;
using MUnique.OpenMU.GameLogic.DuelBetting;
using MUnique.OpenMU.GameLogic.Views;
using MUnique.OpenMU.GameLogic.Views.Duel;
using MUnique.OpenMU.Interfaces;
using MUnique.OpenMU.Persistence;
using Nito.AsyncEx;

/// <summary>
/// The state of the duel.
/// </summary>
public enum DuelState
{
    /// <summary>
    /// Duel state is undefined.
    /// </summary>
    Undefined,

    /// <summary>
    /// A duel was requested.
    /// </summary>
    DuelRequested,

    /// <summary>
    /// Duel request was refused.
    /// </summary>
    DuelRefused,

    /// <summary>
    /// Duel failed to start.
    /// </summary>
    DuelStartFailed,

    /// <summary>
    /// Duel request was accepted.
    /// </summary>
    DuelAccepted,

    /// <summary>
    /// Duel has started.
    /// </summary>
    DuelStarted,

    /// <summary>
    /// Dual was cancelled.
    /// </summary>
    DuelCancelled,

    /// <summary>
    /// Duel has finished.
    /// </summary>
    DuelFinished,
}

/// <summary>
/// A class that manages a duel between two players.
/// </summary>
public sealed class DuelRoom : AsyncDisposable
{
    /// <summary>The seconds of the countdown before combat starts, when the duel takes no bets.</summary>
    internal const int CombatCountdownSeconds = 5;

    private static readonly TimeSpan ArenaLoadTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan ArenaLoadPollInterval = TimeSpan.FromMilliseconds(100);

    private readonly AsyncLock _spectatorLock = new();

    private CancellationTokenSource? _cts = new();
    private byte _scoreRequester;
    private byte _scoreOpponent;
    private int _maximumScore;
    private int _maximumSpectators;

    /// <summary>
    /// Initializes a new instance of the <see cref="DuelRoom" /> class.
    /// </summary>
    /// <param name="area">The duel area.</param>
    /// <param name="requester">The requester.</param>
    /// <param name="opponent">The opponent.</param>
    public DuelRoom(DuelArea area, Player requester, Player opponent)
    {
        this.Area = area;
        this.Index = area.Index;
        this.Requester = requester;
        this.Opponent = opponent;
        this.CreatedAt = DateTime.UtcNow;

        this._maximumScore = requester.GameContext.Configuration.DuelConfiguration?.MaximumScore ?? 10;
        this._maximumSpectators = requester.GameContext.Configuration.DuelConfiguration?.MaximumSpectatorsPerDuelRoom ?? 10;
    }

    /// <summary>
    /// Gets the area of the duel.
    /// </summary>
    public DuelArea Area { get; }

    /// <summary>
    /// Gets the index of the area of the duel.
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// Gets or sets the <see cref="DateTime"/> of the start of the duel.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets the player that requested the duel.
    /// </summary>
    public Player Requester { get; }

    /// <summary>
    /// Gets the player that accepted the duel.
    /// </summary>
    public Player Opponent { get; }

    /// <summary>
    /// Gets or sets the score of the player that requested the duel.
    /// </summary>
    public byte ScoreRequester
    {
        get => this._scoreRequester;
        set
        {
            this._scoreRequester = value;
            if (value >= this._maximumScore)
            {
                this.State = DuelState.DuelFinished;
            }
        }
    }

    /// <summary>
    /// Gets or sets the score of the player that accepted the duel.
    /// </summary>
    public byte ScoreOpponent
    {
        get => this._scoreOpponent;
        set
        {
            this._scoreOpponent = value;
            if (value >= this._maximumScore)
            {
                this.State = DuelState.DuelFinished;
            }
        }
    }

    // public bool IsAccepted { get; set; }

    // public bool IsFinished { get; set; }

    /// <summary>
    /// Gets or sets the state of the duel.
    /// </summary>
    public DuelState State { get; set; }

    /// <summary>
    /// Gets a lock object used to update the score of the duel.
    /// </summary>
    public AsyncLock Lock { get; } = new();

    /// <summary>
    /// Gets the duel room spectators list.
    /// </summary>
    public List<Player> Spectators { get; } = new();

    /// <summary>
    /// Gets all the players taking part in the duel, either as duelists or spectators.
    /// </summary>
    public IEnumerable<Player> AllPlayers
    {
        get
        {
            yield return this.Requester;

            yield return this.Opponent;

            for (var index = this.Spectators.Count - 1; index >= 0; index--)
            {
                var spectator = this.Spectators[index];
                yield return spectator;
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether the duel room still has spectator slots.
    /// </summary>
    public bool IsOpen => this.Spectators.Count < this._maximumSpectators;

    /// <summary>Gets the winning score for this room.</summary>
    internal int WinningScore => this._maximumScore;

    /// <summary>Gets or sets a value indicating whether the challenger opted into public betting.</summary>
    internal bool AllowBetting { get; set; }

    /// <summary>Gets or sets the public countdown presented on the invitation.</summary>
    internal int PublicCountdownSeconds { get; set; }

    /// <summary>Gets the public pool opened after both fighters enter the arena.</summary>
    internal DuelBetRoom? Betting { get; private set; }

    /// <summary>Gets a value indicating whether the room is preparing to start combat.</summary>
    internal bool CanTakeBets => this.State == DuelState.DuelAccepted && this._cts is { IsCancellationRequested: false };

    /// <summary>
    /// Gets a value indicating whether the player is participating in the duel as a duelist.
    /// </summary>
    /// <param name="player">The player.</param>
    public bool IsDuelist(Player player)
    {
        return this.Requester == player || this.Opponent == player;
    }

    /// <summary>
    /// Removes the spectator from the room.
    /// </summary>
    /// <param name="spectator">The spectator which should be removed.</param>
    public async ValueTask RemoveSpectatorAsync(Player spectator)
    {
        Player[] remainingSpectators;
        using (await this._spectatorLock.LockAsync().ConfigureAwait(false))
        {
            if (!this.Spectators.Remove(spectator))
            {
                return;
            }

            if (spectator.DuelRoom == this)
            {
                spectator.DuelRoom = null;
            }

            remainingSpectators = this.Spectators.ToArray();
        }

        foreach (var player in remainingSpectators)
        {
            await player.InvokeViewPlugInAsync<IDuelSpectatorRemovedPlugIn>(p => p.SpectatorRemovedAsync(spectator)).ConfigureAwait(false);
        }

        await spectator.InvokeViewPlugInAsync<IDuelEndedPlugIn>(p => p.DuelEndedAsync()).ConfigureAwait(false);
        await spectator.RemoveInvisibleEffectAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Starts the duel.
    /// </summary>
    /// <returns>A <see cref="Task"/>.</returns>
    /// <exception cref="InvalidOperationException">Occurs when one of the players has left the duel map.</exception>
    public async Task RunDuelAsync()
    {
        try
        {
            var cancellationToken = this._cts?.Token ?? default;

            await this.WaitForArenaAsync(cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            this.State = DuelState.DuelAccepted;

            await this.CountDownAsync(cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            await this.Requester.InvokeViewPlugInAsync<IShowDuelRequestResultPlugIn>(p => p.ShowDuelRequestResultAsync(DuelStartResult.Success, this.Opponent)).ConfigureAwait(false);
            await this.Opponent.InvokeViewPlugInAsync<IShowDuelRequestResultPlugIn>(p => p.ShowDuelRequestResultAsync(DuelStartResult.Success, this.Requester)).ConfigureAwait(false);

            await this.Requester.InvokeViewPlugInAsync<IShowDuelScoreUpdatePlugIn>(p => p.UpdateScoreAsync(this)).ConfigureAwait(false);
            await this.Opponent.InvokeViewPlugInAsync<IShowDuelScoreUpdatePlugIn>(p => p.UpdateScoreAsync(this)).ConfigureAwait(false);

            await this.Requester.InvokeViewPlugInAsync<IInitializeDuelPlugIn>(p => p.InitializeDuelAsync(this)).ConfigureAwait(false);
            await this.Opponent.InvokeViewPlugInAsync<IInitializeDuelPlugIn>(p => p.InitializeDuelAsync(this)).ConfigureAwait(false);

            this.State = DuelState.DuelStarted;

            while (this.State is not (DuelState.DuelFinished or DuelState.DuelCancelled))
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
                if (this.Betting is { } betting)
                {
                    await betting.RenewAsync().ConfigureAwait(false);
                }

                await this.SendCurrentStateToAllPlayersAsync().ConfigureAwait(false);
            }

            if (this.State is DuelState.DuelFinished)
            {
                await this.FinishDuelAsync().ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            if (ex is not OperationCanceledException)
            {
                this.Requester.Logger.LogWarning(ex, "Duel {Room} stopped unexpectedly", this.Index);
            }

            if (this.State is not DuelState.DuelFinished)
            {
                this.State = DuelState.DuelCancelled;
                await this.SettleBetsAsync(null).ConfigureAwait(false);
                await this.StopDuelAsync().ConfigureAwait(false);
            }
            else
            {
                await this.SettleWinningBetsAsync().ConfigureAwait(false);
            }
        }
        finally
        {
            await this.DisposeAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Cancels and stops the duel.
    /// </summary>
    /// <returns>A <see cref="ValueTask"/>.</returns>
    public async ValueTask CancelDuelAsync()
    {
        if (this._cts is { } cts)
        {
            await cts.CancelAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Gets the spawn gate of the player.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns>The gate where the player is teleported to.</returns>
    public ExitGate? GetSpawnGate(Player player)
    {
        if (this.Opponent == player)
        {
            return this.Area.SecondPlayerGate;
        }

        if (this.Requester == player)
        {
            return this.Area.FirstPlayerGate;
        }

        return this.Area.SpectatorsGate;
    }

    /// <summary>
    /// Tries to add the player as a spectator to the duel.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns>A <see cref="ValueTask"/> with the result.</returns>
    public async ValueTask<bool> TryAddSpectatorAsync(Player player)
    {
        if (this.Area.SpectatorsGate is not { } spectatorsGate)
        {
            return false;
        }

        Player[] spectators;
        using (await this._spectatorLock.LockAsync().ConfigureAwait(false))
        {
            if (!this.IsOpen || player.DuelRoom is not null || this.State != DuelState.DuelStarted)
            {
                return false;
            }

            spectators = this.Spectators.ToArray();
            this.Spectators.Add(player);
            player.DuelRoom = this;
        }

        try
        {
            await player.AddInvisibleEffectAsync().ConfigureAwait(false);
            await player.WarpToAsync(spectatorsGate).ConfigureAwait(false);
        }
        catch
        {
            await this.RemoveSpectatorAsync(player).ConfigureAwait(false);
            throw;
        }

        await player.InvokeViewPlugInAsync<IInitializeDuelPlugIn>(p => p.InitializeDuelAsync(this)).ConfigureAwait(false);
        await player.InvokeViewPlugInAsync<IDuelHealthUpdatePlugIn>(p => p.UpdateHealthAsync(this)).ConfigureAwait(false);
        await player.InvokeViewPlugInAsync<IShowDuelScoreUpdatePlugIn>(p => p.UpdateScoreAsync(this)).ConfigureAwait(false);

        // send spectator list to this player
        await player.InvokeViewPlugInAsync<IDuelSpectatorListUpdatePlugIn>(p => p.UpdateSpectatorListAsync(spectators)).ConfigureAwait(false);

        // send spectator added to all spectators of the same room
        for (int i = spectators.Length - 1; i >= 0; i--)
        {
            if (spectators[i] is { } spectator)
            {
                await spectator.InvokeViewPlugInAsync<IDuelSpectatorAddedPlugIn>(p => p.SpectatorAddedAsync(player)).ConfigureAwait(false);
            }
        }

        return true;
    }

    /// <summary>
    /// Resets and disposes of the duel room.
    /// </summary>
    /// <param name="startResult">The resuls of the duel start request.</param>
    /// <returns>A <see cref="ValueTask"/>.</returns>
    public async ValueTask ResetAndDisposeAsync(DuelStartResult startResult)
    {
        this.Requester.DuelRoom = null;
        this.Opponent.DuelRoom = null;
        await this.Requester.GameContext.DuelRoomManager.GiveBackDuelRoomAsync(this).ConfigureAwait(false);
        if (startResult == DuelStartResult.Refused)
        {
            this.State = DuelState.DuelRefused;
            await this.Requester.InvokeViewPlugInAsync<IShowDuelRequestResultPlugIn>(p => p.ShowDuelRequestResultAsync(startResult, this.Opponent)).ConfigureAwait(false);
        }
        else if (startResult != DuelStartResult.Undefined)
        {
            this.State = DuelState.DuelStartFailed;
            await this.Requester.InvokeViewPlugInAsync<IDuelEndedPlugIn>(p => p.DuelEndedAsync()).ConfigureAwait(false);
            await this.Opponent.InvokeViewPlugInAsync<IDuelEndedPlugIn>(p => p.DuelEndedAsync()).ConfigureAwait(false);

            await this.Requester.InvokeViewPlugInAsync<IShowDuelRequestResultPlugIn>(p => p.ShowDuelRequestResultAsync(startResult, this.Opponent)).ConfigureAwait(false);
            await this.Opponent.InvokeViewPlugInAsync<IShowDuelRequestResultPlugIn>(p => p.ShowDuelRequestResultAsync(startResult, this.Requester)).ConfigureAwait(false);
        }
        else
        {
            await this.Requester.InvokeViewPlugInAsync<IDuelEndedPlugIn>(p => p.DuelEndedAsync()).ConfigureAwait(false);
            await this.Opponent.InvokeViewPlugInAsync<IDuelEndedPlugIn>(p => p.DuelEndedAsync()).ConfigureAwait(false);
        }

        await this.DisposeAsyncCore().ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore()
    {
        if (Interlocked.Exchange(ref this._cts, null) is not { } cts)
        {
            return;
        }

        await cts.CancelAsync().ConfigureAwait(false);

        if (this.State >= DuelState.DuelAccepted)
        {
            await this.MovePlayersToExitAsync().ConfigureAwait(false);

            await this.Requester.GameContext.DuelRoomManager.GiveBackDuelRoomAsync(this).ConfigureAwait(false);
        }

        foreach (var player in this.AllPlayers)
        {
            if (player.DuelRoom == this)
            {
                player.DuelRoom = null;
            }
        }

        cts.Dispose();

        await base.DisposeAsyncCore().ConfigureAwait(false);
    }

    private static bool IsInArena(Player player, ExitGate? gate)
        => player.Id != 0 && player.CurrentMap is { } map && map.Definition == gate?.Map
            && player.PlayerState.CurrentState == PlayerState.EnteredWorld;

    private async ValueTask WaitForArenaAsync(CancellationToken cancellationToken)
    {
        var elapsed = System.Diagnostics.Stopwatch.StartNew();
        while (!IsInArena(this.Requester, this.Area.FirstPlayerGate) || !IsInArena(this.Opponent, this.Area.SecondPlayerGate))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (elapsed.Elapsed >= ArenaLoadTimeout
                || (this.Requester.CurrentMap is { } requesterMap && requesterMap.Definition != this.Area.FirstPlayerGate?.Map)
                || (this.Opponent.CurrentMap is { } opponentMap && opponentMap.Definition != this.Area.SecondPlayerGate?.Map))
            {
                throw new InvalidOperationException("Both duelists must finish loading the arena before the countdown.");
            }

            await Task.Delay(ArenaLoadPollInterval, cancellationToken).ConfigureAwait(false);
        }
    }

    private async ValueTask CountDownAsync(CancellationToken cancellationToken)
    {
        var deadline = this.OpenBetting();
        if (this.Betting is not null)
        {
            _ = this.AnnounceBettingAsync(deadline);
        }

        var lastAnnounced = -1;
        while (deadline - DateTime.UtcNow is { Ticks: > 0 } remaining)
        {
            var seconds = (int)Math.Ceiling(remaining.TotalSeconds);
            if (seconds != lastAnnounced && (seconds <= CombatCountdownSeconds || seconds % CombatCountdownSeconds == 0))
            {
                lastAnnounced = seconds;
                await this.AllPlayers.ForEachAsync(p => p.ShowLocalizedGoldenMessageAsync(nameof(PlayerMessage.DuelBattleBeginsInSecondsFormat), seconds)).ConfigureAwait(false);
            }

            var next = deadline.AddSeconds(1 - seconds) - DateTime.UtcNow;
            if (next > TimeSpan.Zero)
            {
                await Task.Delay(next, cancellationToken).ConfigureAwait(false);
            }
        }

        if (this.Betting is { } betting)
        {
            await betting.CloseAsync().ConfigureAwait(false);
        }
    }

    private DateTime OpenBetting()
    {
        var settings = DuelBettingFeaturePlugIn.GetSettings(this.Requester.GameContext);
        var requester = this.Requester.Account?.GetId() ?? Guid.Empty;
        var opponent = this.Opponent.Account?.GetId() ?? Guid.Empty;
        if (!this.AllowBetting || settings is null || requester == Guid.Empty || opponent == Guid.Empty || requester == opponent)
        {
            return DateTime.UtcNow.AddSeconds(CombatCountdownSeconds);
        }

        var deadline = DateTime.UtcNow.AddSeconds(this.PublicCountdownSeconds > 0 ? this.PublicCountdownSeconds : settings.BettingSeconds);
        this.Betting = new DuelBetRoom(this, new DuelBetBook(requester, opponent, deadline, settings));
        return deadline;
    }

    private async Task AnnounceBettingAsync(DateTime deadline)
    {
        try
        {
            var seconds = (int)Math.Ceiling((deadline - DateTime.UtcNow).TotalSeconds);
            if (seconds > 0 && this.CanTakeBets)
            {
                await this.Requester.GameContext.ShowGlobalLocalizedMessageAsync(
                    MessageType.BlueNormal, "DuelBetting_Announce", this.Requester.Name, this.Opponent.Name, seconds).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            this.Requester.Logger.LogWarning(ex, "Could not announce betting for duel {Room}", this.Index);
        }
    }

    private async ValueTask SettleBetsAsync(DuelBetSide? winner)
    {
        if (this.Betting is not { } betting)
        {
            return;
        }

        try
        {
            await betting.SettleAsync(winner).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            this.Requester.Logger.LogError(ex, "Could not settle duel {Match}; its stakes become refundable when the lease expires.", betting.MatchId);
        }
    }

    private ValueTask SettleWinningBetsAsync()
        => this.SettleBetsAsync(this.ScoreRequester > this.ScoreOpponent ? DuelBetSide.Requester : DuelBetSide.Opponent);

    private async ValueTask MovePlayersToExitAsync()
    {
        var duelConfig = this.Requester.GameContext.Configuration.DuelConfiguration;
        var exitGate = duelConfig?.Exit;
        var duelArenaMapNumber = this.Area.FirstPlayerGate?.Map?.Number;

        var players = this.AllPlayers
            .Where(p => p.CurrentMap?.MapId == duelArenaMapNumber);

        foreach (var player in players)
        {
            try
            {
                if (exitGate is not null && !this.IsDuelist(player))
                {
                    await player.WarpToAsync(exitGate).ConfigureAwait(false);
                }
                else
                {
                    await player.WarpToSafezoneAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                player.Logger.LogError(ex, "Unexpected error when moving player away from duel arena.");
            }
        }
    }

    private async ValueTask NotifyDuelFinishedAsync()
    {
        var winner = this.ScoreRequester > this.ScoreOpponent ? this.Requester : this.Opponent;
        var loser = this.Requester == winner ? this.Opponent : this.Requester;
        if (winner.GameContext.FeaturePlugIns.GetPlugIn<RankTitles.RankTitleFeaturePlugIn>() is { } ranks)
        {
            await ranks.RecordDuelWinAsync(winner, loser).ConfigureAwait(false);
        }

        await this.AllPlayers.ForEachAsync(player => player.InvokeViewPlugInAsync<IShowDuelScoreUpdatePlugIn>(p => p.UpdateScoreAsync(this))).ConfigureAwait(false);
        await this.AllPlayers.ForEachAsync(player => player.InvokeViewPlugInAsync<IDuelFinishedPlugIn>(p => p.DuelFinishedAsync(winner, loser))).ConfigureAwait(false);
    }

    private async ValueTask SendCurrentStateToAllPlayersAsync()
    {
        await this.AllPlayers.ForEachAsync(p => p.InvokeViewPlugInAsync<IShowDuelScoreUpdatePlugIn>(p => p.UpdateScoreAsync(this))).ConfigureAwait(false);

        for (var index = this.Spectators.Count - 1; index >= 0; index--)
        {
            var spectator = this.Spectators[index];
            await spectator.InvokeViewPlugInAsync<IDuelHealthUpdatePlugIn>(p => p.UpdateHealthAsync(this)).ConfigureAwait(false);
        }
    }

    private async ValueTask StopDuelAsync()
    {
        await this.Opponent.ResetPetBehaviorAsync().ConfigureAwait(false);
        await this.Requester.ResetPetBehaviorAsync().ConfigureAwait(false);

        await this.ResetAndDisposeAsync(DuelStartResult.Undefined).ConfigureAwait(false);
        await this.AllPlayers.ForEachAsync(player => player.InvokeViewPlugInAsync<IDuelEndedPlugIn>(p => p.DuelEndedAsync())).ConfigureAwait(false);
    }

    private async ValueTask FinishDuelAsync()
    {
        await this.SettleWinningBetsAsync().ConfigureAwait(false);
        await this.Opponent.ResetPetBehaviorAsync().ConfigureAwait(false);
        await this.Requester.ResetPetBehaviorAsync().ConfigureAwait(false);

        await this.NotifyDuelFinishedAsync().ConfigureAwait(false);
        await Task.Delay(10000, default).ConfigureAwait(false);
        await this.MovePlayersToExitAsync().ConfigureAwait(false);
    }
}
