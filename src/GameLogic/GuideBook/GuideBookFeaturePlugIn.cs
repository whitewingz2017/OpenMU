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
    private GuideBookSettings? _configuration;

    // The later default pages were added to the configuration (GuideBookDefaults.AddLaterPages).
    private bool _laterPagesAdded;

    /// <inheritdoc />
    public GuideBookSettings? Configuration
    {
        get => this._configuration;
        set
        {
            this._configuration = value;
            this._laterPagesAdded = false;
        }
    }

    /// <summary>Gets the settings, if the feature is active.</summary>
    /// <param name="context">The game context.</param>
    /// <returns>The settings, or <c>null</c> if the Guide Book is disabled.</returns>
    public static GuideBookSettings? Settings(IGameContext context)
    {
        if (context.FeaturePlugIns.GetPlugIn<GuideBookFeaturePlugIn>() is not { } feature)
        {
            return null;
        }

        var settings = feature.Configuration ??= new GuideBookSettings();
        if (!feature._laterPagesAdded)
        {
            GuideBookDefaults.AddLaterPages(settings);
            feature._laterPagesAdded = true;
        }

        return settings;
    }

    /// <inheritdoc />
    public object CreateDefaultConfig() => new GuideBookSettings();
}
