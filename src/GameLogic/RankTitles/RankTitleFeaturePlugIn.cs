// <copyright file="RankTitleFeaturePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.RankTitles;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.GameLogic.Views.Inventory;
using MUnique.OpenMU.PlugIns;

/// <summary>Enables configured titles and sends the owner's title on entering the world.</summary>
[PlugIn]
[Display(Name = nameof(RankTitleResources.Name), Description = nameof(RankTitleResources.Description), ResourceType = typeof(RankTitleResources))]
[Guid("99681727-502e-4513-9073-fc2982264c1a")]
public sealed class RankTitleFeaturePlugIn : IFeaturePlugIn, IPlayerStateChangedPlugIn,
    ISupportCustomConfiguration<RankTitleSettings>, ISupportDefaultCustomConfiguration, IDisabledByDefault
{
    /// <summary>Keeps the complete JSON catalog inside one C2 packet.</summary>
    internal const int MaximumCatalogTitles = 128;

    /// <summary>Bounds persisted title ownership as the configured catalog evolves.</summary>
    internal const int MaximumOwnedTitles = 1024;

    /// <summary>Fits the client's 256-byte UTF-8 title name limit.</summary>
    internal const int MaximumTitleNameLength = 64;

    /// <inheritdoc />
    public RankTitleSettings? Configuration { get; set; }

    /// <summary>Gets validated settings when the feature is enabled.</summary>
    /// <param name="context">The game context.</param>
    /// <returns>The active settings, or null when disabled or invalid.</returns>
    public static RankTitleSettings? Settings(IGameContext context)
    {
        if (context.FeaturePlugIns.GetPlugIn<RankTitleFeaturePlugIn>() is not { } feature)
        {
            return null;
        }

        var settings = feature.Configuration ??= new RankTitleSettings();
        return settings.IsValid() ? settings : null;
    }

    /// <inheritdoc />
    public object CreateDefaultConfig() => new RankTitleSettings();

    /// <inheritdoc />
    public ValueTask PlayerStateChangedAsync(Player player, State previousState, State currentState)
        => currentState == PlayerState.EnteredWorld
            ? player.ForEachWorldObserverAsync<IRankTitleView>(view => view.AppearanceAsync(player), true)
            : ValueTask.CompletedTask;
}
