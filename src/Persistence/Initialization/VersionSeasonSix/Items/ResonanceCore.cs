// <copyright file="ResonanceCore.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Items;

using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;

/// <summary>Adds the combination material.</summary>
public static class ResonanceCore
{
    /// <summary>Adds a missing item without overwriting administrators' settings.</summary>
    /// <param name="context">The persistence context.</param>
    /// <param name="config">The game configuration.</param>
    public static void Ensure(IContext context, GameConfiguration config)
    {
        var item = config.Items.SingleOrDefault(d => d.Group == 14 && d.Number == 252);
        if (item is not null && item.Name.ValueInNeutralLanguage != "Resonance Core")
        {
            throw new InvalidOperationException("Resonance Core item 14:252 is already in use.");
        }

        if (item is null)
        {
            item = context.CreateNew<ItemDefinition>();
            item.SetGuid(14, 252);
            item.Group = 14;
            item.Number = 252;
            item.Name = "Resonance Core";
            item.Width = 1;
            item.Height = 1;
            item.Durability = 255;
            item.IsTradable = true;
            item.IsStorable = true;
            item.IsDroppable = true;
            item.IsPersonalStoreSellable = true;
            item.IsSellableToNpc = false;
            item.IsRepairable = false;
            config.Items.Add(item);
        }
    }
}
