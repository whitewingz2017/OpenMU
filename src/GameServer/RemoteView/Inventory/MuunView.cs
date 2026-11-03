// <copyright file="MuunView.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView.Inventory;

using System.Runtime.InteropServices;
using System.Text.Json;
using MUnique.OpenMU.AttributeSystem;
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
    /// <summary>Tells the client that a Muun has no expiration.</summary>
    private const long NeverExpires = -1;
    private readonly RemotePlayer _player;
    /// <summary>Initializes the receiving player's view.</summary>
    public MuunView(RemotePlayer player) => this._player = player;
    /// <inheritdoc />
    public async ValueTask ShowAsync(uint requestId, string status)
    {
        if (this._player.Connection is not { } connection) return;
        var config = MuunFeaturePlugIn.Settings(this._player.GameContext);
        var now = DateTime.UtcNow;
        var data = JsonSerializer.SerializeToUtf8Bytes(new
        {
            RequestId = requestId,
            Status = status == "Ready" ? status : this._player.GetLocalizedMessage(status),
            Enabled = config is not null,
            SupportsSlotMoves = true,
            Capacity = config?.Capacity ?? 0,
            Items = this._player.SelectedCharacter?.MuunInventory?.Items.OrderBy(i => i.ItemSlot).Select(i => new
            {
                Slot = i.ItemSlot, Type = MuunRules.Type(i), Name = i.Definition!.Name.ValueInNeutralLanguage,
                Level = i.Level, Life = i.Durability, MaximumLife = i.Definition!.Durability,
                Active = MuunRules.Find(config, MuunRules.Type(i)) is { } rule && MuunRules.IsActive(this._player, i, rule),
                // Unconfigured species report no reason; Active=false already tells the client.
                Inactive = (int)(MuunRules.Find(config, MuunRules.Type(i)) is { } found ? MuunRules.InactiveReason(this._player, i, found, now) : MuunInactiveReason.None),
                ExpiresInSeconds = i.ExpiresAtUtc is { } end ? (long)Math.Max(0, (end - now).TotalSeconds) : NeverExpires,
            }),
            Species = config?.Pets.Take(120).Select(p => new { Type = p.ItemType, p.MaximumLevel, p.EvolutionItemType, p.EvolutionStoneType, p.RepairItemType,
                Name = this.SpeciesName(p.ItemType), Description = this.SpeciesDescription(p),
                Owned = this._player.SelectedCharacter?.MuunInventory?.Items.Any(i => MuunRules.Type(i) == p.ItemType) == true }),
            Exchanges = config?.Exchanges.Take(64).Select((p, i) => new { Index = i, Name = p.Name[..Math.Min(p.Name.Length, 80)], p.MaterialItemType, p.RequiredCount }),
        });
        if (data.Length > ushort.MaxValue - 5)
        {
            // Keep transfers available even if an administrator configures an oversized catalogue.
            data = JsonSerializer.SerializeToUtf8Bytes(new {
                RequestId = requestId, Status = this._player.GetLocalizedMessage("MuunCatalogueTooLarge"), Enabled = config is not null,
                SupportsSlotMoves = true,
                Capacity = config?.Capacity ?? 0,
                Items = this._player.SelectedCharacter?.MuunInventory?.Items.Select(i => new {
                    Slot = i.ItemSlot, Type = MuunRules.Type(i), Name = "Muun", Level = i.Level, Life = i.Durability,
                    MaximumLife = i.Definition!.Durability, Active = false }),
                Species = config?.Pets.Take(120).Select(p => new { Type = p.ItemType, p.MaximumLevel, p.EvolutionItemType, p.EvolutionStoneType, p.RepairItemType })
            });
        }
        await connection.SendAsync(() =>
        {
            var size = MuunResponseExtendedRef.GetRequiredSize(data.Length);
            var packet = new MuunResponseExtendedRef(connection.Output.GetSpan(size)[..size]);
            data.CopyTo(packet.Data); return size;
        }).ConfigureAwait(false);
    }
    private string SpeciesName(int type)
    {
        var name = this._player.GameContext.Configuration.Items.FirstOrDefault(d => d.Group * 512 + d.Number == type)?.Name.ValueInNeutralLanguage ?? type.ToString();
        return name[..Math.Min(name.Length, 80)];
    }
    private string SpeciesDescription(MuunDefinition rule)
    {
        var item = this._player.GameContext.Configuration.Items.FirstOrDefault(d => d.Group * 512 + d.Number == rule.ItemType);
        var bonuses = string.Join("; ", item?.BasePowerUpAttributes.Take(4).Select(b => FormattableString.Invariant($"{b.TargetAttribute?.GetDisplayName()}: {b.BaseValue:0.####} ({b.AggregateType})")) ?? []);
        var levels = rule.MaximumCharacterLevel > 0
            ? FormattableString.Invariant($"Character level {rule.MinimumCharacterLevel}-{rule.MaximumCharacterLevel}")
            : FormattableString.Invariant($"Min character level {rule.MinimumCharacterLevel}");
        var days = rule.Days.Count > 0 ? "; " + string.Join(",", rule.Days.Select(d => d.ToString()[..3])) : string.Empty;
        var hours = rule.FromHourUtc != 0 || rule.UntilHourUtc != MuunRules.HoursPerDay
            ? FormattableString.Invariant($"; {rule.FromHourUtc:00}-{rule.UntilHourUtc:00} UTC") : string.Empty;
        var description = FormattableString.Invariant($"Max +{rule.MaximumLevel}; {levels}{days}{hours}; Combat {rule.AttackDamageFactor:P0}, {rule.AttackRange} tiles/{rule.AttackIntervalMilliseconds}ms; {bonuses}");
        return description[..Math.Min(description.Length, 256)];
    }
    /// <inheritdoc />
    public async ValueTask AttackAsync(Player owner, IAttackable target, byte slot)
    {
        if (this._player.Connection is not { } connection) return;
        var data = JsonSerializer.SerializeToUtf8Bytes(new { Id = owner.GetId(this._player), Name = owner.Name, AttackTarget = target.GetId(this._player), MuunSlot = slot });
        await connection.SendAsync(() =>
        {
            var size = MuunAppearanceExtendedRef.GetRequiredSize(data.Length);
            var packet = new MuunAppearanceExtendedRef(connection.Output.GetSpan(size)[..size]);
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
