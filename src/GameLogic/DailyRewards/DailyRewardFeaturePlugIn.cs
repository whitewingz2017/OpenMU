// <copyright file="DailyRewardFeaturePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.DailyRewards;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.GameLogic.Views.Inventory;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// The daily login rewards: each account claims the reward of the current day of the month once.
/// It's disabled until an administrator activates it and configures the rewards in the admin panel.
/// </summary>
[PlugIn]
[Display(Name = nameof(DailyRewardResources.Name), Description = nameof(DailyRewardResources.Description), ResourceType = typeof(DailyRewardResources))]
[Guid("8f35b78f-33aa-47b8-81ea-8c29271f88a5")]
public sealed class DailyRewardFeaturePlugIn : IFeaturePlugIn, IPlayerStateChangedPlugIn,
    ISupportCustomConfiguration<DailyRewardSettings>, ISupportDefaultCustomConfiguration, IDisabledByDefault
{
    /// <inheritdoc />
    public DailyRewardSettings? Configuration { get; set; }

    /// <summary>Gets the settings, if the feature is active and its configuration is valid.</summary>
    /// <param name="context">The game context.</param>
    /// <returns>The settings, or <c>null</c> if daily rewards can't be claimed.</returns>
    public static DailyRewardSettings? Settings(IGameContext context)
    {
        if (context.FeaturePlugIns.GetPlugIn<DailyRewardFeaturePlugIn>() is not { } feature)
        {
            return null;
        }

        var settings = feature.Configuration ??= new DailyRewardSettings();
        return settings.IsValid() ? settings : null;
    }

    /// <inheritdoc />
    public object CreateDefaultConfig() => new DailyRewardSettings();

    /// <inheritdoc />
    public async ValueTask PlayerStateChangedAsync(Player player, State previousState, State currentState)
    {
        if (currentState == PlayerState.EnteredWorld && player.Account is not null && Settings(player.GameContext) is not null)
        {
            // The client opens the calendar by itself when today's reward can still be claimed.
            await player.InvokeViewPlugInAsync<IDailyRewardView>(view => view.ShowAsync(0, null)).ConfigureAwait(false);
        }
    }
}
