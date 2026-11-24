// <copyright file="ItemEventRecorder.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic;

using System.IO;
using System.Text.Json;
using MUnique.OpenMU.Persistence;

/// <summary>
/// Writes immutable public item snapshots to a journal which the website can import independently.
/// </summary>
public static class ItemEventRecorder
{
    private static readonly object JournalLock = new();
    private static readonly string JournalId = Guid.NewGuid().ToString("N");

    /// <summary>
    /// Records a completed item action when OPENMU_ITEM_EVENT_DIRECTORY is configured.
    /// Account credentials and account identifiers are deliberately excluded from the snapshot.
    /// </summary>
    /// <param name="player">The character who performed the action.</param>
    /// <param name="item">The persistent item after the action.</param>
    /// <param name="eventType">The action code.</param>
    /// <param name="source">The monster, shop, character or other source.</param>
    /// <param name="detail">Additional public context.</param>
    /// <param name="relatedItemId">Another item involved in the action, when applicable.</param>
    /// <param name="monsterNumber">The monster which supplied a picked-up item, when applicable.</param>
    public static void Record(Player player, Item item, string eventType, string source, string detail = "", Guid? relatedItemId = null, int? monsterNumber = null)
    {
        var directory = Environment.GetEnvironmentVariable("OPENMU_ITEM_EVENT_DIRECTORY");
        if (string.IsNullOrWhiteSpace(directory))
        {
            return;
        }

        try
        {
            var itemId = item.GetId();
            if (itemId == Guid.Empty)
            {
                player.Logger.LogWarning("Cannot journal {EventType}: the item has no persistent identifier.", eventType);
                return;
            }

            var now = DateTime.UtcNow;
            var name = item.Definition?.GetNameForLevel(item.Level) ?? "Unknown item";
            var separator = name.IndexOf("||", StringComparison.Ordinal);
            name = separator < 0 ? name : name[..separator];
            var entry = new
            {
                id = Guid.NewGuid(),
                itemId,
                occurredAt = now,
                eventType,
                characterName = player.SelectedCharacter?.Name ?? string.Empty,
                characterClass = player.SelectedCharacter?.CharacterClass?.Name.ToString(),
                itemName = name,
                itemGroup = item.Definition?.Group,
                itemNumber = item.Definition?.Number,
                monsterNumber,
                level = (int)item.Level,
                skill = item.HasSkill,
                luck = item.ItemOptions.Any(option => option.ItemOption?.OptionType == DataModel.Configuration.Items.ItemOptionTypes.Luck),
                excellent = item.IsExcellent(),
                jewel = name.StartsWith("Jewel of ", StringComparison.OrdinalIgnoreCase),
                socketCount = item.SocketCount,
                stats = item.ToString(),
                source,
                detail,
                relatedItemId,
                server = (player.GameContext as IGameServerContext)?.Id.ToString() ?? string.Empty,
            };
            var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(entry) + "\n");
            lock (JournalLock)
            {
                Directory.CreateDirectory(directory);
                var path = Path.Combine(directory, $"events-{now:yyyyMMdd}-{JournalId}.jsonl");
                using var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read, 4096, FileOptions.WriteThrough);
                stream.Write(bytes);
                stream.Flush(true);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Journal failures must be visible to operators without cancelling a completed game action.
            player.Logger.LogError(exception, "Item tracking journal write failed. Check OPENMU_ITEM_EVENT_DIRECTORY and free disk space.");
        }
    }
}
