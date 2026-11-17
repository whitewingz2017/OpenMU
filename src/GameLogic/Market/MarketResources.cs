// <copyright file="MarketResources.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Market;

using System.Globalization;

/// <summary>Provides localized metadata for the Marketplace plugins.</summary>
public static class MarketResources
{
    /// <summary>Gets the feature name.</summary>
    public static string Name => Get("Market_Name");

    /// <summary>Gets the feature description.</summary>
    public static string Description => Get("Market_Description");

    /// <summary>Gets the name of the view.</summary>
    public static string ViewName => Get("MarketView_Name");

    /// <summary>Gets the description of the view.</summary>
    public static string ViewDescription => Get("MarketView_Description");

    private static string Get(string key) => PlugInResources.ResourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? string.Empty;
}
