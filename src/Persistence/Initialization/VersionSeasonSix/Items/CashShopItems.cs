// <copyright file="CashShopItems.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Items;

using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.ItemCrafting;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.Interfaces;
using MUnique.OpenMU.Persistence.Initialization.Properties;

/// <summary>
/// Initializer for the items of the cash shop which aren't put into the inventory. A delivery
/// plugin applies them when a player uses them from the cash shop storage, e.g. the
/// <see cref="GameLogic.CashShop.VaultExtensionDeliveryPlugIn"/>.
/// </summary>
/// <remarks>
/// Existing items aren't changed, so it can be used by an update, too.
/// </remarks>
internal class CashShopItems : InitializerBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CashShopItems"/> class.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="gameConfiguration">The game configuration.</param>
    public CashShopItems(IContext context, GameConfiguration gameConfiguration)
        : base(context, gameConfiguration)
    {
    }

    /// <inheritdoc />
    public override void Initialize()
    {
        new CashShopCatalogItems(this.Context, this.GameConfiguration).Initialize();
        new CashShopEffectItems(this.Context, this.GameConfiguration).Initialize();
        this.CreateItem(91, LocalizedString.FromResource(() => ItemNames.SummonerCharacterCard));
        this.CreateItem(162, LocalizedString.FromResource(() => ItemNames.MagicBackpack));
        this.CreateItem(163, LocalizedString.FromResource(() => ItemNames.VaultExpansionCertificate));
        this.CreateItem(169, LocalizedString.FromResource(() => ItemNames.RageFighterCharacterCard));
        this.ConfigureLuckTalisman();
    }

    private void ConfigureLuckTalisman()
    {
        var talisman = this.GameConfiguration.Items.Single(item => item.Group == 14 && item.Number == 53);
        talisman.Durability = Math.Max(talisman.Durability, (byte)10);
        foreach (var settings in this.GameConfiguration.Monsters.Where(monster => monster.NpcWindow == NpcWindow.ChaosMachine).SelectMany(monster => monster.ItemCraftings)
            .Select(crafting => crafting.SimpleCraftingSettings).OfType<SimpleCraftingSettings>()
            .Where(settings => settings.RequiredItems.All(required => !required.PossibleItems.Contains(talisman))))
        {
            // Native talisman durability is its success-rate percentage; recipe caps still apply.
            var optional = this.Context.CreateNew<ItemCraftingRequiredItem>();
            optional.PossibleItems.Add(talisman);
            optional.MinimumAmount = 0;
            optional.MaximumAmount = 10;
            optional.MaximumItemLevel = 15;
            optional.AddPercentage = 1;
            optional.SuccessResult = MixResult.Disappear;
            optional.FailResult = MixResult.Disappear;
            settings.RequiredItems.Add(optional);
        }
    }

    private void CreateItem(byte number, LocalizedString name)
    {
        const byte group = 14;
        if (this.GameConfiguration.Items.Any(item => item.Group == group && item.Number == number))
        {
            return;
        }

        var item = this.Context.CreateNew<ItemDefinition>();
        item.Group = group;
        item.Number = number;
        item.Name = name;
        item.Width = 1;
        item.Height = 1;
        item.Durability = 1;
        item.SetGuid(item.Group, item.Number);
        this.GameConfiguration.Items.Add(item);
    }
}
