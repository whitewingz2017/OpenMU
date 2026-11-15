// <copyright file="AddBossKillRewardDefaultsUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using System.Text.Json;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.PlugIns.BossKillRewards;
using MUnique.OpenMU.Persistence.Initialization.Properties;
using MUnique.OpenMU.Persistence.Json;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Fills the default bosses into the configuration of the <see cref="BossKillRewardPlugIn"/>, when it has none.
/// </summary>
/// <remarks>
/// The first version of the plugin had no default bosses, and a plugin configuration which already exists
/// doesn't get new defaults. A configuration with rewards is kept as it is.
/// </remarks>
[PlugIn]
[Display(Name = nameof(PlugInResources.AddBossKillRewardDefaultsUpdatePlugIn_Name), Description = nameof(PlugInResources.AddBossKillRewardDefaultsUpdatePlugIn_Description), ResourceType = typeof(PlugInResources))]
[Guid("6CBE306A-C578-4047-B9D1-8CD88261E396")]
public sealed class AddBossKillRewardDefaultsUpdatePlugIn : UpdatePlugInBase
{
    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override string Name => PlugInResources.AddBossKillRewardDefaultsUpdatePlugIn_Name;

    /// <inheritdoc />
    public override string Description => PlugInResources.AddBossKillRewardDefaultsUpdatePlugIn_Description;

    /// <inheritdoc />
    public override bool IsMandatory => false;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 11, 15, 0, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        var plugInConfiguration = gameConfiguration.PlugInConfigurations.FirstOrDefault(c => c.TypeId == typeof(BossKillRewardPlugIn).GUID);
        if (plugInConfiguration is null)
        {
            // The plugin configuration is created with the defaults when the server starts.
            return ValueTask.CompletedTask;
        }

        // Plugin configurations are stored with preserved references ("$id", "$values"),
        // so they are read and written with the same reference handler as the server does.
        BossKillRewardConfiguration? configuration;
        try
        {
            configuration = plugInConfiguration.GetConfiguration<BossKillRewardConfiguration>(new IdReferenceHandler());
        }
        catch (JsonException)
        {
            // A configuration which can't be read has no rewards to keep.
            configuration = null;
        }

        if (configuration is { Rewards.Count: > 0 })
        {
            return ValueTask.CompletedTask;
        }

        var defaults = new BossKillRewardConfiguration();
        defaults.AnnounceKills = configuration?.AnnounceKills ?? defaults.AnnounceKills;
        plugInConfiguration.SetConfiguration(defaults, new IdReferenceHandler());
        return ValueTask.CompletedTask;
    }
}
