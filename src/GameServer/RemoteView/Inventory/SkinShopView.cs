// <copyright file="SkinShopView.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView.Inventory;

using System.Runtime.InteropServices;
using System.Text.Json;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.GameLogic.Cosmetics;
using MUnique.OpenMU.GameLogic.Views.Inventory;
using MUnique.OpenMU.Network;
using MUnique.OpenMU.Network.Packets.ServerToClient;
using MUnique.OpenMU.PlugIns;

/// <summary>Sends skin prices and the configured standard bonus definitions to custom clients.</summary>
[PlugIn]
[Display(Name = "Skin Shop View", Description = "Skin shop catalog and purchase confirmations.")]
[Guid("882a06fc-921c-4859-9501-f7e94b58e228")]
public sealed class SkinShopView : ISkinShopView
{
    private readonly RemotePlayer _player;

    /// <summary>Initializes a new instance of the <see cref="SkinShopView"/> class.</summary>
    /// <param name="player">The receiving player.</param>
    public SkinShopView(RemotePlayer player) => this._player = player;

    /// <inheritdoc />
    public async ValueTask ShowAsync(uint requestId, string status, bool includeCatalog)
    {
        if (this._player.Connection is not { } connection)
        {
            return;
        }

        var settings = SkinShopFeaturePlugIn.GetSettings(this._player.GameContext);
        var items = includeCatalog && settings is not null
            ? this._player.GameContext.Configuration.Items.Where(FenrirMountRules.IsShopItem).OrderBy(d => d.Group).ThenBy(d => d.Number).ToArray()
            : [];
        var data = JsonSerializer.SerializeToUtf8Bytes(new
        {
            RequestId = requestId,
            Status = status,
            Balance = this._player.Account?.WCoinC ?? 0,
            Items = items.Select(d => new { ItemType = (d.Group * 512) + d.Number, Name = d.Name.ValueInNeutralLanguage, Part = FenrirMountRules.IsCustom(d) ? 8 : CosmeticArmorRules.GetSlot(d) - CosmeticArmorRules.FirstSlot, Price = settings!.Price(d), Eligible = CosmeticArmorRules.CanEquip(this._player, d), Pool = Pool(d) }),
            Pools = items.GroupBy(Pool).Select(group => new { Pool = group.Key, Bonuses = SkinBonuses.Choices(group.First()).OrderBy(o => o.Number).Select(o => new { Number = o.Number, Label = BonusLabel(o) }) }),
        });
        await connection.SendAsync(() =>
        {
            var size = SkinShopResponseExtendedRef.GetRequiredSize(data.Length);
            var response = new SkinShopResponseExtendedRef(connection.Output.GetSpan(size)[..size]);
            data.CopyTo(response.Data);
            return size;
        }).ConfigureAwait(false);
    }

    private static string BonusLabel(IncreasableItemOption option)
    {
        if (option.PowerUpDefinition is not { Boost.ConstantValue: { } value } powerUp || powerUp.Boost.RelatedValues.Count > 0)
        {
            return option.PowerUpDefinition?.ToString() ?? option.ToString();
        }

        var amount = value.AggregateType == AggregateType.Multiplicate ? (value.Value - 1) * 100
            : value.Value is > 0 and < 1 ? value.Value * 100 : value.Value;
        var suffix = value.AggregateType == AggregateType.Multiplicate || value.Value is > 0 and < 1 ? "%" : string.Empty;
        return FormattableString.Invariant($"{powerUp.TargetAttribute?.GetDisplayName()} +{amount:0.##}{suffix}");
    }

    private static int Pool(ItemDefinition definition) => definition.Group switch { 6 or 8 => 8, 5 => 5, 12 => 12, 13 => 13, _ => 0 };
}
