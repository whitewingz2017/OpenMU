// <copyright file="PvpCounterattackPaymentTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using System.Buffers.Binary;
using MUnique.OpenMU.GameLogic.MuHelper;

[TestFixture]
public class PvpCounterattackPaymentTests
{
    [Test]
    public async Task PurchaseSaveAndToggleReusePaidHour()
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        player.Account!.WCoinC = 90;
        var request = new byte[257]; request[29] = 17;
        await PvpCounterattackPayment.SaveAsync(player, request).ConfigureAwait(false);
        Assert.That(player.Account.WCoinC, Is.EqualTo(60));
        Assert.That(PvpCounterattackPayment.IsPaid(player.SelectedCharacter!.MuHelperConfiguration!), Is.True);
        request[29] = 0;
        await PvpCounterattackPayment.SaveAsync(player, request).ConfigureAwait(false);
        request[29] = 17;
        await PvpCounterattackPayment.SaveAsync(player, request).ConfigureAwait(false);
        Assert.That(player.Account.WCoinC, Is.EqualTo(60));
        Assert.That(PvpCounterattackPayment.IsPaid(player.SelectedCharacter.MuHelperConfiguration!), Is.True);
    }

    [Test]
    public async Task ForgedLeaseAndMissingConfirmationCannotEnableForFree()
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        player.Account!.WCoinC = 90;
        var request = new byte[257]; request[29] = 1;
        BinaryPrimitives.WriteUInt32LittleEndian(request.AsSpan(30), 0x43505650);
        BinaryPrimitives.WriteInt64LittleEndian(request.AsSpan(34), DateTimeOffset.UtcNow.AddDays(1).ToUnixTimeSeconds());
        await PvpCounterattackPayment.SaveAsync(player, request).ConfigureAwait(false);
        Assert.That(player.Account.WCoinC, Is.EqualTo(90));
        Assert.That(PvpCounterattackPayment.IsPaid(player.SelectedCharacter!.MuHelperConfiguration!), Is.False);
    }

    [TestCase(0, false)]
    [TestCase(30, true)]
    public async Task ExpiredHourRenewsOrDisables(int balance, bool enabled)
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        player.Account!.WCoinC = 30;
        var request = new byte[257]; request[29] = 17;
        await PvpCounterattackPayment.SaveAsync(player, request).ConfigureAwait(false);
        BinaryPrimitives.WriteInt64LittleEndian(player.SelectedCharacter!.MuHelperConfiguration!.AsSpan(34), DateTimeOffset.UtcNow.AddSeconds(-1).ToUnixTimeSeconds());
        player.Account.WCoinC = balance;
        await PvpCounterattackPayment.RenewAsync(player).ConfigureAwait(false);
        await PvpCounterattackPayment.RenewAsync(player).ConfigureAwait(false);
        Assert.That(PvpCounterattackPayment.IsPaid(player.SelectedCharacter.MuHelperConfiguration), Is.EqualTo(enabled));
        Assert.That(player.Account.WCoinC, Is.Zero);
        Assert.That(player.SelectedCharacter.MuHelperConfiguration![29] & 1, Is.EqualTo(enabled ? 1 : 0));
    }

    [Test]
    public async Task FailedSaveRestoresBalanceAndLease()
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        player.Account!.WCoinC = 30;
        var original = new byte[257]; player.SelectedCharacter!.MuHelperConfiguration = original;
        CashShopActionsTest.SaveFailingContext.Install(player, () => true);
        var request = new byte[257]; request[29] = 17;
        await PvpCounterattackPayment.SaveAsync(player, request).ConfigureAwait(false);
        Assert.That(player.Account.WCoinC, Is.EqualTo(30));
        Assert.That(player.SelectedCharacter.MuHelperConfiguration, Is.SameAs(original));
    }
    [Test]
    public async Task RenewalCannotReviveDisabledSettingsOrChargeLegacySettings()
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        player.Account!.WCoinC = 90;
        var legacy = new byte[257]; legacy[29] = 1;
        player.SelectedCharacter!.MuHelperConfiguration = legacy;
        await PvpCounterattackPayment.RenewAsync(player).ConfigureAwait(false);
        Assert.That(player.Account.WCoinC, Is.EqualTo(90));
        Assert.That(player.SelectedCharacter.MuHelperConfiguration![29] & 1, Is.Zero);
        var stale = new byte[257]; stale[29] = 1;
        await PvpCounterattackPayment.SaveAsync(player, stale, renewal: true).ConfigureAwait(false);
        Assert.That(player.SelectedCharacter.MuHelperConfiguration![29] & 1, Is.Zero);
        Assert.That(player.Account.WCoinC, Is.EqualTo(90));
    }

}
