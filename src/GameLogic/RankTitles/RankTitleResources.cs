// <copyright file="RankTitleResources.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.RankTitles;

using System.Globalization;

/// <summary>Provides localized metadata for the title plugins.</summary>
public static class RankTitleResources
{
    /// <summary>Gets the feature name.</summary>
    public static string Name => PlugInResources.ResourceManager.GetString("RankTitles_Name", CultureInfo.CurrentUICulture) ?? string.Empty;

    /// <summary>Gets the feature description.</summary>
    public static string Description => PlugInResources.ResourceManager.GetString("RankTitles_Description", CultureInfo.CurrentUICulture) ?? string.Empty;

    /// <summary>Gets the configuration update name.</summary>
    public static string UpdateName => PlugInResources.ResourceManager.GetString("RankTitles_UpdateName", CultureInfo.CurrentUICulture) ?? string.Empty;

    /// <summary>Gets the configuration update description.</summary>
    public static string UpdateDescription => PlugInResources.ResourceManager.GetString("RankTitles_UpdateDescription", CultureInfo.CurrentUICulture) ?? string.Empty;
}
