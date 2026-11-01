namespace MUnique.OpenMU.GameLogic.Muun;
using System.Runtime.CompilerServices;
using MUnique.OpenMU.GameLogic.NPC;
using MUnique.OpenMU.GameLogic.Pet;
using MUnique.OpenMU.GameLogic.Views.Inventory;
/// <summary>Server-timed assistance against the owner's recently attacked monster, without chasing.</summary>
public sealed class MuunCombat
{
    private readonly ConditionalWeakTable<Player, CombatState> _states = new();
    /// <summary>Records only real player attacks; surrogate pet hits cannot recursively trigger attacks.</summary>
    public void Record(IAttackable target, IAttacker attacker)
    {
        if (attacker is not Player owner || target is not Monster || !target.IsAlive) return;
        var state = this._states.GetValue(owner, _ => new());
        lock (state) { state.Target = new(target); state.LastHit = DateTime.UtcNow; state.Character = owner.SelectedCharacter; }
    }
    /// <summary>Runs from the feature task under the player lock, and uses normal damage and kill processing.</summary>
    public async ValueTask TickAsync(Player owner)
    {
        if (!this._states.TryGetValue(owner, out var state) || !owner.IsAlive || owner.Attributes is null
            || owner.CurrentMap is null || owner.IsAtSafezone()) return;
        IAttackable? target;
        lock (state)
        {
            if (state.Character != owner.SelectedCharacter || DateTime.UtcNow - state.LastHit > TimeSpan.FromSeconds(3)
                || state.Target?.TryGetTarget(out target) != true) return;
        }
        if (target is null || !target.IsAlive || target.IsTeleporting || target.CurrentMap != owner.CurrentMap || target.IsAtSafezone()) return;
        var config = MuunFeaturePlugIn.Settings(owner.GameContext);
        foreach (var item in owner.SelectedCharacter?.MuunInventory?.Items ?? [])
        {
            var rule = MuunRules.Find(config, MuunRules.Type(item));
            if (rule is null || !MuunRules.IsActive(owner, item, rule) || !CanAttack(rule)
                || !target.IsInRange(owner.Position, rule.AttackRange) || state.Next[item.ItemSlot] > DateTime.UtcNow) continue;
            state.Next[item.ItemSlot] = DateTime.UtcNow.AddMilliseconds(rule.AttackIntervalMilliseconds);
            await owner.ForEachWorldObserverAsync<IMuunView>(v => v.AttackAsync(owner, target, item.ItemSlot), true).ConfigureAwait(false);
            await target.AttackByAsync(new AttackerSurrogate(owner, owner.Attributes), null, false, rule.AttackDamageFactor).ConfigureAwait(false);
            if (!target.IsAlive) break;
        }
    }
    /// <summary>Rejects disabled and malformed combat settings rather than allowing accelerated attacks.</summary>
    public static bool CanAttack(MuunDefinition rule) => double.IsFinite(rule.AttackDamageFactor)
        && rule.AttackDamageFactor is > 0 and <= 1 && rule.AttackIntervalMilliseconds is >= 500 and <= 60000
        && rule.AttackRange is >= 1 and <= 10;
    private sealed class CombatState
    {
        public WeakReference<IAttackable>? Target { get; set; }
        public DataModel.Entities.Character? Character { get; set; }
        public DateTime LastHit { get; set; }
        public DateTime[] Next { get; } = new DateTime[MuunRules.EquipmentCount];
    }
}
