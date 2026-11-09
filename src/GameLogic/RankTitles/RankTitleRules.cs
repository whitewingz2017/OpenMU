// <copyright file="RankTitleRules.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.RankTitles;

using System.Text.Json;
using MUnique.OpenMU.GameLogic.Attributes;

/// <summary>Reads the persisted title ownership without accepting invalid identifiers.</summary>
public static class RankTitleRules
{
    /// <summary>Checks the fixed client identifiers which cannot be purchased.</summary>
    /// <param name="rank">The title identifier.</param>
    public static bool IsEarned(int rank) => rank is RankTitleFeaturePlugIn.VipRank or RankTitleFeaturePlugIn.PvpKingRank or RankTitleFeaturePlugIn.DestroyerRank;

    /// <summary>Checks current eligibility, ignoring any old purchases of earned ranks.</summary>
    /// <param name="player">The player whose eligibility is checked.</param>
    /// <param name="rank">The title identifier.</param>
    /// <param name="owned">The player's validated permanent title ownership.</param>
    public static bool CanEquip(Player player, int rank, HashSet<int> owned)
        => rank switch
        {
            RankTitleFeaturePlugIn.VipRank => player.Account?.VipExpiresAtUtc > DateTime.UtcNow || player.Attributes?[Stats.IsVip] > 0,
            RankTitleFeaturePlugIn.PvpKingRank or RankTitleFeaturePlugIn.DestroyerRank
                => player.GameContext.FeaturePlugIns.GetPlugIn<RankTitleFeaturePlugIn>()?.HasCompetitiveTitle(player, rank) == true,
            _ => owned.Contains(rank),
        };

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
    {
        if (player.SelectedCharacter is not { HideRankTitle: false } character
            || RankTitleFeaturePlugIn.Settings(player.GameContext) is not { } settings
            || !TryReadOwned(character, out var owned))
        {
            return 0;
        }

        if (settings.Titles.Any(title => title.Rank == character.ActiveRankTitle) && CanEquip(player, character.ActiveRankTitle, owned))
        {
            return character.ActiveRankTitle;
        }

        // Earned titles appear automatically until the player selects another eligible title.
        return settings.Titles.Where(title => IsEarned(title.Rank) && CanEquip(player, title.Rank, owned))
            .Select(title => title.Rank).OrderDescending().FirstOrDefault();
    }
}
