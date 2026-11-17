// <copyright file="OfflineLevelingResources.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Offline;

using System.Globalization;

/// <summary>Provides localized metadata for the offline leveling plugins.</summary>
public static class OfflineLevelingResources
{
    /// <summary>Gets the feature name.</summary>
    public static string Name => Get("OfflineLeveling_Name");

    /// <summary>Gets the feature description.</summary>
    public static string Description => Get("OfflineLeveling_Description");

    /// <summary>Gets the name of the protection plugin.</summary>
    public static string ProtectionName => Get("OfflineProtection_Name");

    /// <summary>Gets the description of the protection plugin.</summary>
    public static string ProtectionDescription => Get("OfflineProtection_Description");

    /// <summary>Gets the name of the view.</summary>
    public static string ViewName => Get("OfflineLevelingView_Name");

    /// <summary>Gets the description of the view.</summary>
    public static string ViewDescription => Get("OfflineLevelingView_Description");

    private static string Get(string key) => PlugInResources.ResourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? string.Empty;
}
