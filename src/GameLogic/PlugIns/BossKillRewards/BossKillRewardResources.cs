// <copyright file="BossKillRewardResources.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.BossKillRewards;

/// <summary>
/// The localized texts of the <see cref="BossKillRewardPlugIn"/>.
/// </summary>
public static class BossKillRewardResources
{
    /// <summary>Gets the plugin name.</summary>
    public static string Name => Get("BossKillReward_Name");

    /// <summary>Gets the plugin description.</summary>
    public static string Description => Get("BossKillReward_Description");

    /// <summary>Gets the caption of <see cref="BossKillRewardConfiguration.AnnounceKills"/>.</summary>
    public static string AnnounceKills => Get("BossKillReward_AnnounceKills");

    /// <summary>Gets the caption of <see cref="BossKillRewardConfiguration.Rewards"/>.</summary>
    public static string Rewards => Get("BossKillReward_Rewards");

    /// <summary>Gets the caption of <see cref="BossKillReward.Enabled"/>.</summary>
    public static string Enabled => Get("BossKillReward_Enabled");

    /// <summary>Gets the caption of <see cref="BossKillReward.MonsterNumber"/>.</summary>
    public static string MonsterNumber => Get("BossKillReward_MonsterNumber");

    /// <summary>Gets the caption of <see cref="BossKillReward.WCoinC"/>.</summary>
    public static string WCoinC => Get("BossKillReward_WCoinC");

    /// <summary>Gets the caption of <see cref="BossKillReward.WCoinP"/>.</summary>
    public static string WCoinP => Get("BossKillReward_WCoinP");

    /// <summary>Gets the caption of <see cref="BossKillReward.GoblinPoints"/>.</summary>
    public static string GoblinPoints => Get("BossKillReward_GoblinPoints");

    private static string Get(string key)
        => Properties.PlugInResources.ResourceManager.GetString(key, System.Globalization.CultureInfo.CurrentUICulture) ?? key;
}
