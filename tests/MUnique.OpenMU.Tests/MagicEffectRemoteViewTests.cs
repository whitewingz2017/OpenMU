// <copyright file="MagicEffectRemoteViewTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using System.Threading;
using Moq;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.Views;
using MUnique.OpenMU.GameLogic.Views.World;
using MUnique.OpenMU.PlugIns;
using MUnique.OpenMU.GameServer.RemoteView.World;
using MUnique.OpenMU.Network.Packets.ServerToClient;
using MagicEffectDefinition = MUnique.OpenMU.Persistence.BasicModel.MagicEffectDefinition;
using PowerUpDefinition = MUnique.OpenMU.Persistence.BasicModel.PowerUpDefinition;

/// <summary>
/// Verifies that all timed effects publish their real remaining duration to the owner.
/// </summary>
[TestFixture]
public class MagicEffectRemoteViewTests
{
    /// <summary>
    /// Experience and drop-rate seals must use timed effect packets instead of untimed status packets.
    /// </summary>
    /// <param name="master">Whether the seal increases master experience.</param>
    /// <param name="sendDuration">The legacy duration flag, which must not suppress owner countdowns.</param>
    [TestCase(false, true)]
    [TestCase(true, true)]
    [TestCase(false, false)]
    public async Task WealthSealPublishesDurationAsync(bool master, bool sendDuration)
    {
        var (player, output) = CastleSiegeRemoteViewTestHelper.CreatePlayer();
        var definition = new MagicEffectDefinition { Number = (short)(master ? 102 : 30), SendDuration = sendDuration };
        definition.PowerUpDefinitions.Add(new PowerUpDefinition { TargetAttribute = master ? Stats.MasterExperienceRate : Stats.ExperienceRate });
        definition.PowerUpDefinitions.Add(new PowerUpDefinition { TargetAttribute = Stats.ItemDropRate });
        await using var effect = new MagicEffect(TimeSpan.FromSeconds(123456), definition);
        var view = new DeActivateMagicEffectPlugIn(player);

        await view.ActivateMagicEffectAsync(effect, player).ConfigureAwait(false);

        var data = output.ToArray();
        Assert.That(data, Has.Length.EqualTo(EffectItemConsumption.Length * 2));
        for (int index = 0; index < 2; index++)
        {
            var packet = new EffectItemConsumption(data.AsMemory(index * EffectItemConsumption.Length, EffectItemConsumption.Length));
            Assert.That(packet.Header.Code, Is.EqualTo(0x2D));
            Assert.That(packet.MagicEffectNumber, Is.EqualTo(definition.Number));
            Assert.That(packet.Action, Is.EqualTo(EffectItemConsumption.EffectAction.Add));
            Assert.That(packet.Type, Is.EqualTo(index == 0 ? EffectItemConsumption.EffectType.ExperienceRate : EffectItemConsumption.EffectType.DropRate));
            Assert.That(packet.RemainingSeconds, Is.EqualTo(123456u));
        }
    }

    /// <summary>
    /// Effects which use the bonus experience stat also retain the duration supplied at activation.
    /// </summary>
    [Test]
    public async Task BonusExperiencePublishesDurationAsync()
    {
        var (player, output) = CastleSiegeRemoteViewTestHelper.CreatePlayer();
        var definition = new MagicEffectDefinition { Number = 112, SendDuration = true };
        definition.PowerUpDefinitions.Add(new PowerUpDefinition { TargetAttribute = Stats.BonusExperienceRate });
        await using var effect = new MagicEffect(TimeSpan.FromMinutes(7), definition);

        await new DeActivateMagicEffectPlugIn(player).ActivateMagicEffectAsync(effect, player).ConfigureAwait(false);

        var data = output.ToArray();
        Assert.That(data, Has.Length.EqualTo(EffectItemConsumption.Length));
        var packet = new EffectItemConsumption(data);
        Assert.That(packet.RemainingSeconds, Is.EqualTo(420));
        Assert.That(packet.MagicEffectNumber, Is.EqualTo(112));
    }
    /// <summary>
    /// Every visible buff identifier carries its runtime lifetime without requiring a mapped bonus.
    /// </summary>
    /// <param name="effectNumber">The visible buff identifier.</param>
    [Test]
    public async Task EveryVisibleTimedBuffPublishesDurationAsync([Range(1, 199)] int effectNumber)
    {
        var (player, output) = CastleSiegeRemoteViewTestHelper.CreatePlayer();
        var definition = new MagicEffectDefinition { Number = (short)effectNumber, SendDuration = false };
        await using var effect = new MagicEffect(TimeSpan.FromSeconds(1800), definition);

        await new DeActivateMagicEffectPlugIn(player).ActivateMagicEffectAsync(effect, player).ConfigureAwait(false);

        var data = output.ToArray();
        Assert.That(data, Has.Length.EqualTo(EffectItemConsumption.Length));
        var packet = new EffectItemConsumption(data);
        Assert.That(packet.MagicEffectNumber, Is.EqualTo(effectNumber));
        Assert.That(packet.RemainingSeconds, Is.EqualTo(1800));
    }

