// <copyright file="JewelDropTotal.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.EntityFramework;

/// <summary>Global telemetry, keyed by item code across game configurations and game servers.</summary>
internal sealed class JewelDropTotal
{
    /// <summary>Gets or sets the single jewel code. It is an item type, not an entity identifier.</summary>
    public int ItemCode { get; set; }

    /// <summary>Gets or sets the cumulative quantity.</summary>
    public long Total { get; set; }

    /// <summary>Gets or sets when tracking of this jewel first began.</summary>
    public DateTime TrackingSinceUtc { get; set; }
}
