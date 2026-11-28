// <copyright file="SkinAuraColor.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Cosmetics;

/// <summary>A skin aura color. The numeric values match the client palette.</summary>
public enum SkinAuraColor
{
    /// <summary>Use the original aura default for this set.</summary>
    Default = -2,

    /// <summary>Disable the aura.</summary>
    Off = -1,

    /// <summary>Green.</summary>
    Green = 0,

    /// <summary>Red.</summary>
    Red = 1,

    /// <summary>Violet.</summary>
    Violet = 2,

    /// <summary>Blue.</summary>
    Blue = 3,

    /// <summary>Black.</summary>
    Black = 4,

    /// <summary>White.</summary>
    White = 5,

    /// <summary>Pink.</summary>
    Pink = 6,

    /// <summary>Gold.</summary>
    Gold = 7,

    /// <summary>Yellow.</summary>
    Yellow = 8,
}
