namespace MUnique.OpenMU.Tests;
using Moq;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.Muun;
using Basic = MUnique.OpenMU.Persistence.BasicModel;

[TestFixture]
public class MuunTests
{
    [TestCase(0, false)] [TestCase(1, false)] [TestCase(20, false)]
    [TestCase(0, true)] [TestCase(1, true)] [TestCase(20, true)]
    public async Task DirectedDepositPreservesIdentityOrRestoresBag(byte target, bool fail)
    {
        var player = await Create(); var pet = await BagItem(player, 0); var slot = pet.ItemSlot;
        var actions = new MuunActions();
        if (fail) CashShopActionsTest.SaveFailingContext.Install(player, () => true);
        await actions.ExecuteAsync(player, 1, 10, slot, target);
        await actions.ExecuteAsync(player, 1, 10, slot, target);
        Assert.That(pet.ItemSlot, Is.EqualTo(fail ? slot : target));
        Assert.That(player.Inventory!.Items.Contains(pet), Is.EqualTo(fail));
        Assert.That(player.SelectedCharacter!.MuunInventory!.Items.Contains(pet), Is.EqualTo(!fail));
    }

    [TestCase(false)] [TestCase(true)]
    public async Task BagPetReplacesMainAndPreservesPreviousPet(bool fail)
    {
        var player = await Create(); var first = await BagItem(player, 0); var second = await BagItem(player, 1);
        var actions = new MuunActions();
        await actions.ExecuteAsync(player, 1, 10, first.ItemSlot, 0);
        var bagSlot = second.ItemSlot;
        if (fail) CashShopActionsTest.SaveFailingContext.Install(player, () => true);
        await actions.ExecuteAsync(player, 2, 10, bagSlot, 0);
        Assert.That(first.ItemSlot, Is.EqualTo(fail ? 0 : 2));
        Assert.That(second.ItemSlot, Is.EqualTo(fail ? bagSlot : 0));
        Assert.That(player.Inventory!.Items.Contains(second), Is.EqualTo(fail));
        Assert.That(player.SelectedCharacter!.MuunInventory!.Items.Contains(first), Is.True);
        Assert.That(player.SelectedCharacter.MuunInventory.Items.Contains(second), Is.EqualTo(!fail));
    }

    [TestCase(false)] [TestCase(true)]
    public async Task StorageDragAndUnequipUseChosenEmptySlot(bool fail)
    {
        var player = await Create(); var pet = await BagItem(player, 0); var actions = new MuunActions();
        await actions.ExecuteAsync(player, 1, 10, pet.ItemSlot, 20);
        await actions.ExecuteAsync(player, 2, 9, 20, 40);
        Assert.That(pet.ItemSlot, Is.EqualTo(40));
        await actions.ExecuteAsync(player, 3, 3, 40, 1);
        if (fail) CashShopActionsTest.SaveFailingContext.Install(player, () => true);
        await actions.ExecuteAsync(player, 4, 9, 1, 22);
        Assert.That(pet.ItemSlot, Is.EqualTo(fail ? 1 : 22));
        Assert.That(player.SelectedCharacter!.MuunInventory!.Items.Single(), Is.SameAs(pet));
    }

    [Test]
    public async Task DirectedMovesRejectOccupiedStorageInvalidSlotsAndDuplicateEquipment()
    {
        var player = await Create(); var first = await BagItem(player, 0); var second = await BagItem(player, 0);
        var actions = new MuunActions(); var bagSlot = second.ItemSlot;
        await actions.ExecuteAsync(player, 1, 10, first.ItemSlot, 0);
        await actions.ExecuteAsync(player, 2, 10, bagSlot, 1);
        Assert.That(player.Inventory!.Items, Does.Contain(second));
        await actions.ExecuteAsync(player, 3, 10, bagSlot, 102);
        Assert.That(player.Inventory.Items, Does.Contain(second));
        await actions.ExecuteAsync(player, 4, 10, bagSlot, 20);
        await actions.ExecuteAsync(player, 5, 9, 0, 20);
        Assert.That(first.ItemSlot, Is.Zero);
        Assert.That(second.ItemSlot, Is.EqualTo(20));
        await actions.ExecuteAsync(player, 6, 9, 20, 1);
        Assert.That(second.ItemSlot, Is.EqualTo(20));
        await actions.ExecuteAsync(player, 7, 9, 20, 102);
        Assert.That(second.ItemSlot, Is.EqualTo(20));
        var third = await BagItem(player, 1);
        await actions.ExecuteAsync(player, 8, 10, third.ItemSlot, 20);
        Assert.That(player.Inventory.Items, Does.Contain(third));
    }

