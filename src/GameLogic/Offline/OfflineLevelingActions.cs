// <copyright file="OfflineLevelingActions.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Offline;

using System.Text.Json;
using MUnique.OpenMU.GameLogic.Views;

/// <summary>The offline leveling window's requests: the offer, and the start of a paid session.</summary>
public sealed class OfflineLevelingActions
{
    /// <summary>Sends the offer and asks the client to open the window, for /offlevel.</summary>
    /// <param name="player">The player.</param>
    /// <param name="settings">The settings.</param>
    /// <returns>The completion.</returns>
    public static ValueTask ShowOfferAsync(Player player, OfflineLevelingSettings settings)
    {
        var answer = CreateOffer(player, settings);
        answer.Open = true;
        return player.InvokeViewPlugInAsync<IOfflineLevelingView>(view => view.ShowAsync(answer));
    }

    /// <summary>Processes a request and answers it.</summary>
    /// <param name="player">The player.</param>
    /// <param name="requestId">The request identifier which the answer carries.</param>
    /// <param name="operation">The <see cref="OfflineLevelingOperation"/>.</param>
    /// <param name="data">The UTF-8 JSON of the <see cref="OfflineLevelingPlan"/>, for a start.</param>
    /// <returns>The completion.</returns>
    public async ValueTask ExecuteAsync(Player player, uint requestId, byte operation, ReadOnlyMemory<byte> data)
    {
        if (OfflineLevelingFeaturePlugIn.GetSettings(player.GameContext) is not { } settings)
        {
            await ShowAsync(player, new OfflineLevelingAnswer { RequestId = requestId, Result = OfflineLevelingResult.Disabled }).ConfigureAwait(false);
            return;
        }

        if ((OfflineLevelingOperation)operation != OfflineLevelingOperation.Start)
        {
            await ShowAsync(player, requestId, OfflineLevelingResult.None, settings).ConfigureAwait(false);
            return;
        }

        var plan = Read(data);
        var result = plan is null ? OfflineLevelingResult.Invalid : Check(player, settings, plan);
        if (result != OfflineLevelingResult.None)
        {
            await ShowAsync(player, requestId, result, settings).ConfigureAwait(false);
            return;
        }

        await ShowAsync(player, requestId, OfflineLevelingResult.Started, settings).ConfigureAwait(false);
        if (!await player.GameContext.OfflinePlayerManager.StartAsync(player, player.Account!.LoginName, plan).ConfigureAwait(false)
            && player.IsConnected)
        {
            await ShowAsync(player, requestId, OfflineLevelingResult.Failed, settings).ConfigureAwait(false);
        }
    }

    private static OfflineLevelingResult Check(Player player, OfflineLevelingSettings settings, OfflineLevelingPlan plan)
    {
        var rules = OfflineLevelingRules.Check(player);
        if (rules != OfflineLevelingResult.None)
        {
            return rules;
        }

        if (!OfflineLevelingCosts.IsAllowed(settings, plan))
        {
            return OfflineLevelingResult.NotAllowed;
        }

        return OfflineLevelingCosts.CanPay(player, OfflineLevelingCosts.StartCost(player, settings, plan))
            ? OfflineLevelingResult.None
            : OfflineLevelingResult.NotEnough;
    }

    private static OfflineLevelingAnswer CreateOffer(Player player, OfflineLevelingSettings settings)
    {
        var levelBasedZen = OfflineLevelingCosts.IsLevelBasedZen(settings, OfflineLevelingCurrency.Zen);
        return new OfflineLevelingAnswer
        {
            Currencies = Enum.GetValues<OfflineLevelingCurrency>()
                .Where(currency => OfflineLevelingCosts.IsAllowed(settings, currency))
                .Select(currency => CreateCurrencyOffer(player, settings, currency))
                .ToList(),
            AllowProtection = settings.AllowProtection,
            MaximumHours = settings.MaximumHours,
            LevelBasedZen = levelBasedZen,
            ZenStart = levelBasedZen ? OfflineLevelingCosts.LevelBasedStartZen(player) : 0,
            ZenByHours = levelBasedZen && settings.AllowZen
                ? Enumerable.Range(1, settings.MaximumHours).Select(hours => OfflineLevelingCosts.LevelBasedZen(player, TimeSpan.FromHours(hours))).ToList()
                : [],
        };
    }

    private static OfflineLevelingCurrencyOffer CreateCurrencyOffer(Player player, OfflineLevelingSettings settings, OfflineLevelingCurrency currency)
    {
        var protection = OfflineLevelingCosts.ProtectionPerHour(settings, currency);
        return new OfflineLevelingCurrencyOffer
        {
            Currency = currency,
            PerHour = OfflineLevelingCosts.LevelingPerHour(player, settings, currency).Amount,
            Balance = OfflineLevelingCosts.Balance(player, currency),
            ProtectionCurrency = protection.Currency,
            ProtectionPerHour = protection.Amount,
            ProtectionBalance = OfflineLevelingCosts.Balance(player, protection.Currency),
        };
    }

    private static ValueTask ShowAsync(Player player, uint requestId, OfflineLevelingResult result, OfflineLevelingSettings settings)
    {
        var answer = CreateOffer(player, settings);
        answer.RequestId = requestId;
        answer.Result = result;
        return ShowAsync(player, answer);
    }

    private static ValueTask ShowAsync(Player player, OfflineLevelingAnswer answer)
        => player.InvokeViewPlugInAsync<IOfflineLevelingView>(view => view.ShowAsync(answer));

    private static OfflineLevelingPlan? Read(ReadOnlyMemory<byte> data)
    {
        if (data.IsEmpty)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<OfflineLevelingPlan>(data.Span);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
