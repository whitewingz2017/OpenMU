// <copyright file="JewelStorageRow.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.EntityFramework;

/// <summary>A grouped projection; no player inventories are materialized for reporting.</summary>
internal sealed class JewelStorageRow
{
    /// <summary>Gets or sets the storage identifier.</summary>
    public Guid OwnerId { get; set; }

    /// <summary>Gets or sets the single jewel code.</summary>
    public int ItemCode { get; set; }

    /// <summary>Gets or sets the quantity.</summary>
    public long Amount { get; set; }
}
