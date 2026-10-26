// <copyright file="DevilSquareProgressView.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView.MiniGames;

using System.Buffers.Binary;
using Microsoft.Extensions.Logging;
using MUnique.OpenMU.GameLogic.MiniGames;
using MUnique.OpenMU.Network;
using MUnique.OpenMU.Network.Packets.ServerToClient;

/// <summary>Streams live event progress while a participant remains in Devil Square.</summary>
internal static class DevilSquareProgressView
{
    /// <summary>Sends participant snapshots until the event ends or the player leaves.</summary>
    /// <param name="player">The participant connection.</param>
    /// <param name="game">The running game.</param>
    public static async Task StreamAsync(RemotePlayer player, DevilSquareContext game)
    {
        try
        {
            while (ReferenceEquals(player.CurrentMiniGame, game) && game.State == MiniGameState.Playing)
            {
                await SendAsync(player, game.GetProgress(player)).ConfigureAwait(false);
                await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
            }

            if (ReferenceEquals(player.CurrentMiniGame, game) && game.State == MiniGameState.Ended)
            {
                await SendAsync(player, game.GetProgress(player), true).ConfigureAwait(false);
            }
        }
        catch (Exception error)
        {
            player.Logger.LogDebug(error, "Devil Square progress stream stopped for {player}.", player);
        }
    }

    private static ValueTask SendAsync(RemotePlayer player, DevilSquareProgress progress, bool ended = false)
    {
        if (player.Connection is not { } connection)
        {
            return ValueTask.CompletedTask;
        }

        const int FixedLength = 13;
        const int WaveLength = 6;
        var data = new byte[FixedLength + (progress.Waves.Count * WaveLength)];
        data[0] = 1;
        data[1] = 1;
        data[2] = progress.Level;
        data[3] = ended ? (byte)1 : (byte)0;
        BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(4), Seconds(progress.Remaining));
        BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(6), Math.Max(0, progress.Points));
        data[10] = progress.Rank;
        data[11] = progress.Players;
        data[12] = (byte)progress.Waves.Count;
        for (int i = 0; i < progress.Waves.Count; i++)
        {
            int offset = FixedLength + (i * WaveLength);
            var wave = progress.Waves[i];
            data[offset] = wave.Number;
            data[offset + 1] = wave.State;
            BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(offset + 2), Seconds(wave.StartsIn));
            BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(offset + 4), Seconds(wave.EndsIn));
        }

        return connection.SendAsync(() =>
        {
            int size = MiniGameProgressExtendedRef.GetRequiredSize(data.Length);
            var packet = new MiniGameProgressExtendedRef(connection.Output.GetSpan(size)[..size]);
            data.CopyTo(packet.Data);
            return size;
        });
    }

    private static ushort Seconds(TimeSpan value) => (ushort)Math.Clamp(Math.Ceiling(value.TotalSeconds), 0, ushort.MaxValue);
}
