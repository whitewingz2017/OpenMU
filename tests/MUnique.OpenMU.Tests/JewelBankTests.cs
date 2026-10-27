// <copyright file="JewelBankTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using System.Text.Json;
using MUnique.OpenMU.GameServer.RemoteView.Inventory;
using Moq;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.JewelBank;
using MUnique.OpenMU.GameLogic.Views.Inventory;
using BasicModel = MUnique.OpenMU.Persistence.BasicModel;

/// <summary>Checks inventory and ledger conservation across jewel bank operations.</summary>
[TestFixture]
public class JewelBankTests
{
    private static readonly JewelBankConfiguration Configuration = new() { Items = [7181], MaximumBalance = 100 };

    /// <summary>Deposits bundles and singles; duplicate requests cannot mint jewels.</summary>
    [Test]
    public async Task DepositAndWithdrawPreserveCountAsync()
    {
        var player = await CreateAsync().ConfigureAwait(false);
        var single = player.GameContext.Configuration.Items.First();
        var mixed = player.GameContext.Configuration.Items.Last();
        await AddAsync(player, single).ConfigureAwait(false);
        await AddAsync(player, mixed, 2).ConfigureAwait(false);
        var actions = new JewelBankActions();
        await actions.ExecuteAsync(player, Configuration, 1, 1, 7181, 0).ConfigureAwait(false);
        Assert.That(Balance(player), Is.EqualTo(31));
        Assert.That(player.Inventory!.Items, Is.Empty);
        await actions.ExecuteAsync(player, Configuration, 2, 2, 7181, 10).ConfigureAwait(false);
        await actions.ExecuteAsync(player, Configuration, 2, 2, 7181, 10).ConfigureAwait(false);
        Assert.That(Balance(player), Is.EqualTo(21));
        Assert.That(player.Inventory.Items.Count(), Is.EqualTo(10));
        Assert.That(player.Inventory.Items.All(i => i.Definition == single && i.Level == 0), Is.True);
    }

    /// <summary>Bank limits leave whole bundles in inventory.</summary>
    [Test]
    public async Task DepositRespectsLimitAsync()
    {
        var player = await CreateAsync().ConfigureAwait(false);
        player.Account!.JewelBankBalances = "{\"7181:0\":95}";
        await AddAsync(player, player.GameContext.Configuration.Items.Last(), 1).ConfigureAwait(false);
        await AddAsync(player, player.GameContext.Configuration.Items.First()).ConfigureAwait(false);
        await new JewelBankActions().ExecuteAsync(player, Configuration, 1, 1, 7181, 0).ConfigureAwait(false);
        Assert.That(Balance(player), Is.EqualTo(96));
        Assert.That(player.Inventory!.Items.Single().Level, Is.EqualTo(1));
    }

    /// <summary>Requests during NPC or trade windows do not modify inventory.</summary>
    [Test]
    public async Task InvalidStateAndAmountAreRejectedAsync()
    {
        var player = await CreateAsync().ConfigureAwait(false);
        player.Account!.JewelBankBalances = "{\"7181:0\":30}";
        var actions = new JewelBankActions();
        await actions.ExecuteAsync(player, Configuration, 1, 2, 7181, uint.MaxValue).ConfigureAwait(false);
        await player.PlayerState.TryAdvanceToAsync(PlayerState.TradeRequested).ConfigureAwait(false);
        await actions.ExecuteAsync(player, Configuration, 2, 2, 7181, 1).ConfigureAwait(false);
        Assert.That(Balance(player), Is.EqualTo(30));
        Assert.That(player.Inventory!.Items, Is.Empty);
    }

    /// <summary>Withdraw-all removes only the count that fits.</summary>
    [Test]
    public async Task FullInventoryCannotConsumeBankBalanceAsync()
    {
        var player = await CreateAsync().ConfigureAwait(false);
        player.Account!.JewelBankBalances = "{\"7181:0\":30}";
        while (player.Inventory!.FreeSlots.Any())
        {
            await AddAsync(player, player.GameContext.Configuration.Items.Last()).ConfigureAwait(false);
        }

        var before = player.Inventory.Items.Count();
        await new JewelBankActions().ExecuteAsync(player, Configuration, 1, 2, 7181, 0).ConfigureAwait(false);
        Assert.That(Balance(player), Is.EqualTo(30));
        Assert.That(player.Inventory.Items.Count(), Is.EqualTo(before));
    }

