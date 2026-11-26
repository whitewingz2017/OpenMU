// <copyright file="DuelBetEscrow.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.DataModel.Entities;

using System.ComponentModel.DataAnnotations;

/// <summary>Stakes held for one duel, or its outstanding account payouts after settlement.</summary>
[AggregateRoot]
public class DuelBetEscrow
{
    /// <summary>Gets or sets the serialized stakes or outstanding payouts.</summary>
    public string Entries { get; set; } = "[]";

    /// <summary>Gets or sets when the match's last lease expires and its stakes become refundable.</summary>
    public DateTime LeaseUntil { get; set; }

    /// <summary>Gets or sets a value indicating whether the entries are settled payouts.</summary>
    public bool Settled { get; set; }

    /// <summary>Gets or sets the revision guarding settlement and claims from multiple game servers.</summary>
    [ConcurrencyCheck]
    public Guid Revision { get; set; }
}
