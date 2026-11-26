// <copyright file="DuelBettingFeaturePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.DuelBetting;

using System.Runtime.InteropServices;
using MUnique.OpenMU.PlugIns;

/// <summary>Enables public duel challenges and configures their betting pools.</summary>
[PlugIn]
[Display(Name = nameof(DuelBettingResources.Name), Description = nameof(DuelBettingResources.Description), ResourceType = typeof(DuelBettingResources))]
[Guid("c817045f-8921-4654-8cbe-fcb14459fa0d")]
public sealed class DuelBettingFeaturePlugIn : IFeaturePlugIn, IDisabledByDefault, ISupportCustomConfiguration<DuelBettingSettings>, ISupportDefaultCustomConfiguration
{
    /// <inheritdoc />
    public DuelBettingSettings? Configuration { get; set; }

    /// <summary>Gets the betting settings, or null when the feature is disabled or its settings are invalid.</summary>
    /// <param name="context">The game context.</param>
    /// <returns>The valid settings, or null.</returns>
    public static DuelBettingSettings? GetSettings(IGameContext context)
        => context.FeaturePlugIns.GetPlugIn<DuelBettingFeaturePlugIn>() is { } feature
            && (feature.Configuration ?? new DuelBettingSettings()) is { } settings
            && settings.IsValid()
            ? settings
            : null;

    /// <inheritdoc />
    public object CreateDefaultConfig() => new DuelBettingSettings();
}
