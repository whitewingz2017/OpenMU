// <copyright file="GuideBookShown.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.GuideBook;

/// <summary>
/// Which guide an answer to the client contains.
/// </summary>
public enum GuideBookShown
{
    /// <summary>No guide, only the status, e.g. when a request was refused.</summary>
    None,

    /// <summary>The published guide.</summary>
    Published,

    /// <summary>The draft of the game masters.</summary>
    Draft,
}
