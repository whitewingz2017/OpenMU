// <copyright file="ItemBoxRewardEditorTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.Tests.Components;

using Bunit;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.Web.AdminPanel.Components.ItemDrops;
using MUnique.OpenMU.Web.Shared.Components.Form;
using BasicModel = MUnique.OpenMU.Persistence.BasicModel;

/// <summary>Checks that reward controls update only the selected item-box group.</summary>
[TestFixture]
public class ItemBoxRewardEditorTests
{
    /// <summary>Luck, skill, normal options and exact Excellent flags can be changed and reset independently.</summary>
    [Test]
    public void RewardControlsEditOnlyOneGroup()
    {
        using var context = new BunitContext();
        context.ComponentFactories.AddStub<MultiLookupField<ItemDefinition>>();
        var first = new BasicModel.ItemDropItemGroup { Id = Guid.NewGuid() };
        var second = new BasicModel.ItemDropItemGroup { Id = Guid.NewGuid() };
        var form = context.Render<DropItemGroupTable>(parameters => parameters
            .Add(component => component.Groups, new DropItemGroup[] { first, second })
            .Add(component => component.IsItemBoxDrop, true));
        form.Find(".group-row button").Click();
        form.Find("[data-box-option='luck']").Change("true");
        form.Find("[data-box-option='skill']").Change("false");
        form.Find("[data-box-option='normal']").Change("7");
        form.Find("[data-box-option='excellent']").Change("fixed");
        form.Find("[data-excellent-flag='1']").Change(true);
        form.Find("[data-excellent-flag='4']").Change(true);
        Assert.That(first.HasLuck, Is.True);
        Assert.That(first.HasSkill, Is.False);
        Assert.That(first.OptionLevel, Is.EqualTo(7));
        Assert.That(first.ExcellentOptions, Is.EqualTo(5));
        Assert.That(second.HasLuck, Is.Null);
        Assert.That(second.ExcellentOptions, Is.Null);
        form.Find("[data-box-option='luck']").Change("false");
        Assert.That(first.HasLuck, Is.False);
        form.Find("[data-box-option='luck']").Change(string.Empty);
        form.Find("[data-box-option='normal']").Change(string.Empty);
        form.Find("[data-box-option='excellent']").Change("random");
        Assert.That(first.HasLuck, Is.Null);
        Assert.That(first.OptionLevel, Is.Null);
        Assert.That(first.ExcellentOptions, Is.Null);
    }
}
