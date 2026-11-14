// <copyright file="ResonanceCoreDropResources.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions.Items;

/// <summary>
/// The localized texts of the <see cref="ResonanceCoreDropPlugIn"/>.
/// </summary>
public static class ResonanceCoreDropResources
{
    /// <summary>Gets the plugin name.</summary>
    public static string Name => Get("ResonanceCoreDrop_Name");

    /// <summary>Gets the plugin description.</summary>
    public static string Description => Get("ResonanceCoreDrop_Description");

    /// <summary>Gets the caption of <see cref="ResonanceCoreDropConfiguration.MonsterDropChance"/>.</summary>
    public static string MonsterDropChance => Get("ResonanceCoreDrop_MonsterDropChance");

    /// <summary>Gets the caption of <see cref="ResonanceCoreDropConfiguration.BossDropChance"/>.</summary>
    public static string BossDropChance => Get("ResonanceCoreDrop_BossDropChance");

    /// <summary>Gets the caption of <see cref="ResonanceCoreDropConfiguration.BossMonsterNumbers"/>.</summary>
    public static string BossMonsterNumbers => Get("ResonanceCoreDrop_BossMonsterNumbers");

    private static string Get(string key)
        => Properties.PlugInResources.ResourceManager.GetString(key, System.Globalization.CultureInfo.CurrentUICulture) ?? key;
}
