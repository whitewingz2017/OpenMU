// <copyright file="CosmeticArmorItemsUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.PlugIns;

/// <summary>Adds optional cosmetic armor item definitions to existing Season 6 configurations.</summary>
[PlugIn]
[Display(Name = "Set 315 Cosmetic Armor", Description = "Full-set skins, normal item bonuses and the WCoinC Skin Shop.")]
[Guid("190050c7-3e97-4e7b-9ccb-0cbfe839a4e1")]
public sealed class CosmeticArmorItemsUpdatePlugIn : UpdatePlugInBase
{
    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override string Name => "Set 315 Cosmetic Armor";

    /// <inheritdoc />
    public override string Description => "Adds class-specific weapon skins and class limits; installs full-set skins, standalone Fenrir mounts and the skin shop. Incompatible equipped weapons return to the bag. Apply with all players offline; free storage space if conversion cannot fit.";

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 10, 28, 0, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    public override int Version => 8;

    /// <inheritdoc />
    public override DateTime UpdatedAt => new(2026, 10, 29, 0, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    public override bool IsMandatory => false;

    /// <inheritdoc />
    protected override async ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        VersionSeasonSix.Items.CosmeticArmorItems.Ensure(context, gameConfiguration);
        if (gameConfiguration.PlugInConfigurations.All(p => p.TypeId != typeof(GameLogic.Cosmetics.SkinShopFeaturePlugIn).GUID))
        {
            var feature = context.CreateNew<MUnique.OpenMU.PlugIns.PlugInConfiguration>();
            feature.TypeId = typeof(GameLogic.Cosmetics.SkinShopFeaturePlugIn).GUID;
            feature.CustomConfiguration = System.Text.Json.JsonSerializer.Serialize(new GameLogic.Cosmetics.SkinShopSettings());
            feature.IsActive = true;
            gameConfiguration.PlugInConfigurations.Add(feature);
        }
        await CosmeticSetConversion.ApplyAsync(context, gameConfiguration).ConfigureAwait(false);
    }
}
