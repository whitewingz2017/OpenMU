// <copyright file="GuideBookFeaturePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.GuideBook;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// The Guide Book of the custom client: pages about the server which every player reads (F1), and which game
/// master characters edit and publish in the game. The guide is stored in this plugin's configuration.
/// </summary>
[PlugIn]
[Display(Name = nameof(GuideBookResources.Name), Description = nameof(GuideBookResources.Description), ResourceType = typeof(GuideBookResources))]
[Guid("1fd43854-1d71-4fa5-9e47-55fc5e038dc2")]
public sealed class GuideBookFeaturePlugIn : IFeaturePlugIn, ISupportCustomConfiguration<GuideBookSettings>, ISupportDefaultCustomConfiguration
{
    /// <inheritdoc />
    public GuideBookSettings? Configuration { get; set; }

    /// <summary>Gets the settings, if the feature is active.</summary>
    /// <param name="context">The game context.</param>
    /// <returns>The settings, or <c>null</c> if the Guide Book is disabled.</returns>
    public static GuideBookSettings? Settings(IGameContext context)
        => context.FeaturePlugIns.GetPlugIn<GuideBookFeaturePlugIn>() is { } feature
            ? feature.Configuration ??= new GuideBookSettings()
            : null;

    /// <inheritdoc />
    public object CreateDefaultConfig() => new GuideBookSettings();
}
