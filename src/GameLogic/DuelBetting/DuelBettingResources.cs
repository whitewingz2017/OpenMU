// <copyright file="DuelBettingResources.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.DuelBetting;

using System.Globalization;

/// <summary>Localized metadata for the experimental betting configuration.</summary>
public static class DuelBettingResources
{
    /// <summary>Gets the feature name.</summary>
    public static string Name => PlugInResources.ResourceManager.GetString("DuelBetting_Name", CultureInfo.CurrentUICulture) ?? string.Empty;

    /// <summary>Gets the feature description.</summary>
    public static string Description => PlugInResources.ResourceManager.GetString("DuelBetting_Description", CultureInfo.CurrentUICulture) ?? string.Empty;
}
