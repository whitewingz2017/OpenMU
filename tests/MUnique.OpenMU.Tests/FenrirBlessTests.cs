// <copyright file="FenrirBlessTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.PlayerActions.ItemConsumeActions;
using MUnique.OpenMU.Persistence;
using BasicModel = MUnique.OpenMU.Persistence.BasicModel;

/// <summary>Custom mounts inherit the configured ordinary Fenrir Bless repair rule.</summary>
[TestFixture]
public class FenrirBlessTests
{
    [TestCase(true, 200, true)]
    [TestCase(false, 200, false)]
    [TestCase(true, 255, false)]
    public async Task RepairRespectsFenrirConfigurationAsync(bool enabled, double durability, bool expected)
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        var slot = new BasicModel.ItemSlotType();
        slot.ItemSlots.Add(8);
        var skill = new BasicModel.Skill { DamageType = DamageType.Fenrir };
        var original = new BasicModel.ItemDefinition { Group = 13, Number = 37, Skill = skill, ItemSlot = slot };
        var custom = new BasicModel.ItemDefinition { Group = 13, Number = 300, Skill = skill, ItemSlot = slot, Durability = 255 };
        var handler = new TestHandler { Configuration = new BlessJewelConsumeHandlerPlugInConfiguration() };
        if (enabled)
        {
            handler.Configuration.RepairTargetItems.Add(original);
        }

        var item = new TemporaryItem { Definition = custom, Durability = durability };
        Assert.That(handler.Apply(item, player.PersistenceContext), Is.EqualTo(expected));
        Assert.That(item.Durability, Is.EqualTo(expected ? 255 : durability));
        Assert.That(item.Level, Is.Zero);
    }

    private sealed class TestHandler : BlessJewelConsumeHandlerPlugIn
    {
        public bool Apply(Item item, IContext context) => this.ModifyItem(item, context);
    }
}
