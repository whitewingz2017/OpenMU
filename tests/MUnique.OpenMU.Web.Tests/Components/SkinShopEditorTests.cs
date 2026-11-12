// <copyright file="SkinShopEditorTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.Tests.Components;

using Bunit;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MUnique.OpenMU.GameLogic.Cosmetics;
using MUnique.OpenMU.Web.AdminPanel.Components.SkinShop;
using MUnique.OpenMU.Web.Shared.Services;
using BasicModel = MUnique.OpenMU.Persistence.BasicModel;

/// <summary>Checks category switches and editable prices in the skin shop admin editor.</summary>
[TestFixture]
public class SkinShopEditorTests
{
    /// <summary>Editing one item or category keeps the other items and their prices intact.</summary>
    [Test]
    public void SaleRowsEditIndependently()
    {
        using var context = new BunitContext();
        context.Services.AddSingleton(Mock.Of<IChangeNotificationService>());
        var slot = new BasicModel.ItemSlotType();
        slot.ItemSlots.Add(241);
        var definitions = new[]
        {
            new BasicModel.ItemDefinition { Group = 8, Number = 320, Name = "Immortal WareWolf Armor", ItemSlot = slot },
            new BasicModel.ItemDefinition { Group = 8, Number = 318, Name = "Epic Final Armor", ItemSlot = slot },
        };
        var settings = SkinShopEditor.Normalize(new SkinShopSettings { ArmorPrice = 125 }, definitions);
        var form = context.Render<EditForm>(parameters => parameters
            .Add(p => p.Model, settings)
            .Add(p => p.ChildContent, _ => builder =>
            {
                builder.OpenComponent<SkinShopEditor>(0);
                builder.AddAttribute(1, nameof(SkinShopEditor.Item), settings);
                builder.AddAttribute(2, nameof(SkinShopEditor.Definitions), definitions);
                builder.CloseComponent();
            }));
        Assert.That(form.FindAll("tbody tr"), Has.Count.EqualTo(2));
        form.Find("#skin-price-4416").Change("250");
        form.Find("#skin-sale-4414").Change(false);
        Assert.That(settings.Items[4416].Price, Is.EqualTo(250));
        Assert.That(settings.Items[4414].Enabled, Is.False);
        Assert.That(settings.Items[4414].Price, Is.EqualTo(125));
        form.Find("label.form-check input").Change(false);
        Assert.That(settings.DisabledCategories, Does.Contain(SkinShopCategory.Sets));
        Assert.That(settings.Price(definitions[0]), Is.Zero);
        form.Find("label.form-check input").Change(true);
        Assert.That(settings.Price(definitions[0]), Is.EqualTo(250));
        Assert.That(settings.Price(definitions[1]), Is.Zero);
        Assert.That(SkinShopEditor.IsValid(settings), Is.True);
        settings.Items[4416].Price = -1;
        Assert.That(SkinShopEditor.IsValid(settings), Is.False);
        SkinShopEditor.Normalize(settings, definitions);
        Assert.That(settings.Items[4414].Enabled, Is.False);
        Assert.That(settings.Items[4416].Price, Is.EqualTo(-1));
    }
}
