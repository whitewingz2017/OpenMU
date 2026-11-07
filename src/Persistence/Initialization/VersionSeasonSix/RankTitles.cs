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
        if (missing.Length == 0)
        {
            return;
        }

        settings.Titles.AddRange(missing);
        if (!settings.IsValid())
        {
            throw new InvalidOperationException("The animated rank titles would produce an invalid catalog. Check the Rank Titles feature configuration.");
        }

        feature.SetConfiguration(settings, null);
    }
}
