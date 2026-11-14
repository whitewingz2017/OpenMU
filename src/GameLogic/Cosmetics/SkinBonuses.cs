// <copyright file="SkinBonuses.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Cosmetics;

using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.GameLogic.Attributes;

/// <summary>Restricts skin bonuses to one of the configured normal item options which increase HP, mana or zen.</summary>
public static class SkinBonuses
{
    /// <summary>
    /// The attributes a skin bonus may increase: maximum HP and mana and the zen amount of armor,
    /// HP and mana after a monster kill of weapons, and the full HP and mana recovery of wings.
    /// </summary>
    private static readonly IReadOnlySet<Guid> AllowedAttributeIds = new HashSet<Guid>(new AttributeDefinition[]
    {
        Stats.MaximumHealth,
        Stats.MaximumMana,
        Stats.MoneyAmountRate,
        Stats.HealthAfterMonsterKillMultiplier,
        Stats.ManaAfterMonsterKillMultiplier,
        Stats.FullyRecoverHealthAfterHitChance,
        Stats.FullyRecoverManaAfterHitChance,
    }.Select(attribute => attribute.Id));

    /// <summary>Gets the server-authoritative choices for this skin.</summary>
    /// <param name="definition">The skin definition.</param>
    public static IEnumerable<IncreasableItemOption> Choices(ItemDefinition definition)
        => definition.PossibleItemOptions.SelectMany(group => group.PossibleOptions)
            .Where(option => option.OptionType == ItemOptionTypes.Excellent || option.OptionType == ItemOptionTypes.Wing)
            .Where(option => option.PowerUpDefinition?.TargetAttribute is { } attribute && AllowedAttributeIds.Contains(attribute.Id));

    /// <summary>Resolves one GM excellent bit flag, choosing the first configured bonus when omitted.</summary>
    /// <param name="definition">The skin definition.</param>
    /// <param name="flags">The usual excellent flag mask, or zero for the default.</param>
    /// <returns>The chosen option; null for a pet or an invalid mask.</returns>
    public static IncreasableItemOption? FromFlags(ItemDefinition definition, int flags)
        => Choices(definition).OrderBy(option => option.Number).FirstOrDefault(option => flags == 0 || flags == 1 << (option.Number - 1));

    /// <summary>Checks saved options before granting their attributes.</summary>
    /// <param name="item">The persistent skin item.</param>
    public static bool IsValid(Item item)
    {
        if (item.Definition is not { } definition)
        {
            return false;
        }

        var choices = Choices(definition).ToArray();
        return choices.Length == 0 ? item.ItemOptions.Count == 0
            : item.ItemOptions.Count == 1 && item.ItemOptions.All(link => link.Level is 0 or 1 && choices.Contains(link.ItemOption));
    }
}
