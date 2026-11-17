// <copyright file="OfflineLevelingFeaturePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Offline;

using System.Runtime.InteropServices;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// The paid offline leveling of the custom client: /offlevel opens a window where the player pays with zen,
/// WCoinC or Goblin Points, every hour or for a number of hours in advance, and may add the protection from
/// players. Without this plugin, /offlevel starts at once and pays zen as the MU Helper does.
/// </summary>
[PlugIn]
[Display(Name = nameof(OfflineLevelingResources.Name), Description = nameof(OfflineLevelingResources.Description), ResourceType = typeof(OfflineLevelingResources))]
[Guid("7205709a-b190-4af6-be70-11f25aad3ead")]
public sealed class OfflineLevelingFeaturePlugIn : IFeaturePlugIn, ISupportCustomConfiguration<OfflineLevelingSettings>, ISupportDefaultCustomConfiguration
{
    /// <inheritdoc />
    public OfflineLevelingSettings? Configuration { get; set; }

    /// <summary>Gets the settings.</summary>
    public OfflineLevelingSettings Settings => this.Configuration ??= new OfflineLevelingSettings();

    /// <summary>Gets the settings of the active plugin.</summary>
    /// <param name="context">The game context.</param>
    /// <returns>The settings, or <c>null</c> if the paid offline leveling is disabled.</returns>
    public static OfflineLevelingSettings? GetSettings(IGameContext context)
        => context.FeaturePlugIns.GetPlugIn<OfflineLevelingFeaturePlugIn>()?.Settings;

    /// <inheritdoc />
    public object CreateDefaultConfig() => new OfflineLevelingSettings();
}
