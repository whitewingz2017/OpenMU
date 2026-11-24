// <copyright file="ItemEventRecorderTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using System.IO;
using System.Text.Json;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.Persistence;
using BasicModel = MUnique.OpenMU.Persistence.BasicModel;

/// <summary>Checks immutable journal snapshots and the identity of acquired temporary drops.</summary>
[TestFixture]
[NonParallelizable]
public class ItemEventRecorderTests
{
    private string? _previousDirectory;
    private string _directory = null!;

    /// <summary>Isolates the journal from real server data.</summary>
    [SetUp]
    public void SetUp()
    {
        this._previousDirectory = Environment.GetEnvironmentVariable("OPENMU_ITEM_EVENT_DIRECTORY");
        this._directory = Path.Combine(Path.GetTempPath(), "openmu-item-journal-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(this._directory);
        Environment.SetEnvironmentVariable("OPENMU_ITEM_EVENT_DIRECTORY", this._directory);
    }

    /// <summary>Restores the server environment and removes only this test's temporary files.</summary>
    [TearDown]
    public void TearDown()
    {
        Environment.SetEnvironmentVariable("OPENMU_ITEM_EVENT_DIRECTORY", this._previousDirectory);
        Directory.Delete(this._directory, true);
    }

    /// <summary>Upgrades and trades retain an item's identifier and its historical values.</summary>
    [Test]
    public async Task SnapshotsAreImmutableAndPublicAsync()
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        player.SelectedCharacter!.Name = "Knight";
        player.SelectedCharacter.CharacterClass!.Name = "Blade Master";
        var item = new BasicModel.Item { Id = Guid.NewGuid(), Definition = new BasicModel.ItemDefinition { Name = "Dragon Sword", Width = 1, Height = 1 }, Level = 9 };
        ItemEventRecorder.Record(player, item, "LOOT", "Monster:Golden Dragon");
        item.Level = 10;
        ItemEventRecorder.Record(player, item, "UPGRADE", "Jewel of Soul");
        ItemEventRecorder.Record(player, item, "TRADE", "To:Elf");
        var lines = File.ReadAllLines(Directory.GetFiles(this._directory).Single());
        using var first = JsonDocument.Parse(lines[0]);
        using var second = JsonDocument.Parse(lines[1]);
        Assert.Multiple(() =>
        {
            Assert.That(lines, Has.Length.EqualTo(3));
            Assert.That(first.RootElement.GetProperty("level").GetInt32(), Is.EqualTo(9));
            Assert.That(second.RootElement.GetProperty("level").GetInt32(), Is.EqualTo(10));
            Assert.That(first.RootElement.GetProperty("itemId").GetGuid(), Is.EqualTo(item.Id));
            Assert.That(second.RootElement.GetProperty("itemId").GetGuid(), Is.EqualTo(item.Id));
            Assert.That(first.RootElement.GetProperty("id").GetGuid(), Is.Not.EqualTo(second.RootElement.GetProperty("id").GetGuid()));
            Assert.That(first.RootElement.TryGetProperty("accountId", out _), Is.False);
            Assert.That(first.RootElement.TryGetProperty("loginName", out _), Is.False);
            Assert.That(first.RootElement.TryGetProperty("passwordHash", out _), Is.False);
            Assert.That(first.RootElement.GetProperty("characterName").GetString(), Is.EqualTo("Knight"));
            Assert.That(first.RootElement.GetProperty("characterClass").GetString(), Is.EqualTo("Blade Master"));
        });
    }

    /// <summary>Pickup logs the new persistent item, rather than the temporary floor instance.</summary>
    [Test]
    public async Task TemporaryPickupRecordsPersistentIdentityAsync()
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        player.SelectedCharacter!.Name = "Knight";
        var temporary = new TemporaryItem { Definition = new BasicModel.ItemDefinition { Name = "Sword", Width = 1, Height = 1, Group = 0, Number = 3 }, Level = 3 };
        await using var dropped = new DroppedItem(temporary, player.Position, player.CurrentMap!, player) { ItemEventSource = "Bag:Box of Kundun +1" };
        await player.CurrentMap!.AddAsync(dropped).ConfigureAwait(false);
        var result = await dropped.TryPickUpByAsync(player).ConfigureAwait(false);
        Assert.That(result.Success, Is.True);
        var acquired = player.Inventory!.Items.Single();
        using var record = JsonDocument.Parse(File.ReadAllLines(Directory.GetFiles(this._directory).Single()).Single());
        Assert.Multiple(() =>
        {
            Assert.That(acquired.GetId(), Is.Not.EqualTo(Guid.Empty));
            Assert.That(record.RootElement.GetProperty("itemId").GetGuid(), Is.EqualTo(acquired.GetId()));
            Assert.That(record.RootElement.GetProperty("source").GetString(), Is.EqualTo("Bag:Box of Kundun +1"));
            Assert.That(record.RootElement.GetProperty("itemGroup").GetInt32(), Is.EqualTo(0));
            Assert.That(record.RootElement.GetProperty("itemNumber").GetInt32(), Is.EqualTo(3));
        });
    }

    /// <summary>An unsuccessful pickup must not invent a loot event.</summary>
    [Test]
    public async Task FailedPickupDoesNotRecordAsync()
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        var temporary = new TemporaryItem { Definition = new BasicModel.ItemDefinition { Name = "Oversized", Width = 255, Height = 255 } };
        await using var dropped = new DroppedItem(temporary, player.Position, player.CurrentMap!, player);
        var result = await dropped.TryPickUpByAsync(player).ConfigureAwait(false);
        Assert.That(result.Success, Is.False);
        Assert.That(Directory.GetFiles(this._directory), Is.Empty);
    }
}
