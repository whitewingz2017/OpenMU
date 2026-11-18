// <copyright file="ItemBonusResources.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.ItemBonuses;

using System.Globalization;

/// <summary>Provides localized metadata for the item attributes plugin.</summary>
public static class ItemBonusResources
{
    /// <summary>Gets the feature name.</summary>
    public static string Name => Get("ItemBonuses_Name");

    /// <summary>Gets the feature description.</summary>
    public static string Description => Get("ItemBonuses_Description");

    /// <summary>Gets the name of the view.</summary>
    public static string ViewName => Get("ItemBonusesView_Name");

    /// <summary>Gets the description of the view.</summary>
    public static string ViewDescription => Get("ItemBonusesView_Description");

    private static string Get(string key) => PlugInResources.ResourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? string.Empty;
}
