namespace MUnique.OpenMU.GameLogic.Muun;
using MUnique.OpenMU.DataModel;
/// <summary>Commits material consumption and the Muun reward together.</summary>
public static class MuunExchange
{
    /// <summary>Executes under the caller's player persistence lock.</summary>
    public static async ValueTask<string> ExecuteAsync(Player player, MuunConfiguration config, byte source, byte recipeIndex)
    {
        if (recipeIndex >= Math.Min(64, config.Exchanges.Count) || player.Inventory is not { } bag
            || player.SelectedCharacter is not { } character) return "MuunInvalid";
        var recipe = config.Exchanges[recipeIndex];
        var first = bag.GetItem(source);
        if (first is null || first.Level != 0 || first.ExpiresAtUtc is not null || source <= InventoryConstants.LastEquippableItemSlotIndex
            || MuunRules.Type(first) != recipe.MaterialItemType || recipe.RequiredCount is < 1 or > 255
            || recipe.Rewards.Count is < 1 or > 120) return "MuunInvalid";
        var definitions = player.GameContext.Configuration.Items;
        foreach (var reward in recipe.Rewards)
            if (reward.Weight is < 1 or > 1000000 || MuunRules.Find(config, reward.ItemType) is null
                || definitions.Count(d => d.Group * 512 + d.Number == reward.ItemType) != 1) return "MuunInvalid";
        var storage = character.MuunInventory;
        var free = Enumerable.Range(MuunRules.EquipmentCount, config.Capacity - MuunRules.EquipmentCount)
            .FirstOrDefault(slot => storage?.Items.Any(i => i.ItemSlot == slot) != true, -1);
        if (free < 0) return "MuunFull";
        var materials = new List<(Item Item, byte Slot, double Life)>();
        var remaining = recipe.RequiredCount;
        foreach (var item in bag.Items.OrderBy(i => i == first ? 0 : 1).ThenBy(i => i.ItemSlot))
        {
            if (item.ItemSlot <= InventoryConstants.LastEquippableItemSlotIndex || item.Level != 0
                || item.ExpiresAtUtc is not null || MuunRules.Type(item) != recipe.MaterialItemType) continue;
            var count = item.IsStackable() ? (int)item.Durability : 1;
            if (count <= 0) continue;
            materials.Add((item, item.ItemSlot, item.Durability));
            remaining -= Math.Min(remaining, count);
            if (remaining == 0) break;
        }
        if (remaining != 0) return "MuunMaterialsMissing";
        var roll = Random.Shared.Next(recipe.Rewards.Sum(p => p.Weight));
        var chosen = recipe.Rewards[0];
        foreach (var reward in recipe.Rewards) { if (roll < reward.Weight) { chosen = reward; break; } roll -= reward.Weight; }
        storage ??= character.MuunInventory = player.PersistenceContext.CreateNew<ItemStorage>();
        var created = player.PersistenceContext.CreateNew<Item>();
        created.Definition = definitions.Single(d => d.Group * 512 + d.Number == chosen.ItemType);
        created.ItemSlot = (byte)free; created.Durability = created.Definition.Durability;
        var removed = new HashSet<Item>();
        var committed = false;
        try
        {
            remaining = recipe.RequiredCount;
            foreach (var (item, slot, life) in materials)
            {
                var count = item.IsStackable() ? (int)life : 1;
                var take = Math.Min(remaining, count); remaining -= take;
                if (item.IsStackable() && take < count) item.Durability -= take;
                else { await bag.RemoveItemAsync(item).ConfigureAwait(false); removed.Add(item); await player.PersistenceContext.DeleteAsync(item).ConfigureAwait(false); }
            }
            storage.Items.Add(created);
            committed = await player.SaveProgressAsync().ConfigureAwait(false);
            return committed ? "MuunChanged" : "MuunSaveFailed";
        }
        catch (Exception exception)
        {
            player.Logger.LogError(exception, "Muun exchange failed for {Player}.", player);
            return "MuunSaveFailed";
        }
        finally
        {
            if (!committed)
            {
                storage.Items.Remove(created);
                await player.PersistenceContext.DeleteAsync(created).ConfigureAwait(false);
                foreach (var (item, slot, life) in materials)
                {
                    item.Durability = life;
                    if (!removed.Contains(item)) continue;
                    player.PersistenceContext.Attach(item);
                    if (!await bag.AddItemAsync(slot, item).ConfigureAwait(false)) throw new InvalidOperationException("Muun exchange rollback failed.");
                }
            }
        }
    }
}
