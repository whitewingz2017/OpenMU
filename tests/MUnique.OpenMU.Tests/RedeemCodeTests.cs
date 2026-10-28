// <copyright file="RedeemCodeTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using System.Reflection;
using System.Text.Json;
using System.Threading;
using Moq;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.RedeemCodes;
using MUnique.OpenMU.Persistence;
using Basic = MUnique.OpenMU.Persistence.BasicModel;

/// <summary>Gift-code validation, duplicate protection and rollback of inventory and currency rewards.</summary>
[TestFixture]
public class RedeemCodeTests
{
    /// <summary>Successful rewards and the receipt persist; a new session cannot claim again.</summary>
    [Test]
    public async Task RewardAndDuplicateAsync()
    {
        var (player, db) = await CreateAsync().ConfigureAwait(false);
        var result = await new RedeemCodeActions().RedeemAsync(player, " gift-2026 ").ConfigureAwait(false);
        Assert.That(result, Does.Contain("successfully"));
        Assert.That(player.Inventory!.Items.Count(), Is.EqualTo(2));
        Assert.That(player.Account!.WCoinC, Is.EqualTo(50));
        Assert.That(player.Account.GoblinPoints, Is.EqualTo(10));
        Assert.That(db.Code.UsedCount, Is.EqualTo(1));
        Assert.That(db.Claims.Count, Is.EqualTo(1));
        result = await new RedeemCodeActions().RedeemAsync(player, "GIFT-2026").ConfigureAwait(false);
        Assert.That(result, Does.Contain("already redeemed"));
        Assert.That(player.Inventory.Items.Count(), Is.EqualTo(2));
        Assert.That(db.Code.UsedCount, Is.EqualTo(1));
    }

