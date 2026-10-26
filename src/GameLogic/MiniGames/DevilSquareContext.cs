// <copyright file="DevilSquareContext.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.MiniGames;

using System.Collections.Concurrent;
using System.Threading;
using MUnique.OpenMU.Interfaces;
using Nito.Disposables.Internals;

/// <summary>
/// The context of a devil square game.
/// </summary>
public sealed class DevilSquareContext : MiniGameContext
{
    private readonly ConcurrentDictionary<string, PlayerGameState> _gameStates = new();

    private IReadOnlyCollection<(string Name, int Score, int BonusMoney, int BonusExp)>? _highScoreTable;

    /// <summary>
    /// Initializes a new instance of the <see cref="DevilSquareContext"/> class.
    /// </summary>
    /// <param name="key">The key of this context.</param>
    /// <param name="definition">The definition of the mini game.</param>
    /// <param name="gameContext">The game context, to which this game belongs.</param>
    /// <param name="mapInitializer">The map initializer, which is used when the event starts.</param>
    public DevilSquareContext(MiniGameMapKey key, MiniGameDefinition definition, IGameContext gameContext, IMapInitializer mapInitializer)
        : base(key, definition, gameContext, mapInitializer)
    {
    }

    /// <summary>Gets live progress using the configured wave schedule and recorded player scores.</summary>
    /// <param name="player">The participant viewing the journal.</param>
    /// <returns>The current event progress.</returns>
    public DevilSquareProgress GetProgress(Player player)
    {
        var elapsed = this.ElapsedGameTime;
        var scores = this._gameStates.Values.OrderByDescending(s => s.Score).ThenBy(s => s.Player.Name, StringComparer.Ordinal).ToList();
        var mine = scores.FindIndex(s => s.Player.Name == player.Name);
        var waves = this.Definition.SpawnWaves.OrderBy(w => w.StartTime).Take(8)
            .Select(w => new DevilSquareProgress.Wave(
                w.WaveNumber,
                this.IsSpawnWaveActive(w.WaveNumber) ? (byte)1 : elapsed >= w.EndTime ? (byte)2 : (byte)0,
                w.StartTime - elapsed,
                w.EndTime - elapsed)).ToArray();
        return new(
            this.Definition.GameLevel,
            this.Definition.GameDuration - elapsed,
            mine < 0 ? 0 : scores[mine].Score,
            (byte)(mine + 1),
            (byte)scores.Count,
            waves);
    }

    /// <inheritdoc />
    protected override void OnMonsterDied(object? sender, DeathInformation e)
    {
        base.OnMonsterDied(sender, e);

        if (this._gameStates.TryGetValue(e.KillerName, out var state))
        {
            state.AddScore(this.Definition.GameLevel);

            // todo add money? -> in the original servers, in DS drops no money!
        }
    }

    /// <inheritdoc />
    protected override async ValueTask OnGameStartAsync(ICollection<Player> players)
    {
        foreach (var player in players)
        {
            this._gameStates.TryAdd(player.Name, new PlayerGameState(player));
        }

        await base.OnGameStartAsync(players).ConfigureAwait(false);
        await this.ForEachPlayerAsync(player => player.InvokeViewPlugInAsync<IUpdateMiniGameStateViewPlugIn>(view => view.UpdateStateAsync(this.Definition.Type, this.State)).AsTask()).ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override async ValueTask OnObjectAddedToMapAsync((GameMap Map, ILocateable Object) args)
    {
        await base.OnObjectAddedToMapAsync(args).ConfigureAwait(false);
        if (args.Object is Player player && ReferenceEquals(player.CurrentMiniGame, this) && this.State == MiniGameState.Playing)
        {
            await player.InvokeViewPlugInAsync<IUpdateMiniGameStateViewPlugIn>(view => view.UpdateStateAsync(this.Definition.Type, this.State)).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    protected override async ValueTask GameEndedAsync(ICollection<Player> finishers)
    {
        var sortedFinishers = finishers
            .Select(this.GetGameStateOfFinisher)
            .WhereNotNull()
            .OrderBy(state => state.Score)
            .ToList();

        var scoreList = new List<(string Name, int Score, int BonusMoney, int BonusExp)>();
        int rank = 0;
        foreach (var state in sortedFinishers)
        {
            rank++;
            state.Rank = rank;
            var (bonusScore, givenMoney) = await this.GiveRewardsAndGetBonusScoreAsync(state.Player, rank).ConfigureAwait(false);
            state.AddScore(bonusScore);
            scoreList.Add((
                state.Player.Name,
                state.Score,
                givenMoney,
                this.Definition.Rewards.FirstOrDefault(r => r.RewardType == MiniGameRewardType.Experience && (r.Rank is null || r.Rank == rank))?.RewardAmount ?? 0));
        }

        this._highScoreTable = scoreList.AsReadOnly();

        await this.SaveRankingAsync(sortedFinishers.Select(state => (state.Rank, state.Player.SelectedCharacter!, state.Score))).ConfigureAwait(false);
        await base.GameEndedAsync(finishers).ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override async ValueTask ShowScoreAsync(Player player)
    {
        if (this._highScoreTable is { } table
            && this._gameStates.TryGetValue(player.Name, out var state))
        {
            await player.InvokeViewPlugInAsync<IMiniGameScoreTableViewPlugin>(p => p.ShowScoreTableAsync((byte)state.Rank, table)).ConfigureAwait(false);
        }
    }

    private PlayerGameState? GetGameStateOfFinisher(Player finisher)
    {
        if (!this._gameStates.TryGetValue(finisher.Name, out var state))
        {
            return null;
        }

        // The score is kept by the character name, so it's kept when the player rejoined the game
        // after it lost its connection. The rewards must be given to its current player object, though.
        state.Player = finisher;
        return state;
    }

    private sealed class PlayerGameState
    {
        private int _score;

        public PlayerGameState(Player player)
        {
            if (player.SelectedCharacter?.CharacterClass is null)
            {
                throw new InvalidOperationException($"The player '{player}' is in the wrong state");
            }

            this.Player = player;
        }

        /// <summary>
        /// Gets or sets the player of the character. It's replaced by the new player
        /// object of the same character, when it rejoined the game after it lost its connection.
        /// </summary>
        public Player Player { get; set; }

        public int Score => this._score;

        public int Rank { get; set; }

        public void AddScore(int value)
        {
            Interlocked.Add(ref this._score, value);
        }
    }
}