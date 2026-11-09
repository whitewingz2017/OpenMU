// <copyright file="PartyAutoRejoinPlugInConfiguration.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

/// <summary>
/// Configuration for the <see cref="PartyAutoRejoinPlugIn"/>.
/// </summary>
public class PartyAutoRejoinPlugInConfiguration
{
    /// <summary>
    /// Gets or sets how long a disconnected character keeps its party slot.
    /// </summary>
    public TimeSpan ReconnectTimeout { get; set; } = TimeSpan.FromMinutes(15);
}
