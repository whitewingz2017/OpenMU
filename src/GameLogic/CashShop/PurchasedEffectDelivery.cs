// <copyright file="PurchasedEffectDelivery.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.CashShop;

using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.GameLogic.Views.CashShop;

/// <summary>
/// Activates a configured cash-shop effect instead of creating an inert inventory item.
/// </summary>
internal sealed class PurchasedEffectDelivery : ICashShopProductDeliveryPlugIn
{
    /// <inheritdoc />
    public ItemIdentifier Key => default;

    /// <inheritdoc />
    public bool CanDeliver(CashShopProduct product) => product.Duration > TimeSpan.Zero
        && product.ItemDefinition?.ConsumeEffect is { PowerUpDefinitions.Count: > 0 };

    /// <inheritdoc />
    public ValueTask<CashShopUseResult> DeliverAsync(Player player, CashShopProduct product)
    {
        if (!this.CanDeliver(product) || player.SelectedCharacter is not { } character
            || product.ItemDefinition?.ConsumeEffect is not { } definition)
        {
            return ValueTask.FromResult(CashShopUseResult.CannotUse);
        }

        if (product.ItemDefinition is { QualifiedCharacters.Count: > 0 } itemDefinition
            && (character.CharacterClass is null || !itemDefinition.QualifiedCharacters.Contains(character.CharacterClass)))
        {
            return ValueTask.FromResult(CashShopUseResult.CannotUse);
        }

        var now = DateTime.UtcNow;
        if (character.PurchasedEffects.Any(effect => effect.ExpiresAtUtc > now
                && effect.Definition?.SubType == definition.SubType)
            || player.MagicEffectList.TryGetActiveEffectOfSubType(definition.SubType) is not null)
        {
            return ValueTask.FromResult(CashShopUseResult.ConflictingItemActive);
        }

        var entry = player.PersistenceContext.CreateNew<CharacterMagicEffect>();
        entry.Definition = definition;
        entry.ExpiresAtUtc = now.Add(product.Duration);
        character.PurchasedEffects.Add(entry);
        return ValueTask.FromResult(CashShopUseResult.Success);
    }

    /// <inheritdoc />
    public async ValueTask DeliveredAsync(Player player, CashShopProduct product)
        => await player.RunPersistenceExclusiveAsync(() => SynchronizeAsync(player, DateTime.UtcNow)).ConfigureAwait(false);

    /// <summary>
    /// Restores active purchases and removes elapsed purchases under the player's persistence lock.
    /// </summary>
    /// <param name="player">The owner.</param>
    /// <param name="utcNow">The current UTC time.</param>
    /// <returns>Whether persistent entries were removed.</returns>
    internal static async ValueTask<bool> SynchronizeAsync(Player player, DateTime utcNow)
    {
        if (player.SelectedCharacter is not { } character || player.Attributes is null)
        {
            return false;
        }

        var changed = false;
        foreach (var entry in character.PurchasedEffects.ToArray())
        {
            if (entry.ExpiresAtUtc <= utcNow || entry.Definition is not { } definition)
            {
                if (entry.Definition is { } expiredDefinition
                    && player.MagicEffectList.TryGetEffect(expiredDefinition.Number, out var active))
                {
                    await active.DisposeAsync().ConfigureAwait(false);
                }

                character.PurchasedEffects.Remove(entry);
                await player.PersistenceContext.DeleteAsync(entry).ConfigureAwait(false);
                changed = true;
                continue;
            }

            if (player.MagicEffectList.HasEffect(definition))
            {
                continue;
            }

            var boosts = definition.PowerUpDefinitions
                .Where(boost => boost.Boost is not null && boost.TargetAttribute is not null)
                .Select(boost => new MagicEffect.ElementWithTarget(player.Attributes.CreateElement(boost), boost.TargetAttribute!))
                .ToArray();
            await player.MagicEffectList.AddEffectAsync(new MagicEffect(entry.ExpiresAtUtc - utcNow, definition, boosts)).ConfigureAwait(false);
        }

        return changed;
    }
}
