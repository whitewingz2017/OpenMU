// <copyright file="MuunRules.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Muun;

/// <summary>Ownership-independent Muun validation shared by actions, bonuses and views.</summary>
public static class MuunRules
{
    /// <summary>Protocol positions zero and one are equipment, all others are collection cells.</summary>
    public const int EquipmentCount = 2;
    /// <summary>Gets an enabled species definition; duplicated ids are rejected.</summary>
    public static MuunDefinition? Find(MuunConfiguration? config, int type)
        => config?.Pets.Where(p => p.ItemType == type).Take(2).ToArray() is { Length: 1 } matches ? matches[0] : null;
    /// <summary>Gets the item type on the custom client.</summary>
    public static int Type(Item item) => (item.Definition!.Group * 512) + item.Definition!.Number;
    /// <summary>Determines whether a stored pet can currently grant its configured bonus.</summary>
    public static bool IsActive(Player player, Item item, MuunDefinition rule)
        => item.ItemSlot < EquipmentCount && item.Durability > 0 && item.Level <= rule.MaximumLevel
            && (item.ExpiresAtUtc is null || item.ExpiresAtUtc > DateTime.UtcNow)
            && player.Attributes is { } attributes && attributes[Attributes.Stats.Level] >= rule.MinimumCharacterLevel
            && (rule.Maps.Count == 0 || (player.CurrentMap is { } map && rule.Maps.Contains(map.Definition.Number)));
    /// <summary>Gets the special-period multiplier, using server UTC.</summary>
    public static float Multiplier(MuunDefinition rule, DateTime now)
        => (!rule.SpecialFromUtc.HasValue || now >= rule.SpecialFromUtc.Value)
            && (!rule.SpecialUntilUtc.HasValue || now < rule.SpecialUntilUtc.Value)
            ? Math.Clamp(rule.SpecialMultiplier, 1, 10) : 1;
}
