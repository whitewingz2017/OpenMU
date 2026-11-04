// <copyright file="MuunRules.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Muun;

using MUnique.OpenMU.DataModel.Configuration.Items;

/// <summary>Ownership-independent Muun validation shared by actions, bonuses and views.</summary>
public static class MuunRules
{
    /// <summary>Protocol positions zero and one are equipment, all others are collection cells.</summary>
    public const int EquipmentCount = 2;
    /// <summary>Hours per day; an <see cref="MuunDefinition.UntilHourUtc"/> of this value ends the window at midnight.</summary>
    public const int HoursPerDay = 24;
    /// <summary>Gets an enabled species definition; duplicated ids are rejected.</summary>
    public static MuunDefinition? Find(MuunConfiguration? config, int type)
        => config?.Pets.Where(p => p.ItemType == type).Take(2).ToArray() is { Length: 1 } matches ? matches[0] : null;
    /// <summary>Gets the item type on the custom client.</summary>
    public static int Type(Item item) => (item.Definition!.Group * 512) + item.Definition!.Number;

    /// <summary>Mounts use normal pet equipment, not Main/Sub Muun equipment.</summary>
    /// <param name="definition">The configured item definition.</param>
    public static bool IsMount(ItemDefinition? definition)
        => Cosmetics.FenrirMountRules.IsFenrir(definition)
            || (definition?.Group == ItemConstants.DarkHorse.Group && definition.Number == ItemConstants.DarkHorse.Number);

    /// <summary>Determines whether a stored pet can currently grant its configured bonus.</summary>
    public static bool IsActive(Player player, Item item, MuunDefinition rule)
        => InactiveReason(player, item, rule, DateTime.UtcNow) == MuunInactiveReason.None;
    /// <summary>Gets the first failed condition, checked in the order a player can resolve them.</summary>
    public static MuunInactiveReason InactiveReason(Player player, Item item, MuunDefinition rule, DateTime now)
    {
        if (item.ItemSlot >= EquipmentCount || IsMount(item.Definition)) return MuunInactiveReason.NotEquipped;
        if (item.Durability <= 0) return MuunInactiveReason.NoLife;
        if (item.Level > rule.MaximumLevel) return MuunInactiveReason.LevelTooHigh;
        if (item.ExpiresAtUtc is { } end && end <= now) return MuunInactiveReason.Expired;
        if (player.Attributes is not { } attributes || !InLevelRange(rule, attributes[Attributes.Stats.Level])) return MuunInactiveReason.CharacterLevel;
        if (rule.Maps.Count > 0 && (player.CurrentMap is not { } map || !rule.Maps.Contains(map.Definition.Number))) return MuunInactiveReason.Map;
        if (rule.Days.Count > 0 && !rule.Days.Contains(now.DayOfWeek)) return MuunInactiveReason.Day;
        return InHourWindow(rule, now.Hour) ? MuunInactiveReason.None : MuunInactiveReason.Hour;
    }
    /// <summary>Checks the character level against the inclusive configured range.</summary>
    public static bool InLevelRange(MuunDefinition rule, float level)
        => level >= rule.MinimumCharacterLevel && (rule.MaximumCharacterLevel <= 0 || level <= rule.MaximumCharacterLevel);
    /// <summary>Checks a UTC hour against the configured daily window; equal bounds or a lower end wrap past midnight.</summary>
    public static bool InHourWindow(MuunDefinition rule, int hour)
    {
        var from = Math.Clamp(rule.FromHourUtc, 0, HoursPerDay - 1);
        var until = Math.Clamp(rule.UntilHourUtc, 1, HoursPerDay);
        return from < until ? hour >= from && hour < until : hour >= from || hour < until;
    }
    /// <summary>Determines whether the special bonus period is running, using server UTC.</summary>
    public static bool IsSpecial(MuunDefinition rule, DateTime now)
        => (!rule.SpecialFromUtc.HasValue || now >= rule.SpecialFromUtc.Value)
            && (!rule.SpecialUntilUtc.HasValue || now < rule.SpecialUntilUtc.Value);
    /// <summary>Gets the special-period multiplier, using server UTC.</summary>
    public static float Multiplier(MuunDefinition rule, DateTime now)
        => IsSpecial(rule, now) ? Math.Clamp(rule.SpecialMultiplier, 1, 10) : 1;
    /// <summary>Gets the special-period addition for non-multiplicative bonuses.</summary>
    public static float Addition(MuunDefinition rule, DateTime now)
        => IsSpecial(rule, now) && float.IsFinite(rule.SpecialAddition) ? Math.Clamp(rule.SpecialAddition, 0, 1000) : 0;
    /// <summary>Gets the assisted attack interval, shortened during the special period.</summary>
    public static int AttackInterval(MuunDefinition rule, DateTime now)
    {
        var reduction = IsSpecial(rule, now) && double.IsFinite(rule.SpecialAttackIntervalReduction)
            ? Math.Clamp(rule.SpecialAttackIntervalReduction, 0, 0.9) : 0;
        return Math.Max(MuunCombat.MinimumAttackIntervalMilliseconds, (int)(rule.AttackIntervalMilliseconds * (1 - reduction)));
    }
}
