// <copyright file="ExcellentDropResources.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Properties;

using System.Resources;

/// <summary>Localized captions for separate excellent drop categories.</summary>
public static class ExcellentDropResources
{
    /// <summary>Gets the shared resource manager.</summary>
    public static ResourceManager ResourceManager => DropGroupDescriptions.ResourceManager;

    /// <summary>Gets the update name.</summary>
    public static string Name => Get("SeparateExcellentDropsName");

    /// <summary>Gets the update description.</summary>
    public static string Description => Get("SeparateExcellentDropsDescription");

    /// <summary>Gets the accessory drop group caption.</summary>
    public static string ExcellentAccessoriesDropGroup => Get(nameof(ExcellentAccessoriesDropGroup));

    private static string Get(string key)
        => ResourceManager.GetString(key, System.Globalization.CultureInfo.CurrentUICulture) ?? key;
}
