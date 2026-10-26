// <copyright file="DevilSquareProgress.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.MiniGames;

/// <summary>A server-authoritative snapshot for the mobile event journal.</summary>
public sealed record DevilSquareProgress(byte Level, TimeSpan Remaining, int Points, byte Rank, byte Players, IReadOnlyList<DevilSquareProgress.Wave> Waves)
{
    /// <summary>A configured wave, with its actual activation state and countdowns.</summary>
    public sealed record Wave(byte Number, byte State, TimeSpan StartsIn, TimeSpan EndsIn);
}