    /// <summary>
    /// Shadow Phantom's unmapped attack/defense bonuses must not lose its server duration.
    /// </summary>
    [Test]
    public async Task ShadowPhantomPublishesDurationAsync()
    {
        var (player, output) = CastleSiegeRemoteViewTestHelper.CreatePlayer();
        var definition = new MagicEffectDefinition { Number = 3, SendDuration = false };
        definition.PowerUpDefinitions.Add(new PowerUpDefinition { TargetAttribute = Stats.GreaterDamageBonus });
        definition.PowerUpDefinitions.Add(new PowerUpDefinition { TargetAttribute = Stats.GreaterDefenseBonus });
        await using var effect = new MagicEffect(TimeSpan.FromMinutes(90), definition);

        await new DeActivateMagicEffectPlugIn(player).ActivateMagicEffectAsync(effect, player).ConfigureAwait(false);

        var packet = new EffectItemConsumption(output.ToArray());
        Assert.That(packet.MagicEffectNumber, Is.EqualTo(3));
        Assert.That(packet.RemainingSeconds, Is.EqualTo(5400));
    }

    /// <summary>
    /// Infinite effects must not wrap a negative duration into a huge countdown.
    /// </summary>
    [Test]
    public async Task InfiniteEffectRemainsUntimedAsync()
    {
        var (player, output) = CastleSiegeRemoteViewTestHelper.CreatePlayer();
        var definition = new MagicEffectDefinition { Number = 28, SendDuration = false };
        await using var effect = new MagicEffect(Timeout.InfiniteTimeSpan, definition);

        await new DeActivateMagicEffectPlugIn(player).ActivateMagicEffectAsync(effect, player).ConfigureAwait(false);

        var data = output.ToArray();
        Assert.That(data[2], Is.EqualTo(0x07));
    }

    /// <summary>
    /// Refreshing unchanged bonuses must also refresh the owner's countdown.
    /// </summary>
    [Test]
    public async Task RefreshPublishesResetLifetimeAsync()
    {
        var owner = new Mock<IAttackable>();
        var observer = owner.As<IWorldObserver>();
        var container = new Mock<ICustomPlugInContainer<IViewPlugIn>>();
        var view = new Mock<IActivateMagicEffectPlugIn>();
        var durations = new List<TimeSpan>();
        observer.SetupGet(player => player.ViewPlugIns).Returns(container.Object);
        container.Setup(plugins => plugins.GetPlugIn<IActivateMagicEffectPlugIn>()).Returns(view.Object);
        view.Setup(plugin => plugin.ActivateMagicEffectAsync(It.IsAny<MagicEffect>(), owner.Object))
            .Callback<MagicEffect, IAttackable>((effect, _) => durations.Add(effect.Duration))
            .Returns(ValueTask.CompletedTask);
        var definition = new MagicEffectDefinition { Number = 3 };
        await using var list = new MagicEffectsList(owner.Object);

        await list.AddEffectAsync(new MagicEffect(TimeSpan.FromMinutes(5), definition)).ConfigureAwait(false);
        await list.AddEffectAsync(new MagicEffect(TimeSpan.FromMinutes(90), definition)).ConfigureAwait(false);

        Assert.That(durations, Is.EqualTo(new[] { TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(90) }));
        Assert.That(list.GetActiveEffectsSnapshot(), Has.Count.EqualTo(1));
    }

}
