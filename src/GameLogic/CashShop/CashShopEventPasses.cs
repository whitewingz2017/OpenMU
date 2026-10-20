// <copyright file="CashShopEventPasses.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.CashShop;

/// <summary>
/// Resolves live event passes from cash-shop configuration.
/// </summary>
internal static class CashShopEventPasses
{
    /// <summary>
    /// Finds a valid alternate pass, preferring the inventory slot sent by the client.
    /// </summary>
    /// <param name="player">The entrant.</param>
    /// <param name="type">The event.</param>
    /// <param name="level">The event level.</param>
    /// <param name="slot">The optional requested slot.</param>
    /// <returns>The live ticket, or <see langword="null"/>.</returns>
    internal static Item? Find(Player player, MiniGameType type, int level, byte slot = byte.MaxValue)
    {
        var passes = CashShopFeaturePlugIn.GetSettings(player.GameContext)?.EventPasses;
        if (passes is null || player.Inventory is null)
        {
            return null;
        }

        return player.Inventory.Items.OrderBy(item => item.ItemSlot == slot ? 0 : 1)
            .FirstOrDefault(item => item.Durability > 0 && (item.ExpiresAtUtc is null || item.ExpiresAtUtc > DateTime.UtcNow)
                && passes.Any(pass => Matches(pass, item, type, level)));
    }

    /// <summary>
    /// Determines whether entering should retain a timed reusable pass.
    /// </summary>
    /// <param name="player">The entrant.</param>
    /// <param name="ticket">The selected pass.</param>
    /// <param name="type">The event.</param>
    /// <param name="level">The event level.</param>
    /// <returns>Whether the ticket must be retained.</returns>
    internal static bool IsReusable(Player player, Item? ticket, MiniGameType type, int level)
        => ticket?.ExpiresAtUtc > DateTime.UtcNow
            && CashShopFeaturePlugIn.GetSettings(player.GameContext)?.EventPasses.Any(pass => pass.ReusableUntilExpiration && Matches(pass, ticket, type, level)) == true;

    private static bool Matches(CashShopEventPass pass, Item item, MiniGameType type, int level)
        => pass.Event == type && item.Definition is { } definition
            && definition.Group == pass.ItemGroup && definition.Number == pass.ItemNumber
            && (pass.Levels.Count == 0 || pass.Levels.Contains(level));
}
