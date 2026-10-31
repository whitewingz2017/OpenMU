// <copyright file="MuunView.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView.Inventory;

using System.Runtime.InteropServices;
using System.Text.Json;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.Muun;
using MUnique.OpenMU.GameLogic.Views.Inventory;
using MUnique.OpenMU.Network;
using MUnique.OpenMU.GameLogic.Views;
using MUnique.OpenMU.GameServer.RemoteView.World;
using MUnique.OpenMU.Network.Packets.ServerToClient;
using MUnique.OpenMU.PlugIns;

/// <summary>Custom-client Muun collection and visible companion packets.</summary>
[PlugIn]
[Display(Name = nameof(MuunResources.Name), Description = nameof(MuunResources.Description), ResourceType = typeof(MuunResources))]
[Guid("7f59c44b-2dc2-4b15-92be-8ad70b5ffac9")]
public sealed class MuunView : IMuunView
{
    private readonly RemotePlayer _player;
    /// <summary>Initializes the receiving player's view.</summary>
    public MuunView(RemotePlayer player) => this._player = player;
    /// <inheritdoc />
    public async ValueTask ShowAsync(uint requestId, string status)
    {
        if (this._player.Connection is not { } connection) return;
        var config = MuunFeaturePlugIn.Settings(this._player.GameContext);
        var data = JsonSerializer.SerializeToUtf8Bytes(new
        {
            RequestId = requestId,
            Status = status == "Ready" ? status : this._player.GetLocalizedMessage(status),
            Enabled = config is not null,
            Capacity = config?.Capacity ?? 0,
            Items = this._player.SelectedCharacter?.MuunInventory?.Items.OrderBy(i => i.ItemSlot).Select(i => new
            {
                Slot = i.ItemSlot, Type = MuunRules.Type(i), Name = i.Definition!.Name.ValueInNeutralLanguage,
                Level = i.Level, Life = i.Durability, MaximumLife = i.Definition!.Durability,
                Active = MuunRules.Find(config, MuunRules.Type(i)) is { } rule && MuunRules.IsActive(this._player, i, rule),
            }),
            Species = config?.Pets.Take(120).Select(p => new { Type = p.ItemType, p.MaximumLevel, p.EvolutionItemType, p.EvolutionStoneType, p.RepairItemType }),
        });
        await connection.SendAsync(() =>
        {
            var size = MuunResponseExtendedRef.GetRequiredSize(data.Length);
            var packet = new MuunResponseExtendedRef(connection.Output.GetSpan(size)[..size]);
            data.CopyTo(packet.Data); return size;
        }).ConfigureAwait(false);
    }
    /// <inheritdoc />
    public async ValueTask AppearanceAsync(Player owner)
    {
        if (this._player.Connection is not { } connection) return;
        var config = MuunFeaturePlugIn.Settings(owner.GameContext);
        var pets = new[] { -1, -1 };
        foreach (var item in owner.SelectedCharacter?.MuunInventory?.Items ?? [])
        {
            if (item.ItemSlot < pets.Length && MuunRules.Find(config, MuunRules.Type(item)) is not null
                && item.Durability > 0 && (item.ExpiresAtUtc is null || item.ExpiresAtUtc > DateTime.UtcNow)) pets[item.ItemSlot] = MuunRules.Type(item);
        }
        var data = JsonSerializer.SerializeToUtf8Bytes(new { Id = owner.GetId(this._player), Name = owner.Name, Pets = pets });
        await connection.SendAsync(() =>
        {
            var size = MuunAppearanceExtendedRef.GetRequiredSize(data.Length);
            var packet = new MuunAppearanceExtendedRef(connection.Output.GetSpan(size)[..size]);
            data.CopyTo(packet.Data); return size;
        }).ConfigureAwait(false);
    }
}
