// <copyright file="GemstoneBankUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.JewelBank;
using MUnique.OpenMU.PlugIns;

/// <summary>Adds Gemstone to saved bank settings without resetting operator choices.</summary>
[PlugIn]
[Display(Name = nameof(GemstoneBankResources.Name), Description = nameof(GemstoneBankResources.Description), ResourceType = typeof(GemstoneBankResources))]
[Guid("dc77089a-f343-4ad1-afd7-dc7b5a43ce4f")]
public sealed class GemstoneBankUpdatePlugIn : UpdatePlugInBase
{
    // Persisted identity of the existing Jewel Bank packet handler.
    private static readonly Guid BankHandlerId = new("3a2e4ae2-6965-4598-a1d1-17d10331184f");

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override string Name => GemstoneBankResources.Name;

    /// <inheritdoc />
    public override string Description => GemstoneBankResources.Description;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 11, 8, 0, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    public override bool IsMandatory => false;

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        var feature = gameConfiguration.PlugInConfigurations.FirstOrDefault(p => p.TypeId == BankHandlerId);
        var gemstone = gameConfiguration.Items.FirstOrDefault(i => i.Group == ItemConstants.Gemstone.Group && i.Number == ItemConstants.Gemstone.Number);
        if (feature is null || gemstone is null)
        {
            return ValueTask.CompletedTask;
        }

        var configuration = feature.GetConfiguration<JewelBankConfiguration>(null) ?? new();
        var itemId = (gemstone.Group * 512) + gemstone.Number;
        if (!configuration.Items.Contains(itemId))
        {
            configuration.Items = [.. configuration.Items, itemId];
            feature.SetConfiguration(configuration, null);
        }

        return ValueTask.CompletedTask;
    }
}