    /// <summary>Disabled, expired and exhausted codes grant nothing.</summary>
    [TestCase("disabled")]
    [TestCase("expired")]
    [TestCase("limit")]
    public async Task UnavailableCodeAsync(string reason)
    {
        var (player, db) = await CreateAsync().ConfigureAwait(false);
        if (reason == "disabled") db.Code.Enabled = false;
        if (reason == "expired") db.Code.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);
        if (reason == "limit") { db.Code.MaximumUses = 1; db.Code.UsedCount = 1; }
        var result = await new RedeemCodeActions().RedeemAsync(player, "GIFT-2026").ConfigureAwait(false);
        Assert.That(result, Does.Contain("disabled, expired or fully claimed"));
        Assert.That(player.Inventory!.Items, Is.Empty);
        Assert.That(db.Claims, Is.Empty);
    }

    /// <summary>A failed save restores all balances and removes pending rewards and receipts.</summary>
    [Test]
    public async Task SaveFailureRollsBackAsync()
    {
        var (player, db) = await CreateAsync().ConfigureAwait(false); db.FailSave = true;
        var result = await new RedeemCodeActions().RedeemAsync(player, "GIFT-2026").ConfigureAwait(false);
        Assert.That(result, Does.Contain("could not be saved"));
        Assert.That(player.Inventory!.Items, Is.Empty);
        Assert.That(player.Account!.WCoinC, Is.Zero);
        Assert.That(player.Account.GoblinPoints, Is.Zero);
        Assert.That(db.Code.UsedCount, Is.Zero);
        Assert.That(db.Claims, Is.Empty);
        db.FailSave = false;
        Assert.That(await new RedeemCodeActions().RedeemAsync(player, "GIFT-2026").ConfigureAwait(false), Does.Contain("successfully"));
    }

    /// <summary>A full inventory leaves the code available and does not credit coins.</summary>
    [Test]
    public async Task FullInventoryAndOverflowAsync()
    {
        var (player, db) = await CreateAsync().ConfigureAwait(false);
        var definition = player.GameContext.Configuration.Items.First();
        while (player.Inventory!.FreeSlots.Any())
        {
            var item = player.PersistenceContext.CreateNew<Item>(); item.Definition = definition; item.Durability = 1;
            Assert.That(await player.Inventory.AddItemAsync(item).ConfigureAwait(false), Is.True);
        }
        var before = player.Inventory.Items.Count();
        Assert.That(await new RedeemCodeActions().RedeemAsync(player, "GIFT-2026").ConfigureAwait(false), Does.Contain("inventory space"));
        Assert.That(player.Inventory.Items.Count(), Is.EqualTo(before));
        Assert.That(player.Account!.WCoinC, Is.Zero);
        Assert.That(db.Code.UsedCount, Is.Zero);
        Assert.That(db.Claims, Is.Empty);
        player.Account.WCoinC = int.MaxValue;
        Assert.That(await new RedeemCodeActions().RedeemAsync(player, "GIFT-2026").ConfigureAwait(false), Does.Contain("exceed"));
        Assert.That(player.Account.WCoinC, Is.EqualTo(int.MaxValue));
    }

    /// <summary>Invalid codes are rejected, and repeated guesses are throttled.</summary>
    [Test]
    public async Task ValidationAndThrottleAsync()
    {
        var (player, db) = await CreateAsync().ConfigureAwait(false);
        var actions = new RedeemCodeActions();
        Assert.That(await actions.RedeemAsync(player, "GIFT!2026").ConfigureAwait(false), Does.Contain("Invalid"));
        Assert.That(await actions.RedeemAsync(player, "GIFT-2026").ConfigureAwait(false), Does.Contain("Wait"));
        Assert.That(db.Claims, Is.Empty);
        Assert.That(RedeemCodeRules.Normalize(new string('A', 33)), Is.Empty);
        db.Code.ItemsJson = "[]"; db.Code.WCoinC = -1;
        Assert.Throws<ArgumentException>(() => RedeemCodeRules.Rewards(db.Code));
    }

    private static async Task<(Player Player, DatabaseContext Db)> CreateAsync()
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        Mock.Get(player.GameContext.Configuration).Setup(c => c.Items).Returns(new List<ItemDefinition> { new Basic.ItemDefinition { Group = 14, Number = 13, Width = 1, Height = 1, Durability = 1, Name = "Jewel of Bless" } });
        await player.PlayerState.TryAdvanceToAsync(PlayerState.EnteredWorld).ConfigureAwait(false);
        var proxy = DispatchProxy.Create<IPlayerContext, DatabaseContext>();
        var db = (DatabaseContext)(object)proxy; db.Inner = player.PersistenceContext;
        db.Code = new Basic.RedeemCode { Code = "GIFT-2026", Id = RedeemCodeRules.Key("GIFT-2026"), Enabled = true, WCoinC = 50, GoblinPoints = 10,
            ItemsJson = JsonSerializer.Serialize(new[] { new RedeemItemReward { ItemCode = 7181, Count = 2 } }) };
        typeof(Player).GetField("<PersistenceContext>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(player, proxy);
        return (player, db);
    }

    /// <summary>Models a persistent database across detached player contexts, with an injectable failed commit.</summary>
    public class DatabaseContext : DispatchProxy
    {
        public IPlayerContext Inner = null!;
        public RedeemCode Code = null!;
        public Dictionary<Guid, RedeemCodeClaim> Claims = new();
        public bool FailSave;
        private RedeemCodeClaim? _pending;
        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            var type = method!.IsGenericMethod ? method.GetGenericArguments()[0] : null;
            if (method.Name == nameof(IContext.GetByIdAsync) && type == typeof(RedeemCode)) return Task.FromResult((Guid)args![0]! == this.Code.Id ? this.Code : null);
            if (method.Name == nameof(IContext.GetByIdAsync) && type == typeof(RedeemCodeClaim)) return Task.FromResult(this.Claims.GetValueOrDefault((Guid)args![0]!));
            if (method.Name == nameof(IContext.CreateNew) && type == typeof(RedeemCodeClaim)) return this._pending = new Basic.RedeemCodeClaim();
            if (method.Name == nameof(IContext.Detach) && args![0] is RedeemCode or RedeemCodeClaim) { this._pending = null; return false; }
            if (method.Name == nameof(IContext.SaveChangesAsync))
            {
                if (this.FailSave) throw new InvalidOperationException("Injected commit failure");
                if (this._pending != null) this.Claims.Add(this._pending.Id, this._pending);
                return ValueTask.FromResult(true);
            }
            return method.Invoke(this.Inner, args);
        }
    }
}
