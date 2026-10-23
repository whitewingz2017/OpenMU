// <copyright file="DiscordEventTimer.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Discord;

/// <summary>A mini game countdown on one game server.</summary>
/// <param name="Name">The mini game type.</param>
/// <param name="ServerId">The hosting game server.</param>
/// <param name="Phase">The current phase.</param>
/// <param name="DeadlineUtc">The scheduled opening or entrance-closing time.</param>
public sealed record DiscordEventTimer(string Name, byte ServerId, DiscordEventPhase Phase, DateTime? DeadlineUtc);
