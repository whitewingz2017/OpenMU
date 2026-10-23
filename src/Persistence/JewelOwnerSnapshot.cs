// <copyright file="JewelOwnerSnapshot.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence;

/// <summary>A storage or bank snapshot. Empty snapshots replace stale saved holdings, too.</summary>
/// <param name="OwnerId">The storage identifier, or the account identifier for banks.</param>
/// <param name="IsBank">Whether this describes the account's jewel bank.</param>
/// <param name="Amounts">The quantities in single jewel units.</param>
public sealed record JewelOwnerSnapshot(Guid OwnerId, bool IsBank, IReadOnlyList<JewelAmount> Amounts);
