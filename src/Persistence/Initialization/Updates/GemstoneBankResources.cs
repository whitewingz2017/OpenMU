// <copyright file="GemstoneBankResources.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Globalization;
using MUnique.OpenMU.Persistence.Initialization.Properties;

/// <summary>Provides localized metadata for the Gemstone bank update.</summary>
public static class GemstoneBankResources
{
    /// <summary>Gets the configuration update name.</summary>
    public static string Name => PlugInResources.ResourceManager.GetString("GemstoneBank_Name", CultureInfo.CurrentUICulture) ?? string.Empty;

    /// <summary>Gets the configuration update description.</summary>
    public static string Description => PlugInResources.ResourceManager.GetString("GemstoneBank_Description", CultureInfo.CurrentUICulture) ?? string.Empty;
}
