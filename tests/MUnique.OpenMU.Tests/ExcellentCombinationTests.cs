namespace MUnique.OpenMU.Tests;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.PlayerActions.Items;
using Basic = MUnique.OpenMU.Persistence.BasicModel;
[TestFixture]
public class ExcellentCombinationTests
{
    private static async Task<(Player Player, Item Primary, Item Secondary, IncreasableItemOption[] Options)> CreateAsync(int count = 1)
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        await player.PlayerState.TryAdvanceToAsync(PlayerState.EnteredWorld).ConfigureAwait(false);
        var slot = new Basic.ItemSlotType(); slot.ItemSlots.Add(3);
        var definition = new Basic.ItemDefinition { Group = 8, Number = 1, Width = 2, Height = 2, Name = "Armor", ItemSlot = slot };
        var pool = new Basic.ItemOptionDefinition();
        var options = Enumerable.Range(0, 3).Select(i => new Basic.IncreasableItemOption { Id = Guid.NewGuid(), Number = i + 1, OptionType = ItemOptionTypes.Excellent }).ToArray();
        foreach (var option in options) pool.PossibleOptions.Add(option);
        definition.PossibleItemOptions.Add(pool);
        player.GameContext.Configuration.Items.Add(definition);
        var primary = player.PersistenceContext.CreateNew<Item>(); primary.Definition = definition; primary.Level = 9; primary.Durability = 23; primary.SocketCount = 2;
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
        Assert.That(primary.Level, Is.EqualTo(9));Assert.That(primary.Durability, Is.EqualTo(23));Assert.That(primary.SocketCount, Is.EqualTo(2));
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
}
