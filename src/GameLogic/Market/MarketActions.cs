// <copyright file="MarketActions.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Market;

using System.Text.Json;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.Views;
using MUnique.OpenMU.GameLogic.Views.Inventory;

/// <summary>Processes the Marketplace requests of the client and answers them with a page of the market.</summary>
public sealed class MarketActions
{
    /// <summary>Processes a request and answers it.</summary>
    /// <param name="player">The player.</param>
    /// <param name="requestId">The request identifier which the answer carries.</param>
    /// <param name="operation">The <see cref="MarketOperation"/>.</param>
    /// <param name="data">The UTF-8 JSON of the <see cref="MarketRequestData"/>.</param>
    /// <param name="serializeItem">Serializes an item as the client's inventory list carries it, hex.</param>
    /// <returns>The completion of the request and its answer.</returns>
    public async ValueTask ExecuteAsync(Player player, uint requestId, byte operation, ReadOnlyMemory<byte> data, Func<Item, string> serializeItem)
    {
        var request = Read(data);
        if (MarketFeaturePlugIn.Get(player.GameContext) is not { } plugIn)
        {
            await ShowAsync(player, new MarketAnswer { RequestId = requestId, Result = MarketResult.Disabled }).ConfigureAwait(false);
            return;
        }

        var settings = plugIn.Settings;
        var result = await ProcessAsync(player, plugIn, (MarketOperation)operation, request, serializeItem).ConfigureAwait(false);
        var listings = await plugIn.Registry.GetAsync(player.GameContext, settings).ConfigureAwait(false);
        var answer = MarketPages.Create(player, settings, listings, request);
        answer.RequestId = requestId;
        answer.Result = result;
        await ShowAsync(player, answer).ConfigureAwait(false);
        if (result is MarketResult.Listed or MarketResult.Bought)
        {
            await player.InvokeViewPlugInAsync<IUpdateInventoryListPlugIn>(v => v.UpdateInventoryListAsync()).ConfigureAwait(false);
        }
    }

    private static async ValueTask<MarketResult> ProcessAsync(Player player, MarketFeaturePlugIn plugIn, MarketOperation operation, MarketRequestData? request, Func<Item, string> serializeItem)
    {
        if (operation == MarketOperation.Load)
        {
            return MarketResult.None;
        }

        if (request is null || operation > MarketOperation.Cancel)
        {
            return MarketResult.Invalid;
        }

        var check = Check(player, plugIn.Settings);
        if (check != MarketResult.None)
        {
            return check;
        }

        return await player.RunPersistenceExclusiveAsync(() => WriteAsync(player, plugIn, operation, request, serializeItem)).ConfigureAwait(false);
    }

    private static async ValueTask<MarketResult> WriteAsync(Player player, MarketFeaturePlugIn plugIn, MarketOperation operation, MarketRequestData request, Func<Item, string> serializeItem)
    {
        await plugIn.Registry.WriteLock.WaitAsync().ConfigureAwait(false);
        try
        {
            var result = operation switch
            {
                MarketOperation.List => await MarketListingCreation.ListAsync(
                    player,
                    plugIn.Settings,
                    await plugIn.Registry.GetAsync(player.GameContext, plugIn.Settings).ConfigureAwait(false),
                    request,
                    serializeItem).ConfigureAwait(false),
                MarketOperation.Buy => await MarketPurchase.BuyAsync(player, plugIn.Settings, request.Listing).ConfigureAwait(false),
                _ => await MarketReturn.CancelAsync(player, request.Listing).ConfigureAwait(false),
            };

            if (result is MarketResult.Listed or MarketResult.Bought or MarketResult.Cancelled)
            {
                plugIn.Registry.Invalidate();
            }

            return result;
        }
        finally
        {
            plugIn.Registry.WriteLock.Release();
        }
    }

    /// <summary>Checks that the player may write to the market now.</summary>
    private static MarketResult Check(Player player, MarketSettings settings)
    {
        if (player.Account is null || player.SelectedCharacter is null || player.Inventory is null)
        {
            return MarketResult.Invalid;
        }

        if (player.PlayerState.CurrentState != PlayerState.EnteredWorld || player.ShopStorage?.StoreOpen == true
            || player.OpenedNpc is not null || player.TradingPartner is not null || player.IsCashShopOpen
            || player.TemporaryStorage?.Items.Any() == true)
        {
            return MarketResult.Busy;
        }

        return (player.Attributes?[Stats.Level] ?? 0) < settings.MinimumLevel ? MarketResult.LevelTooLow : MarketResult.None;
    }

    private static ValueTask ShowAsync(Player player, MarketAnswer answer)
        => player.InvokeViewPlugInAsync<IMarketView>(view => view.ShowAsync(answer));

    private static MarketRequestData? Read(ReadOnlyMemory<byte> data)
    {
        if (data.IsEmpty)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<MarketRequestData>(data.Span);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
