// <copyright file="CosmeticStorageResizeTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Tests;

using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.Persistence.Initialization.Updates;
using MUnique.OpenMU.Persistence.InMemory;
using BasicModel = MUnique.OpenMU.Persistence.BasicModel;

/// <summary>Verifies inventory resizing plans do not overwrite or lose owned items.</summary>
[TestFixture]
internal class CosmeticStorageResizeTests
{
    [Test]
    public void EnlargedWingMovesAroundExistingItemsWithoutMutatingOriginals()
    {
        var (storage, wing, normal, skins) = Create();
        var plan = CosmeticStorageResize.Plan(storage, 64, 12, skins);
        Assert.That(plan.Single().Item, Is.SameAs(wing));
        Assert.That(plan.Single().Slot, Is.EqualTo(14));
        Assert.That(wing.ItemSlot, Is.EqualTo(12));
        Assert.That(normal.ItemSlot, Is.EqualTo(13));
        Assert.That(storage.Items, Has.Count.EqualTo(2));
    }

    [Test]
    public void FullPageFailsWithoutChangingAnySavedPositions()
    {
        var (storage, wing, normal, skins) = Create();
        for (byte slot = 14; slot < 76; slot++)
        {
            storage.Items.Add(new BasicModel.Item { Id = Guid.NewGuid(), ItemSlot = slot, Definition = normal.Definition });
        }

        Assert.Throws<InvalidOperationException>(() => CosmeticStorageResize.Plan(storage, 64, 12, skins));
        Assert.That(wing.ItemSlot, Is.EqualTo(12));
        Assert.That(storage.Items, Has.Count.EqualTo(64));
    }

    [Test]
    public void EquippedSkinsRemainOutsideTheBagPlan()
    {
        var (storage, wing, _, skins) = Create();
        wing.ItemSlot = 247;
        Assert.That(CosmeticStorageResize.Plan(storage, 64, 12, skins), Is.Empty);
        Assert.That(wing.ItemSlot, Is.EqualTo(247));
    }

    [Test]
    public async Task SuccessfulMigrationCommitsThePlannedPositionsAsync()
    {
        var provider = new InMemoryPersistenceContextProvider();
        using var context = provider.CreateNewContext();
        var configuration = context.CreateNew<GameConfiguration>();
        var character = context.CreateNew<Character>();
        var (storage, wing, normal, _) = Create();
        var slot = new BasicModel.ItemSlotType();
        slot.ItemSlots.Add(247);
        wing.Definition!.ItemSlot = slot;
        configuration.Items.Add(wing.Definition);
        character.Inventory = storage;
        await CosmeticStorageResize.ApplyAsync(context, configuration).ConfigureAwait(false);
        Assert.That(wing.ItemSlot, Is.EqualTo(14));
        Assert.That(normal.ItemSlot, Is.EqualTo(13));
        Assert.That(storage.Items, Has.Count.EqualTo(2));
    }

    [Test]
    public void FailureOnALaterCharacterDoesNotMoveEarlierCharacters()
    {
        var provider = new InMemoryPersistenceContextProvider();
        using var context = provider.CreateNewContext();
        var configuration = context.CreateNew<GameConfiguration>();
        var first = context.CreateNew<Character>();
        var second = context.CreateNew<Character>();
        var (storage, wing, normal, _) = Create();
        first.Inventory = storage;
        var slot = new BasicModel.ItemSlotType();
        slot.ItemSlots.Add(247);
        wing.Definition!.ItemSlot = slot;
        configuration.Items.Add(wing.Definition);
        second.Inventory = new BasicModel.ItemStorage();
        second.Inventory.Items.Add(new BasicModel.Item { Id = Guid.NewGuid(), ItemSlot = 12, Definition = wing.Definition });
        for (byte position = 13; position < 76; position++)
        {
            second.Inventory.Items.Add(new BasicModel.Item { Id = Guid.NewGuid(), ItemSlot = position, Definition = normal.Definition });
        }

        Assert.ThrowsAsync<InvalidOperationException>(async () => await CosmeticStorageResize.ApplyAsync(context, configuration).ConfigureAwait(false));
        Assert.That(wing.ItemSlot, Is.EqualTo(12));
    }

    private static (ItemStorage Storage, Item Wing, Item Normal, IReadOnlyDictionary<Guid, ItemDefinition> Skins) Create()
    {
        var storage = new BasicModel.ItemStorage();
        var definition = new BasicModel.ItemDefinition { Id = Guid.NewGuid(), Width = 5, Height = 3 };
        var wing = new BasicModel.Item { Id = Guid.NewGuid(), Definition = definition, ItemSlot = 12 };
        var normal = new BasicModel.Item { Id = Guid.NewGuid(), Definition = new BasicModel.ItemDefinition { Id = Guid.NewGuid(), Width = 1, Height = 1 }, ItemSlot = 13 };
        storage.Items.Add(wing);
        storage.Items.Add(normal);
        return (storage, wing, normal, new Dictionary<Guid, ItemDefinition> { [definition.Id] = definition });
    }
}
