// <copyright file="PvpCounterattackPayment.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.MuHelper;

using System.Buffers.Binary;
using MUnique.OpenMU.GameLogic.Views;
using MUnique.OpenMU.GameLogic.Views.MuHelper;

/// <summary>Stores the server-owned paid hour in unused helper bytes, alongside the coin deduction.</summary>
public static class PvpCounterattackPayment
{
    // Matches the explicit 30 WCoin C / hour confirmation in the mobile client.
    internal const int HourlyCost = 30;
    private const int FlagsOffset = 29;
    private const byte Enabled = 1;
    private const byte Consent = 16;
    private const int LeaseOffset = 30;
    private const int LeaseSize = 12;
    private const uint LeaseMarker = 0x43505650;
    private const int BlobLength = 257;

    /// <summary>Checks a saved, server-owned lease, never a raw client request.</summary>
    public static bool IsPaid(ReadOnlySpan<byte> saved)
        => saved.Length >= LeaseOffset + LeaseSize && (saved[FlagsOffset] & Enabled) != 0
            && BinaryPrimitives.ReadUInt32LittleEndian(saved[LeaseOffset..]) == LeaseMarker
            && BinaryPrimitives.ReadInt64LittleEndian(saved[(LeaseOffset + 4)..]) > DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    /// <summary>Saves helper settings and atomically purchases or renews a confirmed paid hour.</summary>
    public static ValueTask SaveAsync(Player player, Memory<byte> requested, bool renewal = false)
        => player.RunPersistenceExclusiveAsync(() => SaveCoreAsync(player, requested, renewal));

    /// <summary>Renews enabled settings, or disables them if payment cannot be made.</summary>
    public static async ValueTask RenewAsync(Player player)
    {
        if (player.SelectedCharacter?.MuHelperConfiguration is not { Length: >= BlobLength } saved
            || (saved[FlagsOffset] & Enabled) == 0 || IsPaid(saved))
        {
            return;
        }

        await SaveAsync(player, saved.ToArray(), true).ConfigureAwait(false);
    }

    private static async ValueTask SaveCoreAsync(Player player, Memory<byte> requested, bool renewal)
    {
        if (player.SelectedCharacter is not { } character || player.Account is not { } account || requested.Length != BlobLength)
        {
            return;
        }

        var previous = character.MuHelperConfiguration;
        // A queued renewal must not revive settings disabled or renewed while it waited for the lock.
        if (renewal && (previous is not { Length: >= BlobLength } || (previous[FlagsOffset] & Enabled) == 0 || IsPaid(previous)))
        {
            return;
        }

        var data = renewal ? previous!.ToArray() : requested.ToArray();
        var consent = (data[FlagsOffset] & Consent) != 0;
        data[FlagsOffset] &= unchecked((byte)~Consent);
        data.AsSpan(LeaseOffset, LeaseSize).Clear();
        if (previous is { Length: >= BlobLength })
        {
            previous.AsSpan(LeaseOffset, LeaseSize).CopyTo(data.AsSpan(LeaseOffset));
        }

        var balance = account.WCoinC;
        var committed = false;
        string? notice = null;
        try
        {
            var hasLease = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(LeaseOffset)) == LeaseMarker;
            if ((data[FlagsOffset] & Enabled) != 0 && !IsPaid(data))
            {
                if ((!consent && !(renewal && hasLease)) || account.WCoinC < HourlyCost)
                {
                    data[FlagsOffset] &= unchecked((byte)~Enabled);
                    notice = "PvP Counterattack disabled. Confirm the 30 WCoin C hourly charge and ensure sufficient WCoin C.";
                }
                else
                {
                    account.WCoinC -= HourlyCost;
                    BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(LeaseOffset), LeaseMarker);
                    BinaryPrimitives.WriteInt64LittleEndian(data.AsSpan(LeaseOffset + 4), DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds());
                    notice = "PvP Counterattack: 30 WCoin C charged for one hour. Renews hourly while enabled.";
                }
            }

            character.MuHelperConfiguration = data;
            committed = await player.SaveProgressAsync().ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            player.Logger.LogError(exception, "Could not save PvP Counterattack payment for {Player}", player);
        }
        finally
        {
            if (!committed)
            {
                account.WCoinC = balance;
                character.MuHelperConfiguration = previous;
                notice = "Could not save PvP Counterattack payment. No WCoin C was spent.";
            }
        }

        if (character.MuHelperConfiguration is { } result)
        {
            await player.InvokeViewPlugInAsync<IMuHelperConfigurationUpdatePlugIn>(view => view.UpdateMuHelperConfigurationAsync(result)).ConfigureAwait(false);
        }

        if (notice is not null)
        {
            await player.InvokeViewPlugInAsync<IShowMessagePlugIn>(view => view.ShowMessageAsync(notice, Interfaces.MessageType.BlueNormal)).ConfigureAwait(false);
        }
    }
}
