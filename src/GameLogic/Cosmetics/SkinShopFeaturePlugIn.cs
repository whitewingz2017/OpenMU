// <copyright file="SkinShopFeaturePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Cosmetics;

using System.Runtime.InteropServices;
using MUnique.OpenMU.PlugIns;

/// <summary>Enables the skin shop and exposes category prices in the administration panel.</summary>
[PlugIn]
[Display(Name = "Skin Shop", Description = "Permanent tradable skins, with a chosen normal item bonus, paid in WCoinC.")]
[Guid("a38877e0-c239-40b4-91ea-45a8a7f814c7")]
public sealed class SkinShopFeaturePlugIn : IFeaturePlugIn, ISupportCustomConfiguration<SkinShopSettings>, ISupportDefaultCustomConfiguration
{
    /// <inheritdoc />
    public SkinShopSettings? Configuration { get; set; }

    /// <summary>Gets the active shop settings, or null when the feature is disabled.</summary>
    /// <param name="context">The game context.</param>
    public static SkinShopSettings? GetSettings(IGameContext context)
        => context.FeaturePlugIns.GetPlugIn<SkinShopFeaturePlugIn>() is { } feature ? feature.Configuration ?? new SkinShopSettings() : null;

    /// <inheritdoc />
    public object CreateDefaultConfig() => new SkinShopSettings();
}
