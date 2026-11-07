// <copyright file="RankTitles.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix;

using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.RankTitles;
using MUnique.OpenMU.PlugIns;

/// <summary>Installs the title feature for Season 6 without replacing customized prices.</summary>
public static class RankTitles
{
    /// <summary>Adds enabled default settings when the title feature has not been configured.</summary>
    /// <param name="context">The configuration context.</param>
    /// <param name="configuration">The game configuration.</param>
    public static void Ensure(IContext context, GameConfiguration configuration)
    {
        if (configuration.PlugInConfigurations.Any(plugin => plugin.TypeId == typeof(RankTitleFeaturePlugIn).GUID))
        {
            return;
        }

        var feature = context.CreateNew<PlugInConfiguration>();
        feature.TypeId = typeof(RankTitleFeaturePlugIn).GUID;
        feature.SetConfiguration(new RankTitleSettings(), null);
        feature.IsActive = true;
        configuration.PlugInConfigurations.Add(feature);
    }
}
