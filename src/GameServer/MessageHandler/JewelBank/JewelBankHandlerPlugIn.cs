// <copyright file="JewelBankHandlerPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.MessageHandler.JewelBank;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.JewelBank;
using MUnique.OpenMU.Network.Packets.ClientToServer;
using MUnique.OpenMU.PlugIns;

/// <summary>Handles the opt-in custom client's bank protocol.</summary>
[PlugIn]
[Display(Name = "Jewel Bank", Description = "Account jewel balances, deposits and withdrawals for the custom client.")]
[Guid("3a2e4ae2-6965-4598-a1d1-17d10331184f")]
internal sealed class JewelBankHandlerPlugIn : IPacketHandlerPlugIn, ISupportCustomConfiguration<JewelBankConfiguration>, ISupportDefaultCustomConfiguration
{
    private readonly JewelBankActions _actions = new();

    /// <inheritdoc />
    public byte Key => JewelBankRequestExtended.Code;

    /// <inheritdoc />
    public bool IsEncryptionExpected => false;

    /// <inheritdoc />
    public JewelBankConfiguration? Configuration { get; set; }

    /// <inheritdoc />
    public object CreateDefaultConfig() => new JewelBankConfiguration();

    /// <inheritdoc />
    public async ValueTask HandlePacketAsync(Player player, Memory<byte> packet)
    {
        if (packet.Length != JewelBankRequestExtended.Length || packet.Span[0] != 0xC1 || packet.Span[3] != JewelBankRequestExtended.SubCode)
        {
            return;
        }

        JewelBankRequestExtended request = packet;
        await this._actions.ExecuteAsync(player, this.Configuration ??= new(), request.RequestId, request.Operation, request.Item, request.Amount).ConfigureAwait(false);
    }
}
