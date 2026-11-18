// <copyright file="FeatureListResources.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Features;

using System.Globalization;

/// <summary>Provides localized metadata for the feature list view.</summary>
public static class FeatureListResources
{
    /// <summary>Gets the name of the view.</summary>
    public static string ViewName => Get("FeatureListView_Name");

    /// <summary>Gets the description of the view.</summary>
    public static string ViewDescription => Get("FeatureListView_Description");

    private static string Get(string key) => PlugInResources.ResourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? string.Empty;
}
