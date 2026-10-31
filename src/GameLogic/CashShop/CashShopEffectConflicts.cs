// <copyright file="CashShopEffectConflicts.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.CashShop;

/// <summary>
/// Applies configurable pairwise replacements without treating every seal as one exclusive group.
/// </summary>
internal static class CashShopEffectConflicts
{
    internal static bool Conflicts(Player player, MagicEffectDefinition incoming, MagicEffectDefinition existing)
    {
        if (incoming.Number == existing.Number)
        {
            return true;
        }

        var replacements = (CashShopFeaturePlugIn.GetSettings(player.GameContext) ?? new CashShopSettings()).EffectReplacements;
        return replacements.TryGetValue(incoming.Number, out var numbers) && replacements.ContainsKey(existing.Number)
            ? numbers.Contains(existing.Number)
            : incoming.SubType == existing.SubType;
    }

    internal static bool BlocksPurchase(Player player, MagicEffectDefinition incoming, MagicEffectDefinition existing)
    {
        var replacements = (CashShopFeaturePlugIn.GetSettings(player.GameContext) ?? new CashShopSettings()).EffectReplacements;
        return Conflicts(player, incoming, existing)
            && (incoming.Number == existing.Number
                || !replacements.TryGetValue(incoming.Number, out var numbers)
                || !numbers.Contains(existing.Number));
    }

    internal static async ValueTask RemoveAsync(Player player, MagicEffectDefinition incoming)
    {
        if (player.SelectedCharacter is { } character)
        {
            foreach (var entry in character.PurchasedEffects
                         .Where(entry => entry.Definition is { } definition && Conflicts(player, incoming, definition)).ToArray())
            {
                character.PurchasedEffects.Remove(entry);
                await player.PersistenceContext.DeleteAsync(entry).ConfigureAwait(false);
            }
        }

        foreach (var effect in player.MagicEffectList.GetActiveEffectsSnapshot()
                     .Where(effect => Conflicts(player, incoming, effect.Definition)))
        {
            await effect.DisposeAsync().ConfigureAwait(false);
        }
    }
}
