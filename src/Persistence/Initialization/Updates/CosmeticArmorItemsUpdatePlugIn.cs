// <copyright file="CosmeticArmorItemsUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.PlugIns;

/// <summary>Adds optional cosmetic armor item definitions to existing Season 6 configurations.</summary>
[PlugIn]
[Display(Name = "Set 315 Cosmetic Armor", Description = "Adds five tradable skin items and custom cosmetic slots, without combat bonuses.")]
[Guid("190050c7-3e97-4e7b-9ccb-0cbfe839a4e1")]
public sealed class CosmeticArmorItemsUpdatePlugIn : UpdatePlugInBase
{
    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override string Name => "Set 315 Cosmetic Armor";

    /// <inheritdoc />
    public override string Description => "Restores normal skin inventory footprints and repacks affected pages safely. Apply while players are offline; free space if a page cannot fit the resized items.";

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 10, 28, 0, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    public override int Version => 4;

    /// <inheritdoc />
    public override DateTime UpdatedAt => new(2026, 10, 29, 0, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    public override bool IsMandatory => false;

    /// <inheritdoc />
    protected override async ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        VersionSeasonSix.Items.CosmeticArmorItems.Ensure(context, gameConfiguration);
        await CosmeticStorageResize.ApplyAsync(context, gameConfiguration).ConfigureAwait(false);
    }
}
