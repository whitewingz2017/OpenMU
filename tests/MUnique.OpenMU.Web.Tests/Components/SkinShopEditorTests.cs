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
        var rentalInputs = form.FindAll("#skin-rental-prices input");
        Assert.That(rentalInputs, Has.Count.EqualTo(3));
        rentalInputs[0].Change("10");
        form.FindAll("#skin-rental-prices input")[1].Change("25");
        form.FindAll("#skin-rental-prices input")[2].Change("50");
        Assert.That(settings.RentalOneDayPrice, Is.EqualTo(10));
        Assert.That(settings.RentalThreeDaysPrice, Is.EqualTo(25));
        Assert.That(settings.RentalSevenDaysPrice, Is.EqualTo(50));
        Assert.That(SkinShopEditor.IsValid(settings), Is.True);
        settings.RentalOneDayPrice = -1;
        Assert.That(SkinShopEditor.IsValid(settings), Is.False);
        settings.RentalOneDayPrice = 10;
        Assert.That(form.FindAll("tbody tr"), Has.Count.EqualTo(2));
        Assert.That(form.FindAll("tbody tr input[id^=skin-sale-]").Select(input => input.Id), Is.EqualTo(new[] { "skin-sale-4414", "skin-sale-4416" }));
        form.Find("#skin-excellent-effect-4414").Change(true);
        Assert.That(settings.Items[4414].ExcellentEffect, Is.True);
        Assert.That(settings.Items[4416].ExcellentEffect, Is.False);
        form.Find("#skin-aura-4414").Change("Gold");
        Assert.That(settings.Items[4414].AuraColor, Is.EqualTo(SkinAuraColor.Gold));
        Assert.That(settings.Items[4416].AuraColor, Is.EqualTo(SkinAuraColor.Default));
        Assert.That(settings.AuraColors(definitions).Single(), Is.EqualTo(new[] { 4414, (int)SkinAuraColor.Gold }));
        form.Find("#skin-rental-1-4414").Change("38");
        form.Find("#skin-rental-3-4414").Change("0");
        form.Find("#skin-rental-7-4414").Change("112");
        Assert.That(settings.Items[4414].RentalOneDayPrice, Is.EqualTo(38));
        Assert.That(settings.Items[4414].RentalThreeDaysPrice, Is.Zero);
        Assert.That(settings.Items[4414].RentalSevenDaysPrice, Is.EqualTo(112));
        Assert.That(settings.Items[4416].RentalOneDayPrice, Is.Null);
        form.Find("#skin-rental-1-4414").Change("");
        Assert.That(settings.Items[4414].RentalOneDayPrice, Is.Null);
        form.Find("#skin-price-4416").Change("250");
        form.Find("#skin-sale-4414").Change(false);
        Assert.That(form.FindAll("tbody tr input[id^=skin-sale-]").Select(input => input.Id), Is.EqualTo(new[] { "skin-sale-4416", "skin-sale-4414" }));
        Assert.That(settings.Items[4414].ExcellentEffect, Is.True);
        Assert.That(settings.Items[4416].Price, Is.EqualTo(250));
        Assert.That(settings.Items[4414].Enabled, Is.False);
        Assert.That(settings.Items[4414].Price, Is.EqualTo(125));
        form.Find("label.form-check input").Change(false);
        Assert.That(settings.DisabledCategories, Does.Contain(SkinShopCategory.Sets));
        Assert.That(settings.Price(definitions[0]), Is.Zero);
        form.Find("label.form-check input").Change(true);
        Assert.That(settings.Price(definitions[0]), Is.EqualTo(250));
        Assert.That(settings.Price(definitions[1]), Is.Zero);
        form.Find("#skin-sale-4414").Change(true);
        Assert.That(form.FindAll("tbody tr input[id^=skin-sale-]").Select(input => input.Id), Is.EqualTo(new[] { "skin-sale-4414", "skin-sale-4416" }));
        form.Find("#skin-sale-4414").Change(false);
        Assert.That(SkinShopEditor.IsValid(settings), Is.True);
        settings.Items[4416].Price = -1;
        Assert.That(SkinShopEditor.IsValid(settings), Is.False);
        SkinShopEditor.Normalize(settings, definitions);
        Assert.That(settings.Items[4414].Enabled, Is.False);
        Assert.That(settings.Items[4416].Price, Is.EqualTo(-1));
    }
}
