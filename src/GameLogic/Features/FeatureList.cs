// <copyright file="FeatureList.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Features;

using MUnique.OpenMU.GameLogic.DailyRewards;
using MUnique.OpenMU.GameLogic.GuideBook;
using MUnique.OpenMU.GameLogic.Market;
using MUnique.OpenMU.GameLogic.Muun;
using MUnique.OpenMU.GameLogic.PartySearch;
using MUnique.OpenMU.GameLogic.PlayerActions.Items;
using MUnique.OpenMU.GameLogic.RankTitles;

/// <summary>
/// The features of the custom client which an administrator activates or deactivates as plugins: the client hides
/// the menu entries and buttons of an inactive one, and shows them again when it's activated.
/// </summary>
public static class FeatureList
{
    /// <summary>Gets which features are active, by the name the client knows them by.</summary>
    /// <param name="context">The game context.</param>
    /// <returns><c>true</c> for an active feature, by name.</returns>
    public static IReadOnlyDictionary<string, bool> Create(IGameContext context)
    {
        var plugIns = context.FeaturePlugIns;
        return new Dictionary<string, bool>(StringComparer.Ordinal)
        {
            ["GuideBook"] = plugIns.GetPlugIn<GuideBookFeaturePlugIn>() is not null,
            ["Marketplace"] = plugIns.GetPlugIn<MarketFeaturePlugIn>() is not null,
            ["PartySearch"] = plugIns.GetPlugIn<PartySearchFeaturePlugIn>() is not null,
            ["DailyRewards"] = plugIns.GetPlugIn<DailyRewardFeaturePlugIn>() is not null,
            ["ExcellentCombination"] = plugIns.GetPlugIn<ExcellentCombinationFeaturePlugIn>() is not null,
            ["Muun"] = plugIns.GetPlugIn<MuunFeaturePlugIn>() is not null,
            ["RankShop"] = plugIns.GetPlugIn<RankTitleFeaturePlugIn>() is not null,
        };
    }
}
