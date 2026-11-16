// <copyright file="PartySearchResources.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PartySearch;

using System.Globalization;

/// <summary>Provides localized metadata for the Party Search plugins.</summary>
public static class PartySearchResources
{
    /// <summary>Gets the feature name.</summary>
    public static string Name => Get("PartySearch_Name");

    /// <summary>Gets the feature description.</summary>
    public static string Description => Get("PartySearch_Description");

    /// <summary>Gets the name of the plugin which joins by the party's password.</summary>
    public static string PasswordName => Get("PartySearchPassword_Name");

    /// <summary>Gets the description of the plugin which joins by the party's password.</summary>
    public static string PasswordDescription => Get("PartySearchPassword_Description");

    private static string Get(string key) => PlugInResources.ResourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? string.Empty;
}
