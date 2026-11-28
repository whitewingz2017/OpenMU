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
    // This import requires client assets and would push the legacy catalog over its packet limit.
    private const int DevilzMuWingGroup = 12;
    private const int FirstDevilzMuWing = 400;
    private const int LastDevilzMuWing = 504;

    private readonly RemotePlayer _player;

    /// <summary>Initializes a new instance of the <see cref="SkinShopView"/> class.</summary>
    /// <param name="player">The receiving player.</param>
    public SkinShopView(RemotePlayer player) => this._player = player;

    /// <inheritdoc />
    public async ValueTask ShowAsync(uint requestId, string status, bool includeCatalog, bool compactCatalog = false)
    {
        if (this._player.Connection is not { } connection)
        {
            return;
        }

        var data = Serialize(this._player, requestId, status, includeCatalog, compactCatalog);
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
    /// <param name="compactCatalog">Whether the client accepts compact item rows.</param>
    /// <returns>The response JSON.</returns>
    internal static byte[] Serialize(Player player, uint requestId, string status, bool includeCatalog, bool compactCatalog = false)
    {
        var settings = SkinShopFeaturePlugIn.GetSettings(player.GameContext);
        var items = includeCatalog && settings is not null
            ? player.GameContext.Configuration.Items.Where(d => FenrirMountRules.IsShopItem(d)
                && (compactCatalog || !IsDevilzMuWing(d))).OrderBy(d => d.Group).ThenBy(d => d.Number).ToArray()
            : [];

        // Share identical attribute lists so repeated wing/set attributes fit the C2 packet size.
        var attributePools = items.Select(definition => (Definition: definition, Lines: ItemBonusActions.Lines(definition)))
            .GroupBy(item => JsonSerializer.Serialize(item.Lines)).ToArray();
        var attributeIndices = attributePools.SelectMany((pool, index) => pool.Select(item => (item.Definition, Index: index)))
            .ToDictionary(item => item.Definition, item => item.Index);
        var rentalsEnabled = settings is not null && player.GameContext.PlugInManager.IsPlugInActive(typeof(ItemExpirationPlugIn));
        var defaults = rentalsEnabled ? (
            One: Math.Max(0, settings!.RentalOneDayPrice),
            Three: Math.Max(0, settings.RentalThreeDaysPrice),
            Seven: Math.Max(0, settings.RentalSevenDaysPrice)) : (One: 0, Three: 0, Seven: 0);
        (List<int> DefaultItems, List<int[]> CustomQuotes) rentals = rentalsEnabled
            ? BuildRentalCatalog(items, settings!, defaults, compactCatalog)
            : ([], []);
        var offers = items.Select(d => new
        {
            ItemType = (d.Group * 512) + d.Number,
            Name = d.Name.ValueInNeutralLanguage,
            Part = FenrirMountRules.IsCustom(d) ? 8 : CosmeticArmorRules.GetSlot(d) - CosmeticArmorRules.FirstSlot,
            Price = settings!.Price(d),
            GoblinPrice = settings.GoblinPrice(d),
            Eligible = CosmeticArmorRules.CanPurchase(player, d),
            Pool = Pool(d),
            AttributePool = attributeIndices[d],
        });
        return JsonSerializer.SerializeToUtf8Bytes(new
        {
            RequestId = requestId,
            Status = status,
            Balance = player.Account?.WCoinC ?? 0,
            GoblinBalance = player.Account?.GoblinPoints ?? 0,
            RentalOneDayPrice = defaults.One,
            RentalThreeDaysPrice = defaults.Three,
            RentalSevenDaysPrice = defaults.Seven,
            RentableItems = rentals.DefaultItems,
            RentalQuotes = rentals.CustomQuotes,
            SkinAuraColors = settings?.AuraColors(player.GameContext.Configuration.Items).ToArray() ?? [],
            ExcellentEffectItems = settings?.ExcellentEffectItems(player.GameContext.Configuration.Items).ToArray() ?? [],
            Items = compactCatalog
                ? (object)offers.Select(offer => new object?[] { offer.ItemType, offer.Name, offer.Part, offer.Price,
                    offer.GoblinPrice, offer.Eligible, offer.Pool, offer.AttributePool })
                : offers,
            Pools = items.GroupBy(Pool).Select(group => new { Pool = group.Key, Bonuses = Array.Empty<object>() }),
            AttributePools = attributePools.Select(pool => pool.First().Lines),
        });
    }

    private static (List<int> DefaultItems, List<int[]> CustomQuotes) BuildRentalCatalog(
        IEnumerable<ItemDefinition> items,
        SkinShopSettings settings,
        (int One, int Three, int Seven) defaults,
        bool compactCatalog)
    {
        var defaultItems = new List<int>();
        var customQuotes = new List<int[]>();
        foreach (var definition in items)
        {
            var quote = (One: settings.RentalPrice(definition, 1), Three: settings.RentalPrice(definition, 3), Seven: settings.RentalPrice(definition, 7));
            if (quote == (0, 0, 0))
            {
                continue;
            }

            var type = (definition.Group * 512) + definition.Number;
            if (quote == defaults)
            {
                defaultItems.Add(type);
            }
            else if (compactCatalog)
            {
                // Older clients can still rent items with default prices from RentableItems.
                customQuotes.Add([type, quote.One, quote.Three, quote.Seven]);
            }
        }

        return (defaultItems, customQuotes);
    }

    private static bool IsDevilzMuWing(ItemDefinition definition)
        => definition.Group == DevilzMuWingGroup && definition.Number is >= FirstDevilzMuWing and <= LastDevilzMuWing;

    private static int Pool(ItemDefinition definition) => definition.Group switch { 6 or 8 => 8, 5 => 5, 12 => 12, 13 => 13, _ => 0 };
}