    [Test]
    public async Task FullCollectionRejectsBagReplacementButAllowsEmptyEquipmentSlot()
    {
        var player = await Create();
        player.GameContext.FeaturePlugIns.GetPlugIn<MuunFeaturePlugIn>()!.Configuration!.Capacity = 3;
        var first = await BagItem(player, 0); var stored = await BagItem(player, 1); var third = await BagItem(player, 1);
        var actions = new MuunActions();
        await actions.ExecuteAsync(player, 1, 10, first.ItemSlot, 0);
        await actions.ExecuteAsync(player, 2, 10, stored.ItemSlot, 2);
        await actions.ExecuteAsync(player, 3, 10, third.ItemSlot, 0);
        Assert.That(first.ItemSlot, Is.Zero);
        Assert.That(player.Inventory!.Items, Does.Contain(third));
        await actions.ExecuteAsync(player, 4, 10, third.ItemSlot, 1);
        Assert.That(third.ItemSlot, Is.EqualTo(1));
        Assert.That(player.Inventory.Items, Is.Empty);
    }

    [Test]
    public async Task TransfersPreserveIdentityAndRejectReplay()
    {
        var player = await Create(); var item = await BagItem(player, 0); var bagSlot = item.ItemSlot;
        var actions = new MuunActions();
        await actions.ExecuteAsync(player, 1, 1, bagSlot, 0);
        Assert.That(player.Inventory!.Items, Does.Not.Contain(item));
        Assert.That(player.SelectedCharacter!.MuunInventory!.Items.Single(), Is.SameAs(item));
        var slot = item.ItemSlot;
        await actions.ExecuteAsync(player, 2, 3, slot, 0);
        Assert.That(item.ItemSlot, Is.Zero);
        await actions.ExecuteAsync(player, 3, 2, 0, 0);
        await actions.ExecuteAsync(player, 3, 2, 0, 0);
        Assert.That(player.Inventory.Items.Count(i => i == item), Is.EqualTo(1));
        Assert.That(player.SelectedCharacter.MuunInventory.Items, Is.Empty);
    }
    [TestCase(false)] [TestCase(true)]
    public async Task SwitchingAndSwappingEquipmentPreservesItems(bool fail)
    {
        var player = await Create(); var first = await BagItem(player, 0); var second = await BagItem(player, 1);
        var actions = new MuunActions();
        await actions.ExecuteAsync(player, 1, 1, first.ItemSlot, 0);
        await actions.ExecuteAsync(player, 2, 1, second.ItemSlot, 0);
        await actions.ExecuteAsync(player, 3, 3, first.ItemSlot, 0);
        await actions.ExecuteAsync(player, 4, 3, 0, 1);
        Assert.That(first.ItemSlot, Is.EqualTo(1));
        await actions.ExecuteAsync(player, 5, 3, second.ItemSlot, 0);
        if (fail) CashShopActionsTest.SaveFailingContext.Install(player, () => true);
        await actions.ExecuteAsync(player, 6, 3, 1, 0);
        Assert.That(first.ItemSlot, Is.EqualTo(fail ? 1 : 0));
        Assert.That(second.ItemSlot, Is.EqualTo(fail ? 0 : 1));
        Assert.That(player.SelectedCharacter!.MuunInventory!.Items.Count, Is.EqualTo(2));
        Assert.That(player.Inventory!.Items, Is.Empty);
    }
    [TestCase(false)] [TestCase(true)]
    public async Task StoredCompanionCanReplaceEquippedCompanion(bool fail)
    {
        var player = await Create(); var first = await BagItem(player, 0); var second = await BagItem(player, 1);
        var actions = new MuunActions();
        await actions.ExecuteAsync(player, 1, 1, first.ItemSlot, 0);
        await actions.ExecuteAsync(player, 2, 1, second.ItemSlot, 0);
        await actions.ExecuteAsync(player, 3, 3, first.ItemSlot, 0);
        var storedSlot = second.ItemSlot;
        if (fail) CashShopActionsTest.SaveFailingContext.Install(player, () => true);
        await actions.ExecuteAsync(player, 4, 3, storedSlot, 0);
        Assert.That(first.ItemSlot, Is.EqualTo(fail ? 0 : storedSlot));
        Assert.That(second.ItemSlot, Is.EqualTo(fail ? storedSlot : 0));
        Assert.That(player.SelectedCharacter!.MuunInventory!.Items.Count, Is.EqualTo(2));
    }
    [Test]
    public async Task CompanionCanBeWithdrawnFromCollectionOrEitherEquipmentSlot()
    {
        var player = await Create(); var actions = new MuunActions(); uint request = 0;
        foreach (var equipment in new[] { -1, 0, 1 })
        {
            var pet = await BagItem(player, 0);
            await actions.ExecuteAsync(player, ++request, 1, pet.ItemSlot, 0);
            if (equipment >= 0) await actions.ExecuteAsync(player, ++request, 3, pet.ItemSlot, (byte)equipment);
            await actions.ExecuteAsync(player, ++request, 2, pet.ItemSlot, 0);
            Assert.That(player.Inventory!.Items.Count(i => i == pet), Is.EqualTo(1));
            Assert.That(player.SelectedCharacter!.MuunInventory!.Items, Is.Empty);
            await player.Inventory.RemoveItemAsync(pet);
        }
    }
    [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)]
    public async Task SaveFailuresRestoreLocations(int operation)
    {
        var player = await Create(); var item = await BagItem(player, 0); var actions = new MuunActions();
        if (operation != 1) await actions.ExecuteAsync(player, 1, 1, item.ItemSlot, 0);
        if (operation == 4) await actions.ExecuteAsync(player, 2, 3, item.ItemSlot, 0);
        var slot = item.ItemSlot;
        CashShopActionsTest.SaveFailingContext.Install(player, () => true);
        await actions.ExecuteAsync(player, 4, (byte)operation, slot, 0);
        Assert.That(item.ItemSlot, Is.EqualTo(slot));
        Assert.That(player.Inventory!.Items.Contains(item), Is.EqualTo(operation == 1));
        Assert.That(player.SelectedCharacter!.MuunInventory?.Items.Contains(item) ?? false, Is.EqualTo(operation != 1));
    }
    [TestCase(0, false)] [TestCase(1, false)] [TestCase(2, false)]
    [TestCase(0, true)] [TestCase(1, true)] [TestCase(2, true)]
    public async Task DuplicateFeedingCommitsOnceOrRollsBack(byte targetSlot, bool fail)
    {
        var player = await Create(); var first = await BagItem(player, 0); var second = await BagItem(player, 0); var actions = new MuunActions();
        await actions.ExecuteAsync(player, 1, 1, first.ItemSlot, 0); await actions.ExecuteAsync(player, 2, 1, second.ItemSlot, 0);
        var materialSlot = second.ItemSlot;
        if (targetSlot < 2) await actions.ExecuteAsync(player, 3, 3, first.ItemSlot, targetSlot);
        if (fail) CashShopActionsTest.SaveFailingContext.Install(player, () => true);
        await actions.ExecuteAsync(player, 4, 5, first.ItemSlot, materialSlot);
        await actions.ExecuteAsync(player, 4, 5, first.ItemSlot, materialSlot);
        Assert.That(first.Level, Is.EqualTo(fail ? 0 : 1));
        Assert.That(player.SelectedCharacter!.MuunInventory!.Items.Contains(second), Is.EqualTo(fail));
        Assert.That(player.Inventory!.Items, Is.Empty);
    }
    [TestCase(false)] [TestCase(true)]
    public async Task EvolutionAndRepairConserveMaterials(bool fail)
    {
        var player = await Create(); var pet = await BagItem(player, 0); var stone = await BagItem(player, 2); var actions = new MuunActions();
        await actions.ExecuteAsync(player, 1, 1, pet.ItemSlot, 0); pet.Level = 4;
        if (fail) CashShopActionsTest.SaveFailingContext.Install(player, () => true);
        var stoneSlot = stone.ItemSlot;
        await actions.ExecuteAsync(player, 2, 6, pet.ItemSlot, stoneSlot);
        Assert.That(pet.Definition!.Number, Is.EqualTo(fail ? 450 : 451));
        Assert.That(pet.Level, Is.EqualTo(fail ? 4 : 0));
        Assert.That(player.Inventory!.Items.Contains(stone), Is.EqualTo(fail));
    }
    [Test]
    public async Task BonusConditionsAndReinitializationDoNotStack()
    {
        var player = await Create(); var pet = await BagItem(player, 0); var actions = new MuunActions();
        pet.Definition!.BasePowerUpAttributes.Add(new Basic.ItemBasePowerUpDefinition { TargetAttribute = Stats.MaximumHealth, BaseValue = 50, AggregateType = AggregateType.AddRaw });
        var before = player.Attributes![Stats.MaximumHealth];
        await actions.ExecuteAsync(player, 1, 1, pet.ItemSlot, 0); await actions.ExecuteAsync(player, 2, 3, pet.ItemSlot, 0);
        Assert.That(player.Attributes[Stats.MaximumHealth], Is.EqualTo(before + 50));
        MuunBonuses.Refresh(player); Assert.That(player.Attributes[Stats.MaximumHealth], Is.EqualTo(before + 50));
        pet.Durability = 0; MuunBonuses.Refresh(player); Assert.That(player.Attributes[Stats.MaximumHealth], Is.EqualTo(before));
        pet.Durability = 100; MuunBonuses.Refresh(player); Assert.That(player.Attributes[Stats.MaximumHealth], Is.EqualTo(before + 50));
        pet.ExpiresAtUtc = DateTime.UtcNow.AddSeconds(-1); MuunBonuses.Refresh(player); Assert.That(player.Attributes[Stats.MaximumHealth], Is.EqualTo(before));
    }
    [Test]
    public async Task ForeignAndEquipmentBagSlotsAreRejected()
    {
        var player = await Create(); var item = await BagItem(player, 0); var actions = new MuunActions();
        await actions.ExecuteAsync(player, 1, 1, 0, 0); await actions.ExecuteAsync(player, 2, 2, 119, 0);
        Assert.That(player.Inventory!.Items.Single(), Is.SameAs(item));
    }
    [Test]
    public void SpecialPeriodAndDuplicateDefinitionsAreValidated()
    {
        var config = new MuunConfiguration(); var rule = config.Pets[0]; var now = DateTime.UtcNow;
        rule.SpecialMultiplier = 3; rule.SpecialUntilUtc = now.AddHours(1);
        Assert.That(MuunRules.Multiplier(rule, now), Is.EqualTo(3));
        Assert.That(MuunRules.Multiplier(rule, now.AddHours(2)), Is.EqualTo(1));
        config.Pets.Add(rule); Assert.That(MuunRules.Find(config, rule.ItemType), Is.Null);
    }
    [TestCase(false)] [TestCase(true)]
    public async Task DeathLifeLossAndRepairRollBackOnSaveFailure(bool fail)
    {
        var player = await Create(); var pet = await BagItem(player, 0); var actions = new MuunActions();
        await actions.ExecuteAsync(player, 1, 1, pet.ItemSlot, 0); await actions.ExecuteAsync(player, 2, 3, pet.ItemSlot, 0);
        var view = Mock.Get(player.ViewPlugIns.GetPlugIn<MUnique.OpenMU.GameLogic.Views.Inventory.IMuunView>()!);
        view.Invocations.Clear();
        if (fail) CashShopActionsTest.SaveFailingContext.Install(player, () => true);
        await new MuunFeaturePlugIn().AttackableGotKilledAsync(player, null);
        Assert.That(pet.Durability, Is.EqualTo(fail ? 100 : 99));
        view.Verify(v => v.ShowAsync(0, "Ready"), Times.Once);
    }
    [TestCase(false)] [TestCase(true)]
    public async Task RepairConsumesOnlyOneMaterial(bool fail)
    {
        var player = await Create(); var pet = await BagItem(player, 0); var actions = new MuunActions();
        var lifeDefinition = new Basic.ItemDefinition { Group = 14, Number = 16, Width = 1, Height = 1, Durability = 10, Name = "Jewel of Life" };
        player.GameContext.Configuration.Items.Add(lifeDefinition);
        var jewel = player.PersistenceContext.CreateNew<Item>(); jewel.Definition = lifeDefinition; jewel.Durability = 3;
        Assert.That(await player.Inventory!.AddItemAsync(jewel), Is.True);
        await actions.ExecuteAsync(player, 1, 1, pet.ItemSlot, 0); pet.Durability = 20;
        if (fail) CashShopActionsTest.SaveFailingContext.Install(player, () => true);
        await actions.ExecuteAsync(player, 2, 7, pet.ItemSlot, jewel.ItemSlot);
        Assert.That(pet.Durability, Is.EqualTo(fail ? 20 : 100));
        Assert.That(jewel.Durability, Is.EqualTo(fail ? 3 : 2));
    }
    [Test]
    public async Task AppearanceAndSnapshotUseIndependentPackets()
    {
        var (player, output) = CastleSiegeRemoteViewTestHelper.CreatePlayer();
        await new MUnique.OpenMU.GameServer.RemoteView.Inventory.MuunView(player).ShowAsync(7, "Ready");
        var bytes = output.ToArray();
        Assert.That(bytes[0], Is.EqualTo(0xC2)); Assert.That(bytes[3], Is.EqualTo(0xFD)); Assert.That(bytes[4], Is.EqualTo(5));
        using var json = System.Text.Json.JsonDocument.Parse(bytes.AsMemory(5));
        Assert.That(json.RootElement.GetProperty("RequestId").GetUInt32(), Is.EqualTo(7));
        Assert.That(json.RootElement.GetProperty("SupportsSlotMoves").GetBoolean(), Is.True);
    }
    [TestCase(false)]
    [TestCase(true)]
    public async Task ImportedCompanionsUseExistingItemsWithoutDuplicateRules(bool useMount)
    {
        var player = await Create();
        var settings = MuunFeaturePlugIn.Settings(player.GameContext)!;
        var definition = new Basic.ItemDefinition { Group = 13, Number = 313, Width = 2, Height = 2, Durability = 1, Name = "fighterpet_evol Skin" };
        var mount = new Basic.ItemDefinition { Group = 13, Number = 300, Width = 2, Height = 2, Durability = 1, Name = "Fenrir1 Mount" };
        player.GameContext.Configuration.Items.Add(definition);
        player.GameContext.Configuration.Items.Add(mount);
        var original = settings.Pets[0]; original.SpecialMultiplier = 3;
        var count = settings.Pets.Count;
        MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Items.ImportedMuunItems.Register(player.GameContext.Configuration, settings);
        MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Items.ImportedMuunItems.Register(player.GameContext.Configuration, settings);
        Assert.That(settings.Pets.Count, Is.EqualTo(count + 2));
        Assert.That(original.SpecialMultiplier, Is.EqualTo(3));
        Assert.That(MuunRules.Find(settings, 13 * 512 + 300), Is.Not.Null);
        if (useMount) definition = mount;
        var item = player.PersistenceContext.CreateNew<Item>(); item.Definition = definition; item.Level = 7; item.Durability = 1;
        Assert.That(await player.Inventory!.AddItemAsync(item), Is.True);
        var actions = new MuunActions();
        await actions.ExecuteAsync(player, 1, 1, item.ItemSlot, 0);
        Assert.That(player.SelectedCharacter!.MuunInventory!.Items.Single(), Is.SameAs(item));
        await actions.ExecuteAsync(player, 2, 3, item.ItemSlot, 0);
        Assert.That(item.ItemSlot, Is.Zero);
        await actions.ExecuteAsync(player, 3, 2, item.ItemSlot, 0);
        Assert.That(player.Inventory.Items.Single(), Is.SameAs(item));
        Assert.That(item.Level, Is.EqualTo(7));
        Assert.That(item.Definition, Is.SameAs(definition));
    }
    [TestCase(false)] [TestCase(true)]
    public async Task EggExchangeCommitsExactlyOnceOrRestoresMaterials(bool fail)
    {
        var player = await Create(); var config = MuunFeaturePlugIn.Settings(player.GameContext)!;
        config.Exchanges = [new() { MaterialItemType = 14 * 512 + 250, RequiredCount = 2, Rewards = [new()] }];
        var first = await BagItem(player, 2); var second = await BagItem(player, 2); var original = first.ItemSlot;
        if (fail) CashShopActionsTest.SaveFailingContext.Install(player, () => true);
        var actions = new MuunActions();
        await actions.ExecuteAsync(player, 1, 8, original, 0);
        await actions.ExecuteAsync(player, 1, 8, original, 0);
        Assert.That(player.Inventory!.Items.Count, Is.EqualTo(fail ? 2 : 0));
        Assert.That(player.SelectedCharacter!.MuunInventory!.Items.Count, Is.EqualTo(fail ? 0 : 1));
        if (fail) Assert.That(player.Inventory.GetItem(original), Is.SameAs(first));
    }
    [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
    public async Task InvalidEggExchangeCannotConsumeMaterials(int invalid)
    {
        var player = await Create(); var config = MuunFeaturePlugIn.Settings(player.GameContext)!;
        config.Exchanges = [new() { MaterialItemType = 14 * 512 + 250, Rewards = [new()] }];
        var egg = await BagItem(player, 2);
        if (invalid == 0) config.Exchanges[0].RequiredCount = 2;
        if (invalid == 1) config.Exchanges[0].Rewards[0].ItemType = 8191;
        if (invalid == 2) config.Exchanges[0].Rewards[0].Weight = 0;
        if (invalid == 3) egg.ExpiresAtUtc = DateTime.UtcNow.AddDays(1);
        await new MuunActions().ExecuteAsync(player, 1, 8, egg.ItemSlot, 0);
        Assert.That(player.Inventory!.Items.Single(), Is.SameAs(egg));
        Assert.That(player.SelectedCharacter!.MuunInventory?.Items.Count ?? 0, Is.Zero);
    }
    [TestCase(0.1, 3000, 4, true)] [TestCase(0, 3000, 4, false)]
    [TestCase(-1, 3000, 4, false)] [TestCase(2, 3000, 4, false)]
    [TestCase(0.1, 0, 4, false)] [TestCase(0.1, 3000, 50, false)]
    public void CombatSettingsRejectExcessiveRatesAndDamage(double factor, int interval, int range, bool allowed)
        => Assert.That(MuunCombat.CanAttack(new() { AttackDamageFactor = factor, AttackIntervalMilliseconds = interval, AttackRange = range }), Is.EqualTo(allowed));
    [Test]
    public async Task CombatAssistsOnlyRecentInRangeTargetsAndRespectsCooldown()
    {
        var player = await Create(); var pet = await BagItem(player, 0);
        var actions = new MuunActions(); await actions.ExecuteAsync(player, 1, 1, pet.ItemSlot, 0);
        await actions.ExecuteAsync(player, 2, 3, pet.ItemSlot, 0);
        player.IsAlive = true;
        player.Position = new(100, 100);
        player.CurrentMap!.Terrain.SafezoneMap[100, 100] = false;
        var definition = new Basic.MonsterDefinition { Id = Guid.NewGuid(), ObjectKind = MUnique.OpenMU.DataModel.Configuration.NpcObjectKind.Monster };
        definition.Attributes.Add(new Basic.MonsterAttribute { AttributeDefinition = Stats.MaximumHealth, Value = 100000 });
        definition.Attributes.Add(new Basic.MonsterAttribute { AttributeDefinition = Stats.DamageReceiveDecrement, Value = 1 });
        var spawn = new Basic.MonsterSpawnArea { MonsterDefinition = definition, X1 = 101, X2 = 101, Y1 = 100, Y2 = 100, Quantity = 1 };
        var monster = new MUnique.OpenMU.GameLogic.NPC.Monster(spawn, definition, player.CurrentMap, NullDropGenerator.Instance,
            new Mock<MUnique.OpenMU.GameLogic.INpcIntelligence>().Object, player.GameContext.PlugInManager, player.GameContext.PathFinderPool);
        monster.Initialize(); monster.Position = new(101, 100);
        player.CurrentMap.Terrain.SafezoneMap[101, 100] = false;
        var view = Mock.Get(player.ViewPlugIns.GetPlugIn<MUnique.OpenMU.GameLogic.Views.Inventory.IMuunView>()!);
        var combat = new MuunCombat();
        await combat.TickAsync(player);
        view.Verify(v => v.AttackAsync(player, monster, 0), Times.Never);
        combat.Record(monster, player);
        await combat.TickAsync(player); await combat.TickAsync(player);
        view.Verify(v => v.AttackAsync(player, monster, 0), Times.Once);
        monster.Position = new(110, 100);
        player.CurrentMap.Terrain.SafezoneMap[110, 100] = false;
        combat = new MuunCombat(); combat.Record(monster, player); await combat.TickAsync(player);
        view.Verify(v => v.AttackAsync(player, monster, 0), Times.Once);
        monster.Position = new(101, 100);
        player.CurrentMap.Terrain.SafezoneMap[100, 100] = true;
        combat = new MuunCombat(); combat.Record(monster, player); await combat.TickAsync(player);
        view.Verify(v => v.AttackAsync(player, monster, 0), Times.Once);
        player.CurrentMap.Terrain.SafezoneMap[100, 100] = false;
        combat = new MuunCombat(); combat.Record(monster, new MUnique.OpenMU.GameLogic.Pet.AttackerSurrogate(player, player.Attributes!));
        await combat.TickAsync(player);
        view.Verify(v => v.AttackAsync(player, monster, 0), Times.Once);
        monster.Dispose();
    }
    [TestCase(0, 24, 0, true)] [TestCase(0, 24, 23, true)] [TestCase(8, 18, 7, false)] [TestCase(8, 18, 8, true)] [TestCase(8, 18, 18, false)]
    [TestCase(22, 2, 23, true)] [TestCase(22, 2, 1, true)] [TestCase(22, 2, 2, false)] [TestCase(22, 2, 12, false)]
    public void DailyWindowWrapsPastMidnight(int from, int until, int hour, bool expected)
        => Assert.That(MuunRules.InHourWindow(new MuunDefinition { FromHourUtc = from, UntilHourUtc = until }, hour), Is.EqualTo(expected));
    [Test]
    public void LevelRangeTreatsZeroMaximumAsUnlimited()
    {
        var rule = new MuunDefinition { MinimumCharacterLevel = 10 };
        Assert.That(MuunRules.InLevelRange(rule, 9), Is.False);
        Assert.That(MuunRules.InLevelRange(rule, 400), Is.True);
        rule.MaximumCharacterLevel = 200;
        Assert.That(MuunRules.InLevelRange(rule, 200), Is.True);
        Assert.That(MuunRules.InLevelRange(rule, 201), Is.False);
    }
    [Test]
    public void SpecialPeriodShortensAttacksAndAddsFlatBonuses()
    {
        var now = new DateTime(2026, 11, 1, 12, 0, 0, DateTimeKind.Utc);
        var rule = new MuunDefinition { AttackIntervalMilliseconds = 3000, SpecialAttackIntervalReduction = 0.5, SpecialAddition = 5, SpecialFromUtc = now.AddHours(-1), SpecialUntilUtc = now.AddHours(1) };
        Assert.That(MuunRules.AttackInterval(rule, now), Is.EqualTo(1500));
        Assert.That(MuunRules.Addition(rule, now), Is.EqualTo(5));
        Assert.That(MuunRules.AttackInterval(rule, now.AddHours(2)), Is.EqualTo(3000));
        Assert.That(MuunRules.Addition(rule, now.AddHours(2)), Is.Zero);
        rule.AttackIntervalMilliseconds = 600; rule.SpecialAttackIntervalReduction = 0.9;
        Assert.That(MuunRules.AttackInterval(rule, now), Is.EqualTo(MuunCombat.MinimumAttackIntervalMilliseconds));
    }
    [Test]
    public async Task InactiveReasonReportsScheduleConditions()
    {
        var player = await Create(); var item = await BagItem(player, 0); item.ItemSlot = 0;
        var monday = new DateTime(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc);
        var rule = new MuunDefinition { Days = [DayOfWeek.Sunday] };
        Assert.That(MuunRules.InactiveReason(player, item, rule, monday), Is.EqualTo(MuunInactiveReason.Day));
        rule.Days.Clear(); rule.FromHourUtc = 13; rule.UntilHourUtc = 14;
        Assert.That(MuunRules.InactiveReason(player, item, rule, monday), Is.EqualTo(MuunInactiveReason.Hour));
        rule.FromHourUtc = 12;
        Assert.That(MuunRules.InactiveReason(player, item, rule, monday), Is.EqualTo(MuunInactiveReason.None));
        item.ItemSlot = MuunRules.EquipmentCount;
        Assert.That(MuunRules.InactiveReason(player, item, rule, monday), Is.EqualTo(MuunInactiveReason.NotEquipped));
    }
    private static async Task<Item> BagItem(Player player, int index)
    {
        var item = player.PersistenceContext.CreateNew<Item>(); item.Definition = player.GameContext.Configuration.Items.ElementAt(index); item.Durability = item.Definition.Durability;
        Assert.That(await player.Inventory!.AddItemAsync(item), Is.True); return item;
    }
    private static async Task<Player> Create()
    {
        var player = await PlayerTestHelper.CreatePlayerAsync();
        player.GameContext.FeaturePlugIns.AddPlugIn(new MuunFeaturePlugIn { Configuration = new MuunConfiguration() }, true);
        var slot = new Basic.ItemSlotType(); slot.ItemSlots.Add(249);
        player.GameContext.Configuration.Items.Add(new Basic.ItemDefinition { Group = 13, Number = 450, Width = 2, Height = 2, Durability = 100, Name = "Magic Muun", ItemSlot = slot });
        player.GameContext.Configuration.Items.Add(new Basic.ItemDefinition { Group = 13, Number = 451, Width = 2, Height = 2, Durability = 100, Name = "Evolved Magic Muun", ItemSlot = slot });
        player.GameContext.Configuration.Items.Add(new Basic.ItemDefinition { Group = 14, Number = 250, Width = 1, Height = 1, Durability = 1, Name = "Muun Evolution Stone" });
        await player.PlayerState.TryAdvanceToAsync(PlayerState.EnteredWorld); return player;
    }
}
