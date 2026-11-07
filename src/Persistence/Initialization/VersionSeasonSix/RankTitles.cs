// <copyright file="RankTitles.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix;

using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.RankTitles;
using MUnique.OpenMU.Persistence.Initialization.Updates;
using MUnique.OpenMU.PlugIns;

/// <summary>Installs the title feature for Season 6 without replacing customized prices.</summary>
public static class RankTitles
{
    private const int FirstAnimatedTitleRank = 9;
    private const int RemovedTwinMuTitleRank = 10;
    private const int BetaTesterRank = 15;

    /// <summary>Installs the catalog or adds missing animated titles to existing settings.</summary>
    /// <param name="context">The configuration context.</param>
    /// <param name="configuration">The game configuration.</param>
    public static void Ensure(IContext context, GameConfiguration configuration)
    {
        var defaults = new RankTitleSettings();
        if (configuration.PlugInConfigurations.FirstOrDefault(plugin => plugin.TypeId == typeof(RankTitleFeaturePlugIn).GUID) is not { } feature)
        {
            feature = context.CreateNew<PlugInConfiguration>();
            feature.TypeId = typeof(RankTitleFeaturePlugIn).GUID;
            feature.SetConfiguration(defaults, null);
            feature.IsActive = true;
            configuration.PlugInConfigurations.Add(feature);
            return;
        }

        var settings = feature.GetConfiguration<RankTitleSettings>(new ConfigureNpcTalkPlugInsPlugInBase.IdReferenceHandler());
        if (settings is null)
        {
            feature.SetConfiguration(defaults, null);
            return;
        }

        var missing = defaults.Titles.Where(title => title.Rank >= FirstAnimatedTitleRank)
            .ExceptBy(settings.Titles.Select(title => title.Rank), title => title.Rank).ToArray();
        var retired = settings.Titles.Where(title => title.Rank == RemovedTwinMuTitleRank).ToArray();
        var betaTester = settings.Titles.FirstOrDefault(title => title.Rank == BetaTesterRank && title.Name == "TwinMU II");
        if (missing.Length == 0 && retired.Length == 0 && betaTester is null)
        {
            return;
        }

        foreach (var title in retired)
        {
            settings.Titles.Remove(title);
        }

        if (betaTester is { })
        {
            betaTester.Name = defaults.Titles.Single(title => title.Rank == BetaTesterRank).Name;
        }

        foreach (var title in missing)
        {
            settings.Titles.Add(title);
        }

        if (!settings.IsValid())
        {
            throw new InvalidOperationException("The animated rank titles would produce an invalid catalog. Check the Rank Titles feature configuration.");
        }

        feature.SetConfiguration(settings, null);
    }
}
