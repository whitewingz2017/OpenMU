// <copyright file="CharacterClassFamily.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic;

using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;

/// <summary>Resolves character class families using the configured promotion chains.</summary>
public static class CharacterClassFamily
{
    /// <summary>Gets the first class in the player's promotion chain.</summary>
    /// <param name="player">The player.</param>
    /// <returns>The base class, if the player has selected a character.</returns>
    public static CharacterClass? Of(Player player)
    {
        if (player.SelectedCharacter?.CharacterClass is not { } current)
        {
            return null;
        }

        var classes = player.GameContext.Configuration.CharacterClasses;
        var selectedNumber = current.Number;
        current = classes.FirstOrDefault(candidate => candidate.Number == selectedNumber) ?? current;
        for (var remaining = classes.Count; remaining > 0; remaining--)
        {
            var number = current.Number;
            var parent = classes.FirstOrDefault(candidate => candidate.NextGenerationClass?.Number == number);
            if (parent is null)
            {
                return current;
            }

            current = parent;
        }

        return current;
    }

    /// <summary>Gets the available families from the configured classes.</summary>
    /// <param name="configuration">The game configuration.</param>
    /// <returns>The base class of each family.</returns>
    public static IEnumerable<CharacterClass> Roots(GameConfiguration configuration)
    {
        var promotions = configuration.CharacterClasses.Select(c => c.NextGenerationClass?.Number).ToHashSet();
        return configuration.CharacterClasses.Where(c => !promotions.Contains(c.Number));
    }

    /// <summary>Checks whether the item allows any member of the family.</summary>
    /// <param name="item">The item definition.</param>
    /// <param name="family">The base class of the family, or null for no selected class.</param>
    /// <returns>Whether the item is unrestricted or matches the family.</returns>
    public static bool Allows(ItemDefinition item, CharacterClass? family)
    {
        if (item.QualifiedCharacters.Count == 0)
        {
            return true;
        }

        var visited = new HashSet<byte>();
        for (var member = family; member is not null && visited.Add(member.Number); member = member.NextGenerationClass)
        {
            if (item.QualifiedCharacters.Any(qualified => qualified.Number == member.Number))
            {
                return true;
            }
        }

        return false;
    }
}
