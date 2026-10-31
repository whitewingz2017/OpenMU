// <copyright file="MuunResources.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Muun;
/// <summary>Localized plugin metadata from the standard game resources.</summary>
public static class MuunResources
{
    /// <summary>Gets the localized name.</summary>
    public static string Name => Properties.PlugInResources.ResourceManager.GetString("Muun_Name", System.Globalization.CultureInfo.CurrentUICulture) ?? "Muun_Name";
    /// <summary>Gets the localized description.</summary>
    public static string Description => Properties.PlugInResources.ResourceManager.GetString("Muun_Description", System.Globalization.CultureInfo.CurrentUICulture) ?? "Muun_Description";
}
