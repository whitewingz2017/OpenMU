// <copyright file="SkinShopView.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView.Inventory;

using System.Runtime.InteropServices;
using System.Text.Json;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.Cosmetics;
using MUnique.OpenMU.GameLogic.ItemBonuses;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.GameLogic.Views.Inventory;
using MUnique.OpenMU.Network;
using MUnique.OpenMU.Network.Packets.ServerToClient;
using MUnique.OpenMU.PlugIns;

/// <summary>Sends skin prices and configured item attributes to custom clients.</summary>
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

        var data = Serialize(this._player, requestId, status, includeCatalog);
        await connection.SendAsync(() =>
        {
            var size = SkinShopResponseExtendedRef.GetRequiredSize(data.Length);
            var response = new SkinShopResponseExtendedRef(connection.Output.GetSpan(size)[..size]);
            data.CopyTo(response.Data);
            return size;
        }).ConfigureAwait(false);
    }

    /// <summary>Serializes the catalog with purchase attributes, independently of the tooltip feature.</summary>
    /// <param name="player">The receiving player.</param>
    /// <param name="requestId">The request identifier.</param>
    /// <param name="status">The result text.</param>
    /// <param name="includeCatalog">Whether the catalog is requested.</param>
    /// <returns>The response JSON.</returns>
    internal static byte[] Serialize(Player player, uint requestId, string status, bool includeCatalog)
    {
        var settings = SkinShopFeaturePlugIn.GetSettings(player.GameContext);
        var items = includeCatalog && settings is not null
            ? player.GameContext.Configuration.Items.Where(FenrirMountRules.IsShopItem).OrderBy(d => d.Group).ThenBy(d => d.Number).ToArray()
            : [];

        // Share identical attribute lists so repeated wing/set attributes fit the C2 packet size.
        var attributePools = items.Select(definition => (Definition: definition, Lines: ItemBonusActions.Lines(definition)))
            .GroupBy(item => JsonSerializer.Serialize(item.Lines)).ToArray();
        var attributeIndices = attributePools.SelectMany((pool, index) => pool.Select(item => (item.Definition, Index: index)))
            .ToDictionary(item => item.Definition, item => item.Index);
        var rentalsEnabled = player.GameContext.PlugInManager.IsPlugInActive(typeof(ItemExpirationPlugIn));
        return JsonSerializer.SerializeToUtf8Bytes(new
        {
            RequestId = requestId,
            Status = status,
            Balance = player.Account?.WCoinC ?? 0,
            GoblinBalance = player.Account?.GoblinPoints ?? 0,
            RentalOneDayPrice = rentalsEnabled ? Math.Max(0, settings?.RentalOneDayPrice ?? 0) : 0,
            RentalThreeDaysPrice = rentalsEnabled ? Math.Max(0, settings?.RentalThreeDaysPrice ?? 0) : 0,
            RentalSevenDaysPrice = rentalsEnabled ? Math.Max(0, settings?.RentalSevenDaysPrice ?? 0) : 0,
            RentableItems = items.Where(d => rentalsEnabled && settings is not null && (settings.RentalPrice(d, 1) > 0 || settings.RentalPrice(d, 3) > 0 || settings.RentalPrice(d, 7) > 0)).Select(d => (d.Group * 512) + d.Number),
            ExcellentEffectItems = settings?.ExcellentEffectItems(player.GameContext.Configuration.Items).ToArray() ?? [],
            Items = items.Select(d => new { ItemType = (d.Group * 512) + d.Number, Name = d.Name.ValueInNeutralLanguage, Part = FenrirMountRules.IsCustom(d) ? 8 : CosmeticArmorRules.GetSlot(d) - CosmeticArmorRules.FirstSlot, Price = settings!.Price(d), GoblinPrice = settings.GoblinPrice(d), Eligible = CosmeticArmorRules.CanPurchase(player, d), Pool = Pool(d), AttributePool = attributeIndices[d] }),
            Pools = items.GroupBy(Pool).Select(group => new { Pool = group.Key, Bonuses = Array.Empty<object>() }),
            AttributePools = attributePools.Select(pool => pool.First().Lines),
        });
    }

    private static int Pool(ItemDefinition definition) => definition.Group switch { 6 or 8 => 8, 5 => 5, 12 => 12, 13 => 13, _ => 0 };
}