    /// <summary>A failed withdrawal save restores the ledger and removes tentative items.</summary>
    [Test]
    public async Task WithdrawalSaveFailureRollsBackAsync()
    {
        var player = await CreateAsync().ConfigureAwait(false);
        player.Account!.JewelBankBalances = "{\"7181:0\":30}";
        CashShopActionsTest.SaveFailingContext.Install(player, () => true);
        Assert.ThrowsAsync<InvalidOperationException>(async () => await new JewelBankActions().ExecuteAsync(player, Configuration, 1, 2, 7181, 10).ConfigureAwait(false));
        Assert.That(Balance(player), Is.EqualTo(30));
        Assert.That(player.Inventory!.Items, Is.Empty);
    }

    /// <summary>A failed deposit save restores the original inventory slot and count.</summary>
    [Test]
    public async Task DepositSaveFailureRollsBackAsync()
    {
        var player = await CreateAsync().ConfigureAwait(false);
        var item = await AddAsync(player, player.GameContext.Configuration.Items.First()).ConfigureAwait(false);
        byte slot = item.ItemSlot;
        CashShopActionsTest.SaveFailingContext.Install(player, () => true);
        Assert.ThrowsAsync<InvalidOperationException>(async () => await new JewelBankActions().ExecuteAsync(player, Configuration, 1, 1, 7181, 0).ConfigureAwait(false));
        Assert.That(Balance(player), Is.Zero);
        Assert.That(player.Inventory!.GetItem(slot), Is.SameAs(item));
    }

    /// <summary>The custom client receives a valid C2 frame and JSON confirmation.</summary>
    [Test]
    public async Task SnapshotWireFormatAsync()
    {
        var (player, output) = CastleSiegeRemoteViewTestHelper.CreatePlayer();
        await new JewelBankView(player).ShowAsync(7, "Ready", [new(7181, "Jewel of Bless", 42, 3, 100)]).ConfigureAwait(false);
        var data = output.ToArray();
        Assert.That(data[0], Is.EqualTo(0xC2));
        Assert.That((data[1] << 8) + data[2], Is.EqualTo(data.Length));
        Assert.That(data[3], Is.EqualTo(0xFA));
        Assert.That(data[4], Is.EqualTo(0x19));
        using var json = JsonDocument.Parse(data.AsMemory(5));
        Assert.That(json.RootElement.GetProperty("RequestId").GetUInt32(), Is.EqualTo(7));
        Assert.That(json.RootElement.GetProperty("Rows")[0].GetProperty("Balance").GetInt32(), Is.EqualTo(42));
    }

    /// <summary>Ledger labels contain only the neutral jewel name, not serialized translations.</summary>
    [Test]
    public async Task SnapshotUsesOnlyNeutralJewelNameAsync()
    {
        var player = await CreateAsync().ConfigureAwait(false);
        player.GameContext.Configuration.Items.First().Name = new MUnique.OpenMU.Interfaces.LocalizedString("Jewel of Bless||de=Segen||@src=ItemNames/JewelOfBless");
        var view = Mock.Get(player.ViewPlugIns.GetPlugIn<IJewelBankView>()!);
        await new JewelBankActions().ExecuteAsync(player, Configuration, 1, 0, 0, 0).ConfigureAwait(false);
        view.Verify(v => v.ShowAsync(1, It.IsAny<string>(), It.Is<IReadOnlyList<MUnique.OpenMU.GameLogic.JewelBank.JewelBankRow>>(rows => rows.Count == 1 && rows[0].Name == "Jewel of Bless")), Times.Once);
    }

    private static int Balance(Player player)
    {
        var ledger = JsonSerializer.Deserialize<Dictionary<string, int>>(player.Account!.JewelBankBalances)!;
        return ledger.GetValueOrDefault("7181:0");
    }

    private static async ValueTask<Item> AddAsync(Player player, ItemDefinition definition, byte level = 0)
    {
        var item = player.PersistenceContext.CreateNew<Item>();
        item.Definition = definition;
        item.Durability = 1;
        item.Level = level;
        Assert.That(await player.Inventory!.AddItemAsync(item).ConfigureAwait(false), Is.True);
        return item;
    }

    private static async ValueTask<Player> CreateAsync()
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        var single = new BasicModel.ItemDefinition { Group = 14, Number = 13, Width = 1, Height = 1, Durability = 1, Name = "Jewel of Bless" };
        var bundle = new BasicModel.ItemDefinition { Group = 12, Number = 30, Width = 1, Height = 1, Durability = 1, Name = "Bundle of Bless" };
        Mock.Get(player.GameContext.Configuration).Setup(c => c.Items).Returns(new List<ItemDefinition> { single, bundle });
        Mock.Get(player.GameContext.Configuration).Setup(c => c.JewelMixes).Returns(new List<JewelMix> { new() { SingleJewel = single, MixedJewel = bundle } });
        player.Account!.JewelBankBalances = "{}";
        await player.PlayerState.TryAdvanceToAsync(PlayerState.EnteredWorld).ConfigureAwait(false);
        return player;
    }
}
