// <copyright file="GuideBookOperation.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.GuideBook;

/// <summary>
/// The operations of a Guide Book request. The values are sent by the client.
/// </summary>
public enum GuideBookOperation : byte
{
    /// <summary>Loads the published guide.</summary>
    Load = 0,

    /// <summary>Loads the draft; game masters only.</summary>
    LoadDraft = 1,

    /// <summary>Saves the whole draft; game masters only.</summary>
    SaveDraft = 2,

    /// <summary>Publishes the draft to every player; game masters only.</summary>
    Publish = 3,
}
