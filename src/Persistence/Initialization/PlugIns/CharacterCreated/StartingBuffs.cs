// <copyright file="StartingBuffs.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.PlugIns.CharacterCreated;

using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;

/// <summary>Resolves and validates the starting buffs of a class profile.</summary>
public static class StartingBuffs
{
    /// <summary>
    /// Determines whether the item can be a starting buff, which is the case when its consume effect boosts something,
    /// as for the items whose effect the cash shop activates.
    /// </summary>
    /// <param name="definition">The item definition.</param>
    /// <returns><c>true</c>, if the item can be a starting buff.</returns>
    public static bool IsBuffItem(ItemDefinition definition) => definition.ConsumeEffect is { PowerUpDefinitions.Count: > 0 };

    /// <summary>Gets the time span of the duration.</summary>
    /// <param name="duration">The duration.</param>
    /// <returns>The time span.</returns>
    public static TimeSpan ToTimeSpan(this StartingBuffDuration duration) => TimeSpan.FromHours((int)duration);

    /// <summary>Resolves the effects of the buffs, and checks that they don't conflict with each other.</summary>
    /// <param name="buffs">The buffs.</param>
    /// <param name="definitions">The item definitions.</param>
    /// <returns>The effect of each buff, in the same order.</returns>
    /// <exception cref="InvalidOperationException">When a buff is unknown, has no effect, has an undefined duration, or conflicts with another buff.</exception>
    /// <remarks>
    /// Two effects conflict when they are the same or share their sub type, the default rule of the cash shop.
    /// The character would otherwise get just one of them.
    /// </remarks>
    public static IReadOnlyList<MagicEffectDefinition> ResolveEffects(IEnumerable<StartingBuffReward> buffs, IEnumerable<ItemDefinition> definitions)
    {
        var effects = new List<MagicEffectDefinition>();
        foreach (var buff in buffs)
        {
            if (!Enum.IsDefined(buff.Duration))
            {
                throw new InvalidOperationException($"Undefined starting buff duration {buff.Duration}.");
            }

            var definition = definitions.SingleOrDefault(i => i.Group == buff.Group && i.Number == buff.Number);
            if (definition is null || !IsBuffItem(definition) || definition.ConsumeEffect is not { } effect)
            {
                throw new InvalidOperationException($"Item {buff.Group}:{buff.Number} is not a buff item.");
            }

            if (effects.Any(e => e.Number == effect.Number || e.SubType == effect.SubType))
            {
                throw new InvalidOperationException($"The starting buff {effect} conflicts with another starting buff.");
            }

            effects.Add(effect);
        }

        return effects;
    }
}
