// <copyright file="MuunBonuses.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Muun;

using System.Runtime.CompilerServices;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.GameLogic.Attributes;

/// <summary>Applies normal configured item bonuses without occupying the normal helper slot.</summary>
public static class MuunBonuses
{
    private static readonly ConditionalWeakTable<Player, State> States = new();
    /// <summary>Refreshes only when equipment, map, level, expiry or the attribute system changed.</summary>
    public static bool Refresh(Player player)
    {
        if (player.Attributes is not { } attributes) return false;
        var config = MuunFeaturePlugIn.Settings(player.GameContext);
        var state = States.GetValue(player, _ => new());
        var hash = new HashCode();
        hash.Add(attributes); hash.Add(config); hash.Add(player.CurrentMap); hash.Add(attributes[Attributes.Stats.Level]);
        foreach (var item in player.SelectedCharacter?.MuunInventory?.Items ?? [])
        {
            if (item.ItemSlot >= MuunRules.EquipmentCount) continue;
            hash.Add(item); hash.Add(item.Definition); hash.Add(item.Level); hash.Add(item.Durability);
            var rule = MuunRules.Find(config, MuunRules.Type(item));
            hash.Add(rule is not null && MuunRules.IsActive(player, item, rule));
            hash.Add(rule is null ? 1 : MuunRules.Multiplier(rule, DateTime.UtcNow));
        }
        var signature = hash.ToHashCode();
        if (state.Signature == signature && ReferenceEquals(state.Attributes, attributes) && state.Items.All(attributes.ItemPowerUps.ContainsKey)) return false;
        foreach (var old in state.Items)
        {
            if (attributes.ItemPowerUps.Remove(old, out var values)) foreach (var value in values) value.Dispose();
        }
        state.Items.Clear(); state.Attributes = attributes; state.Signature = signature;
        foreach (var item in player.SelectedCharacter?.MuunInventory?.Items ?? [])
        {
            var rule = MuunRules.Find(config, MuunRules.Type(item));
            if (rule is null || !MuunRules.IsActive(player, item, rule)) continue;
            var multiplier = MuunRules.Multiplier(rule, DateTime.UtcNow);
            var values = new List<PowerUpWrapper>();
            foreach (var bonus in item.Definition!.BasePowerUpAttributes)
            {
                if (bonus.TargetAttribute is not { } target) continue;
                var value = bonus.BaseValue + (bonus.BonusPerLevelTable?.BonusPerLevel.FirstOrDefault(b => b.Level == item.Level)?.AdditionalValue ?? 0);
                value = bonus.AggregateType == AggregateType.Multiplicate ? 1 + ((value - 1) * multiplier) : value * multiplier;
                values.Add(new PowerUpWrapper(new ConstantElement(value, bonus.AggregateType), target, attributes));
            }
            attributes.ItemPowerUps[item] = values;
            state.Items.Add(item);
        }
        return true;
    }
    private sealed class State
    {
        public int Signature { get; set; }
        public Attributes.ItemAwareAttributeSystem? Attributes { get; set; }
        public List<Item> Items { get; } = [];
    }
}
