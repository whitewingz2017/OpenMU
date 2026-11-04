// <copyright file="ExcellentCombinationFeaturePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions.Items;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>Enables combination and exposes its rates in the existing plugin configuration editor.</summary>
[PlugIn]
[Display(Name = nameof(ExcellentCombinationResources.Name), Description = nameof(ExcellentCombinationResources.Description), ResourceType = typeof(ExcellentCombinationResources))]
[Guid("78f1b540-c9e4-4541-97b0-f2b1c182703e")]
public sealed class ExcellentCombinationFeaturePlugIn : IFeaturePlugIn, ISupportCustomConfiguration<ExcellentCombinationConfiguration>, ISupportDefaultCustomConfiguration, IDisabledByDefault
{
    /// <inheritdoc />
    public ExcellentCombinationConfiguration? Configuration { get; set; }

    /// <summary>Gets active settings, or null if the feature is disabled.</summary>
    /// <param name="context">The game context.</param>
    /// <returns>The active settings.</returns>
    public static ExcellentCombinationConfiguration? Settings(IGameContext context)
        => context.FeaturePlugIns.GetPlugIn<ExcellentCombinationFeaturePlugIn>() is { } plugin
            ? plugin.Configuration ??= new ExcellentCombinationConfiguration() : null;

    /// <inheritdoc />
    public object CreateDefaultConfig() => new ExcellentCombinationConfiguration();
}
