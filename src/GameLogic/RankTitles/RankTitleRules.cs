// <copyright file="RankTitleRules.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.RankTitles;

using System.Text.Json;

/// <summary>Reads the persisted title ownership without accepting invalid identifiers.</summary>
public static class RankTitleRules
{
    /// <summary>Reads owned title identifiers, including characters created before titles existed.</summary>
    /// <param name="character">The character whose titles are read.</param>
    /// <param name="owned">The validated title identifiers, or an empty set on failure.</param>
    /// <returns>Whether the persisted data is valid.</returns>
    public static bool TryReadOwned(Character character, out HashSet<int> owned)
    {
        owned = [];
        try
        {
            var titles = JsonSerializer.Deserialize<HashSet<int>>(character.OwnedRankTitlesJson);
            if (titles is null || titles.Count > RankTitleFeaturePlugIn.MaximumOwnedTitles || titles.Any(rank => rank is <= 0 or > ushort.MaxValue))
            {
                return false;
            }

            owned = titles;
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    /// <summary>Gets the owned, configured title shown to other players.</summary>
    /// <param name="player">The title owner.</param>
    /// <returns>The active rank, or zero when none can be shown.</returns>
    public static int Active(Player player)
        => player.SelectedCharacter is { } character
            && RankTitleFeaturePlugIn.Settings(player.GameContext) is { } settings
            && settings.Titles.Any(title => title.Rank == character.ActiveRankTitle)
            && TryReadOwned(character, out var owned) && owned.Contains(character.ActiveRankTitle)
            ? character.ActiveRankTitle : 0;
}
