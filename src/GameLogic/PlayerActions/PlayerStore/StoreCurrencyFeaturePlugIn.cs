// <copyright file="StoreCurrencyFeaturePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions.PlayerStore;

using System.Runtime.InteropServices;
using MUnique.OpenMU.PlugIns;

/// <summary>Enables validated inventory jewel and cash-shop coin payments.</summary>
[PlugIn]
[Display(Name = "Personal Store Currencies", Description = "Inventory jewel and cash-shop coin prices for extended clients.")]
[Guid("51f550f0-bc43-4b04-8364-5ac2ad126882")]
public class StoreCurrencyFeaturePlugIn : IFeaturePlugIn, ISupportCustomConfiguration<StoreCurrencySettings>, ISupportDefaultCustomConfiguration
{
    /// <inheritdoc />
    public StoreCurrencySettings? Configuration { get; set; }

    /// <inheritdoc />
    public object CreateDefaultConfig() => new StoreCurrencySettings();

    /// <summary>Gets the settings, or null when disabled.</summary>
    public static StoreCurrencySettings? GetSettings(IGameContext context)
        => context.FeaturePlugIns.GetPlugIn<StoreCurrencyFeaturePlugIn>() is { } plugin
            ? plugin.Configuration ?? new StoreCurrencySettings() : null;
}
