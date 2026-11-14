// <copyright file="ResonanceCoreDropPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions.Items;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.NPC;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Drops the Resonance Core: rarely from any monster, and from each boss at most once per day,
/// on a random kill of that day.
/// </summary>
/// <remarks>
/// The bosses which already dropped today are kept in memory and shared by all game servers of the process,
/// so a restart of the server allows each boss to drop it again on the same day.
/// </remarks>
[PlugIn]
[Display(Name = nameof(ResonanceCoreDropResources.Name), Description = nameof(ResonanceCoreDropResources.Description), ResourceType = typeof(ResonanceCoreDropResources))]
[Guid("ffa9f3cf-ef6c-438c-9d32-6980cdc32e04")]
public sealed class ResonanceCoreDropPlugIn : IAttackableGotKilledPlugIn, ISupportCustomConfiguration<ResonanceCoreDropConfiguration>, ISupportDefaultCustomConfiguration
{
    private const byte ResonanceCoreGroup = 14;

    private const short ResonanceCoreNumber = 252;

    private static readonly Dictionary<short, DateOnly> SharedBossDropDays = new();

    private readonly IRandomizer _randomizer;

    private readonly Func<DateOnly> _today;

    private readonly Dictionary<short, DateOnly> _bossDropDays;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResonanceCoreDropPlugIn"/> class.
    /// </summary>
    public ResonanceCoreDropPlugIn()
        : this(Rand.GetRandomizer(), () => DateOnly.FromDateTime(DateTime.Now), SharedBossDropDays)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResonanceCoreDropPlugIn"/> class.
    /// </summary>
    /// <param name="randomizer">The randomizer.</param>
    /// <param name="today">The function which returns the current day of the server.</param>
    /// <param name="bossDropDays">The days on which the bosses dropped their last core, by monster number.</param>
    internal ResonanceCoreDropPlugIn(IRandomizer randomizer, Func<DateOnly> today, Dictionary<short, DateOnly> bossDropDays)
    {
        this._randomizer = randomizer;
        this._today = today;
        this._bossDropDays = bossDropDays;
    }

    /// <inheritdoc />
    public ResonanceCoreDropConfiguration? Configuration { get; set; }

    /// <inheritdoc />
    public object CreateDefaultConfig() => new ResonanceCoreDropConfiguration();

    /// <inheritdoc />
    public async ValueTask AttackableGotKilledAsync(IAttackable killed, IAttacker? killer)
    {
        if (killed is not AttackableNpcBase monster
            || killed.IsSummonedMonster
            || (killer as Player ?? (killer as IPlayerSurrogate)?.Owner) is not { } player
            || monster.CurrentMap is not { } map
            || player.GameContext.Configuration.Items.FirstOrDefault(d => d.Group == ResonanceCoreGroup && d.Number == ResonanceCoreNumber) is not { } definition
            || !this.ShouldDrop(monster.Definition.Number))
        {
            return;
        }

        var item = new TemporaryItem { Definition = definition, Durability = 1 };
        var owners = player.Party?.PartyList.AsEnumerable() ?? player.GetAsEnumerable();
        await map.AddAsync(new DroppedItem(item, monster.Position, map, null, owners)).ConfigureAwait(false);
    }

    /// <summary>
    /// Determines whether the kill of a monster drops a Resonance Core. A boss which drops one is
    /// marked for the rest of the day.
    /// </summary>
    /// <param name="monsterNumber">The number of the killed monster.</param>
    /// <returns><see langword="true"/> when a core drops.</returns>
    internal bool ShouldDrop(short monsterNumber)
    {
        var configuration = this.Configuration ??= new ResonanceCoreDropConfiguration();
        if (!configuration.BossMonsterNumbers.Contains(monsterNumber))
        {
            return this.Roll(configuration.MonsterDropChance);
        }

        var today = this._today();
        lock (this._bossDropDays)
        {
            if (this._bossDropDays.TryGetValue(monsterNumber, out var dropDay) && dropDay == today)
            {
                return false;
            }

            if (!this.Roll(configuration.BossDropChance))
            {
                return false;
            }

            this._bossDropDays[monsterNumber] = today;
            return true;
        }
    }

    private bool Roll(double chancePercent) => chancePercent > 0 && this._randomizer.NextDouble() * 100 < chancePercent;
}
