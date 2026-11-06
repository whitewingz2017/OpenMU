// <copyright file="MonsterWingsCrafting.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions.Craftings;

using MUnique.OpenMU.DataModel.Configuration.ItemCrafting;
using MUnique.OpenMU.GameLogic.PlayerActions.Items;
using MUnique.OpenMU.GameLogic.Views.NPC;

/// <summary>
/// Crafting for the monster wings (2.5 wings): one second wing or cape, a Jewel of Chaos, a Jewel of Creation
/// and one monster wing material. The material decides the resulting wing.
/// </summary>
/// <remarks>
/// Like CChaosBox::MonsterWingMix of the Pegasus game server: Death King's Bone (14:176) gives the Cloak of Death (12:262),
/// Hell Maine's Leather (14:177) the Wings of Chaos (12:263), Dark Phoenix Flame (14:178) the Wings of Power (12:264)
/// and Death Beam Knight's Soul (14:179) the Wings of Life (12:265).
/// </remarks>
public class MonsterWingsCrafting : SecondWingsCrafting
{
    private const byte MaterialGroup = 14;
    private const short FirstMaterialNumber = 176;
    private const short LastMaterialNumber = 179;
    private const byte WingGroup = 12;
    private const short FirstWingNumber = 262;

    private readonly SimpleCraftingSettings _settings;

    /// <summary>
    /// Initializes a new instance of the <see cref="MonsterWingsCrafting"/> class.
    /// </summary>
    /// <param name="settings">The settings.</param>
    public MonsterWingsCrafting(SimpleCraftingSettings settings)
        : base(settings)
    {
        this._settings = settings;
    }

    /// <inheritdoc/>
    public override CraftingResult? TryGetRequiredItems(Player player, out IList<CraftingRequiredItemLink> items, out byte successRate)
    {
        var result = base.TryGetRequiredItems(player, out items, out successRate);
        if (result is not null)
        {
            return result;
        }

        // The second wings and the capes are separate requirements, because their price divisors differ.
        var wingCount = items.SelectMany(link => link.Items).Count(item => item.IsWing());
        if (wingCount == 1)
        {
            return result;
        }

        successRate = 0;
        return wingCount == 0 ? CraftingResult.LackingMixItems : CraftingResult.TooManyItems;
    }

    /// <inheritdoc/>
    protected override async ValueTask<List<Item>> CreateOrModifyResultItemsAsync(IList<CraftingRequiredItemLink> requiredItems, Player player, byte socketSlot, byte successRate)
    {
        var material = requiredItems.SelectMany(link => link.Items)
            .FirstOrDefault(item => item.Definition is { Group: MaterialGroup, Number: >= FirstMaterialNumber and <= LastMaterialNumber });
        var wingNumber = material is null ? -1 : FirstWingNumber + material.Definition!.Number - FirstMaterialNumber;
        var resultItem = this._settings.ResultItems.FirstOrDefault(r => r.ItemDefinition is { Group: WingGroup } wing && wing.Number == wingNumber);
        if (resultItem is null)
        {
            player.Logger.LogWarning("Monster wings crafting has no result item for material {Material}.", material);
            return [];
        }

        return await this.CreateResultItemsAsync(player, requiredItems, resultItem, successRate).ConfigureAwait(false);
    }
}
