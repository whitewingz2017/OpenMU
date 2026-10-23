// <copyright file="JewelStatistics.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence;

using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Entities;

/// <summary>Normalizes physical jewels and configured jewel bundles for reports.</summary>
public static class JewelStatistics
{
    /// <summary>Counts physical single jewels, including all storage expansion slots.</summary>
    /// <param name="items">The physical items.</param>
    /// <param name="configuration">The owning game configuration.</param>
    /// <param name="itemCodes">The single jewel types.</param>
    /// <param name="utcNow">The snapshot time.</param>
    /// <returns>The quantities, with bundles unpacked logically.</returns>
    public static IReadOnlyList<JewelAmount> Count(IEnumerable<Item> items, GameConfiguration configuration, int[] itemCodes, DateTime utcNow)
    {
        var result = new Dictionary<int, long>();
        foreach (var item in items)
        {
            if (item.ExpiresAtUtc <= utcNow || item.Definition is not { } definition)
            {
                continue;
            }

            var code = definition.Group * 512 + definition.Number;
            long amount = 1;
            if (itemCodes.Contains(code))
            {
                if (item.Level != 0)
                {
                    continue;
                }
            }
            else if (item.Level <= 2 && configuration.JewelMixes?.FirstOrDefault(mix => mix.MixedJewel == definition)?.SingleJewel is { } single
                     && itemCodes.Contains(code = single.Group * 512 + single.Number))
            {
                amount = (item.Level + 1) * 10;
            }
            else
            {
                continue;
            }

            result[code] = result.GetValueOrDefault(code) + amount;
        }

        return result.Select(pair => new JewelAmount(pair.Key, pair.Value)).ToList();
    }
}
