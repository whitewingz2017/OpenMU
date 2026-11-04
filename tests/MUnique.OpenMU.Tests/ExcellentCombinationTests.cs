namespace MUnique.OpenMU.Tests;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.PlayerActions.Items;
using Moq;
using MUnique.OpenMU.GameLogic.Views.Inventory;
using Basic = MUnique.OpenMU.Persistence.BasicModel;
[TestFixture]
internal class ExcellentCombinationTests
{
    private static async Task<(Player Player, Item Primary, Item Secondary, IncreasableItemOption[] Options)> CreateAsync(int count = 1)
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        player.GameContext.FeaturePlugIns.AddPlugIn(new ExcellentCombinationFeaturePlugIn { Configuration = new ExcellentCombinationConfiguration { BaseRate = 100, MaximumRate = 100 } }, true);
        await player.PlayerState.TryAdvanceToAsync(PlayerState.EnteredWorld).ConfigureAwait(false);
        var slot = new Basic.ItemSlotType(); slot.ItemSlots.Add(3);
        var definition = new Basic.ItemDefinition { Group = 8, Number = 1, Width = 2, Height = 2, Name = "Armor", ItemSlot = slot };
        var pool = new Basic.ItemOptionDefinition();
        var options = Enumerable.Range(0, 3).Select(i => new Basic.IncreasableItemOption { Id = Guid.NewGuid(), Number = i + 1, OptionType = ItemOptionTypes.Excellent }).ToArray();
        foreach (var option in options) pool.PossibleOptions.Add(option);
        definition.PossibleItemOptions.Add(pool);
        player.GameContext.Configuration.Items.Add(definition);
        var primary = player.PersistenceContext.CreateNew<Item>(); primary.Definition = definition; primary.Level = 9; primary.Durability = 23; primary.SocketCount = 0;
        var secondary = player.PersistenceContext.CreateNew<Item>(); secondary.Definition = definition;
        for (int i = 0; i < count; ++i) { var link = player.PersistenceContext.CreateNew<ItemOptionLink>(); link.ItemOption = options[i]; link.Level = 1; primary.ItemOptions.Add(link); }
        var material = player.PersistenceContext.CreateNew<ItemOptionLink>(); material.ItemOption = options[2]; material.Level = 1; secondary.ItemOptions.Add(material);
        Assert.That(await player.Inventory!.AddItemAsync(primary).ConfigureAwait(false), Is.True);
        Assert.That(await player.Inventory.AddItemAsync(secondary).ConfigureAwait(false), Is.True);
        player.SelectedCharacter!.Inventory!.Money = 2_000_000;
        return (player, primary, secondary, options);
    }
    [TestCase(1)] [TestCase(2)]
    public async Task SuccessfulCombinationPreservesMetadataAndRejectsReplay(int count)
    {
        var (p, primary, secondary, options) = await CreateAsync(count).ConfigureAwait(false);
        var action = new ExcellentCombinationActions();var a = primary.ItemSlot;var b = secondary.ItemSlot;
        await action.ExecuteAsync(p, 1, 0, a, b).ConfigureAwait(false);
        await action.ExecuteAsync(p, 2, 1, a, b).ConfigureAwait(false);
        await action.ExecuteAsync(p, 2, 1, a, b).ConfigureAwait(false);
        await action.ExecuteAsync(p, 3, 1, a, b).ConfigureAwait(false);
        Assert.That(primary.ItemOptions.Count, Is.EqualTo(2));
        Assert.That(primary.ItemOptions.Any(o => o.ItemOption == options[2]), Is.True);
        Assert.That(p.Inventory!.Items, Does.Not.Contain(secondary));
        Assert.That(p.Inventory.GetItem(a), Is.SameAs(primary));
        Assert.That(p.SelectedCharacter!.Inventory!.Money, Is.EqualTo(1_000_000));
        Assert.That(primary.Level, Is.EqualTo(9));Assert.That(primary.Durability, Is.EqualTo(23));Assert.That(primary.SocketCount, Is.EqualTo(0));
    }
    [Test]
    public async Task DuplicateMultipleAndUnsupportedOptionsAreRejected()
    {
        var (_, a, b, options) = await CreateAsync().ConfigureAwait(false);
        b.ItemOptions.Single().ItemOption = options[0];Assert.That(ExcellentCombinationActions.Validate(a,b), Is.Not.Null);
        b.ItemOptions.Single().ItemOption = new Basic.IncreasableItemOption { Id = Guid.NewGuid(), OptionType = ItemOptionTypes.Excellent };Assert.That(ExcellentCombinationActions.Validate(a,b), Is.Not.Null);
        b.ItemOptions.Single().ItemOption = options[2];
        b.ItemOptions.Add(new Basic.ItemOptionLink { ItemOption = options[1] });Assert.That(ExcellentCombinationActions.Validate(a,b), Is.Not.Null);
        b.ItemOptions.Remove(b.ItemOptions.Last());b.Definition = new Basic.ItemDefinition { Group = 8, Number = 2 };Assert.That(ExcellentCombinationActions.Validate(a,b), Is.Not.Null);
    }
    [TestCase(true)] [TestCase(false)]
    public async Task InsufficientZenOrMissingMaterialNeverConsumes(bool noMoney)
    {
        var (p,a,b,_) = await CreateAsync().ConfigureAwait(false);var action = new ExcellentCombinationActions();var slot=b.ItemSlot;
        await action.ExecuteAsync(p,1,0,a.ItemSlot,slot).ConfigureAwait(false);
        if(noMoney) p.SelectedCharacter!.Inventory!.Money = 0;
        else await p.Inventory!.RemoveItemAsync(b).ConfigureAwait(false);
        await action.ExecuteAsync(p,2,1,a.ItemSlot,slot).ConfigureAwait(false);
        Assert.That(a.ItemOptions.Count, Is.EqualTo(1));Assert.That(p.SelectedCharacter!.Inventory!.Money,Is.EqualTo(noMoney ? 0 : 2_000_000));
        if(noMoney) Assert.That(p.Inventory!.Items,Does.Contain(b));
    }
    [Test]
    public async Task ChangedOptionAfterQuoteIsRejected()
    {
        var (p,a,b,options)=await CreateAsync().ConfigureAwait(false);var action=new ExcellentCombinationActions();
        await action.ExecuteAsync(p,1,0,a.ItemSlot,b.ItemSlot).ConfigureAwait(false);
        b.ItemOptions.Single().ItemOption=options[1];
        await action.ExecuteAsync(p,2,1,a.ItemSlot,b.ItemSlot).ConfigureAwait(false);
        Assert.That(a.ItemOptions.Count,Is.EqualTo(1));Assert.That(p.Inventory!.Items,Does.Contain(b));Assert.That(p.SelectedCharacter!.Inventory!.Money,Is.EqualTo(2_000_000));
    }
    [Test]
    public async Task CombinationPreservesNonExcellentOptionLinks()
    {
        var (p,a,b,_)=await CreateAsync().ConfigureAwait(false);
        var harmony=p.PersistenceContext.CreateNew<ItemOptionLink>();harmony.ItemOption=new Basic.IncreasableItemOption { Id=Guid.NewGuid(),OptionType=ItemOptionTypes.HarmonyOption };harmony.Level=7;a.ItemOptions.Add(harmony);
        var actions=new ExcellentCombinationActions();
        await actions.ExecuteAsync(p,1,0,a.ItemSlot,b.ItemSlot).ConfigureAwait(false);
        await actions.ExecuteAsync(p,2,1,a.ItemSlot,b.ItemSlot).ConfigureAwait(false);
        Assert.That(a.ItemOptions.Count,Is.EqualTo(3));Assert.That(a.ItemOptions,Does.Contain(harmony));Assert.That(harmony.Level,Is.EqualTo(7));
    }
    [Test]
    public async Task ConfirmationWithoutQuoteCannotConsumeItems()
    {
        var (p,a,b,_)=await CreateAsync().ConfigureAwait(false);
        await new ExcellentCombinationActions().ExecuteAsync(p,1,1,a.ItemSlot,b.ItemSlot).ConfigureAwait(false);
        Assert.That(a.ItemOptions.Count,Is.EqualTo(1));Assert.That(p.Inventory!.Items,Does.Contain(b));Assert.That(p.SelectedCharacter!.Inventory!.Money,Is.EqualTo(2_000_000));
    }
    [TestCase(1)] [TestCase(2)]
    public async Task SaveFailureRestoresMoneyOptionsAndMaterial(int count)
    {
        var (p,a,b,_) = await CreateAsync(count).ConfigureAwait(false);var action=new ExcellentCombinationActions();var slot=b.ItemSlot;var original=a.ItemOptions.ToArray();
        await action.ExecuteAsync(p,1,0,a.ItemSlot,slot).ConfigureAwait(false);
        CashShopActionsTest.SaveFailingContext.Install(p,()=>true);
        await action.ExecuteAsync(p,2,1,a.ItemSlot,slot).ConfigureAwait(false);
        Assert.That(p.Inventory!.GetItem(slot),Is.SameAs(b));Assert.That(a.ItemOptions,Is.EquivalentTo(original));Assert.That(p.SelectedCharacter!.Inventory!.Money,Is.EqualTo(2_000_000));
    }

    [TestCase(0, 35)]
    [TestCase(1, 45)]
    [TestCase(2, 55)]
    [TestCase(3, 65)]
    [TestCase(4, 75)]
    public void RatesReachCap(int cores, int expected)
    {
        var settings = new ExcellentCombinationConfiguration();
        Assert.That(settings.IsValid, Is.True);
        Assert.That(settings.MaximumCores, Is.EqualTo(4));
        Assert.That(settings.Rate(cores), Is.EqualTo(expected));
    }

    [TestCase(0, false)]
    [TestCase(0, true)]
    [TestCase(100, false)]
    [TestCase(100, true)]
    public async Task BothRollOutcomesConsumeOnlySelectedCoresAndRestoreOnSaveFailure(int rate, bool fail)
    {
        var (p, a, b, _) = await CreateAsync().ConfigureAwait(false);
        p.GameContext.FeaturePlugIns.GetPlugIn<ExcellentCombinationFeaturePlugIn>()!.Configuration = new ExcellentCombinationConfiguration();
        var core = p.PersistenceContext.CreateNew<Item>();
        core.Definition = new Basic.ItemDefinition { Group = 14, Number = 252, Width = 1, Height = 1, Durability = 255, Name = "Resonance Core" };
        core.Durability = 3;
        Assert.That(await p.Inventory!.AddItemAsync(core).ConfigureAwait(false), Is.True);
        var coreSlot = core.ItemSlot;
        var donorSlot = b.ItemSlot;
        var original = a.ItemOptions.ToArray();
        var randomizer = new Mock<IRandomizer>();
        randomizer.Setup(r => r.NextInt(0, 100)).Returns(rate == 100 ? 54 : 55);
        var action = new ExcellentCombinationActions(randomizer.Object);
        const byte selected = 2;
        await action.ExecuteAsync(p, 1, 0, a.ItemSlot, donorSlot, selected).ConfigureAwait(false);
        if (fail) CashShopActionsTest.SaveFailingContext.Install(p, () => true);
        await action.ExecuteAsync(p, 2, 1, a.ItemSlot, donorSlot, selected).ConfigureAwait(false);
        Assert.That(core.Durability, Is.EqualTo(fail ? 3 : 1));
        Assert.That(p.Inventory.GetItem(coreSlot), Is.SameAs(core));
        Assert.That(p.Inventory.Items.Contains(b), Is.EqualTo(fail));
        Assert.That(p.SelectedCharacter!.Inventory!.Money, Is.EqualTo(fail ? 2_000_000 : 1_000_000));
        if (fail || rate == 0) Assert.That(a.ItemOptions, Is.EquivalentTo(original));
        else Assert.That(a.ItemOptions.Count, Is.EqualTo(2));
        randomizer.Verify(r => r.NextInt(0, 100), Times.Once);
        if (!fail)
        {
            var view = Mock.Get(p.ViewPlugIns.GetPlugIn<IExcellentCombinationView>()!);
            view.Verify(v => v.ShowAsync(2, It.IsAny<string>(), false, rate == 100, true,
                It.Is<ExcellentCombinationConfiguration>(c => c.BaseRate == 35), 1, 2, a), Times.Once);
        }
    }

    [TestCase("count")]
    [TestCase("stack")]
    [TestCase("settings")]
    public async Task ChangedCoreQuoteCannotConsume(string changed)
    {
        var (p, a, b, _) = await CreateAsync().ConfigureAwait(false);
        var settings = new ExcellentCombinationConfiguration();
        p.GameContext.FeaturePlugIns.GetPlugIn<ExcellentCombinationFeaturePlugIn>()!.Configuration = settings;
        var core = p.PersistenceContext.CreateNew<Item>();
        core.Definition = new Basic.ItemDefinition { Group = 14, Number = 252, Width = 1, Height = 1, Durability = 255, Name = "Resonance Core" };
        core.Durability = 5;
        await p.Inventory!.AddItemAsync(core).ConfigureAwait(false);
        var action = new ExcellentCombinationActions();
        await action.ExecuteAsync(p, 1, 0, a.ItemSlot, b.ItemSlot, 4).ConfigureAwait(false);
        if (changed == "stack") core.Durability = 4;
        if (changed == "settings") settings.ZenCost = 1;
        await action.ExecuteAsync(p, 2, 1, a.ItemSlot, b.ItemSlot, changed == "count" ? (byte)3 : (byte)4).ConfigureAwait(false);
        Assert.That(p.Inventory.Items, Does.Contain(b));
        Assert.That(a.ItemOptions.Count, Is.EqualTo(1));
        Assert.That(p.SelectedCharacter!.Inventory!.Money, Is.EqualTo(2_000_000));
        Assert.That(core.Durability, Is.EqualTo(changed == "stack" ? 4 : 5));
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task AncientOrSocketGearIsRejected(bool ancient)
    {
        var (_, a, b, _) = await CreateAsync().ConfigureAwait(false);
        if (ancient) a.ItemSetGroups.Add(new Basic.ItemOfItemSet { AncientSetDiscriminator = 1 });
        else b.SocketCount = 1;
        Assert.That(ExcellentCombinationActions.Validate(a, b), Is.Not.Null);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task FullCoreStacksRestoreOrDisappearAtomically(bool fail)
    {
        var (p, a, b, _) = await CreateAsync().ConfigureAwait(false);
        p.GameContext.FeaturePlugIns.GetPlugIn<ExcellentCombinationFeaturePlugIn>()!.Configuration = new ExcellentCombinationConfiguration();
        var core = p.PersistenceContext.CreateNew<Item>();
        core.Definition = new Basic.ItemDefinition { Group = 14, Number = 252, Width = 1, Height = 1, Durability = 255, Name = "Resonance Core" };
        core.Durability = 4;
        await p.Inventory!.AddItemAsync(core).ConfigureAwait(false);
        var slot = core.ItemSlot;
        var randomizer = new Mock<IRandomizer>();
        randomizer.Setup(r => r.NextInt(0, 100)).Returns(75);
        var action = new ExcellentCombinationActions(randomizer.Object);
        await action.ExecuteAsync(p, 1, 0, a.ItemSlot, b.ItemSlot, 4).ConfigureAwait(false);
        if (fail) CashShopActionsTest.SaveFailingContext.Install(p, () => true);
        await action.ExecuteAsync(p, 2, 1, a.ItemSlot, b.ItemSlot, 4).ConfigureAwait(false);
        Assert.That(p.Inventory.GetItem(slot), fail ? Is.SameAs(core) : Is.Null);
        Assert.That(core.Durability, Is.EqualTo(4));
        Assert.That(a.ItemOptions.Count, Is.EqualTo(1));
        Assert.That(p.Inventory.Items.Contains(b), Is.EqualTo(fail));
    }

    [TestCase(0, 1)]
    [TestCase(3, 4)]
    [TestCase(5, 5)]
    public async Task MissingOrExcessCoresRejectWithoutConsumption(int available, byte selected)
    {
        var (p, a, b, _) = await CreateAsync().ConfigureAwait(false);
        p.GameContext.FeaturePlugIns.GetPlugIn<ExcellentCombinationFeaturePlugIn>()!.Configuration = new ExcellentCombinationConfiguration();
        var core = p.PersistenceContext.CreateNew<Item>();
        core.Definition = new Basic.ItemDefinition { Group = 14, Number = 252, Width = 1, Height = 1, Durability = 255, Name = "Resonance Core" };
        core.Durability = available;
        await p.Inventory!.AddItemAsync(core).ConfigureAwait(false);
        var action = new ExcellentCombinationActions();
        await action.ExecuteAsync(p, 1, 0, a.ItemSlot, b.ItemSlot, selected).ConfigureAwait(false);
        await action.ExecuteAsync(p, 2, 1, a.ItemSlot, b.ItemSlot, selected).ConfigureAwait(false);
        Assert.That(p.Inventory.Items, Does.Contain(b));
        Assert.That(core.Durability, Is.EqualTo(available));
        Assert.That(p.SelectedCharacter!.Inventory!.Money, Is.EqualTo(2_000_000));
    }

    [TestCase(-1, 75, 10)]
    [TestCase(35, 30, 10)]
    [TestCase(35, 101, 10)]
    [TestCase(35, 75, 0)]
    public void InvalidAdminRatesAreRejected(int basis, int cap, int bonus)
    {
        var settings = new ExcellentCombinationConfiguration { BaseRate = basis, MaximumRate = cap, BonusPerCore = bonus };
        Assert.That(settings.IsValid, Is.False);
        Assert.That(settings.Validate(new System.ComponentModel.DataAnnotations.ValidationContext(settings)), Is.Not.Empty);
    }
}
