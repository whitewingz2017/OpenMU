// <copyright file="JewelStatisticsSnapshot.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence;

/// <summary>The saved holdings and lifetime monster drop totals.</summary>
/// <param name="Owners">The saved storage and bank balances.</param>
/// <param name="Produced">The cumulative monster drops.</param>
public sealed record JewelStatisticsSnapshot(IReadOnlyList<JewelOwnerSnapshot> Owners, IReadOnlyList<JewelAmount> Produced);
