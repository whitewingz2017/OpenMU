// <copyright file="StartingBuffDuration.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.PlugIns.CharacterCreated;

/// <summary>
/// The duration of a starting buff. The value is the number of hours.
/// </summary>
/// <remarks>
/// The value is stored in the plugin configuration, so members may only be appended.
/// </remarks>
public enum StartingBuffDuration
{
    /// <summary>One hour.</summary>
    OneHour = 1,

    /// <summary>Three hours.</summary>
    ThreeHours = 3,

    /// <summary>Five hours.</summary>
    FiveHours = 5,

    /// <summary>One day.</summary>
    OneDay = 24,

    /// <summary>Three days.</summary>
    ThreeDays = 72,

    /// <summary>Seven days.</summary>
    SevenDays = 168,
}
