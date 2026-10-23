// <copyright file="DiscordEconomyStatus.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Discord;

using MUnique.OpenMU.Persistence;

/// <summary>Jewel totals in single jewel units, without account-identifying information.</summary>
/// <param name="Produced">Lifetime monster-ground drops.</param>
/// <param name="Held">Current player-owned jewels, including bank balances.</param>
public sealed record DiscordEconomyStatus(IReadOnlyList<JewelAmount> Produced, IReadOnlyList<JewelAmount> Held);
