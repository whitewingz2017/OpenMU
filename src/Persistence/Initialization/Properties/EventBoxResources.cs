// <copyright file="EventBoxResources.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Properties;

using System.Resources;

/// <summary>Localized captions for the event reward box update.</summary>
public static class EventBoxResources
{
    /// <summary>Gets the resource manager.</summary>
    public static ResourceManager ResourceManager => DropGroupDescriptions.ResourceManager;

    /// <summary>Gets the update name.</summary>
    public static string Name => ResourceManager.GetString("EventRewardBoxesName", System.Globalization.CultureInfo.CurrentUICulture)!;

    /// <summary>Gets the update description.</summary>
    public static string Description => ResourceManager.GetString("EventRewardBoxesDescription", System.Globalization.CultureInfo.CurrentUICulture)!;
}
