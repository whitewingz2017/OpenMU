// <copyright file="StartingSkillResources.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Properties;

using System.Resources;

/// <summary>
/// Localized captions for the starting Castle Siege skill configuration.
/// </summary>
public static class StartingSkillResources
{
    private static readonly ResourceManager Resources = new(typeof(StartingSkillResources));

    /// <summary>Gets the dark wizard starting skill plug-in name.</summary>
    public static string AddLance_Name => Resources.GetString(nameof(AddLance_Name)) ?? nameof(AddLance_Name);

    /// <summary>Gets the dark wizard starting skill plug-in description.</summary>
    public static string AddLance_Description => Resources.GetString(nameof(AddLance_Description)) ?? nameof(AddLance_Description);

    /// <summary>Gets the configuration update name.</summary>
    public static string Update_Name => Resources.GetString(nameof(Update_Name)) ?? nameof(Update_Name);

    /// <summary>Gets the configuration update description.</summary>
    public static string Update_Description => Resources.GetString(nameof(Update_Description)) ?? nameof(Update_Description);
}
