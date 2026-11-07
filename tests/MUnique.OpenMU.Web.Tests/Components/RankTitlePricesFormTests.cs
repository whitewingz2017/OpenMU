// <copyright file="RankTitlePricesFormTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.Tests.Components;

using System.ComponentModel.DataAnnotations;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MUnique.OpenMU.GameLogic.RankTitles;
using MUnique.OpenMU.Persistence;
using MUnique.OpenMU.Web.Shared.Components.Form;
using MUnique.OpenMU.Web.Shared.Components.Modal;
using MUnique.OpenMU.Web.Shared.Services;

/// <summary>Verifies that title prices can be edited through the plugin's admin form.</summary>
[TestFixture]
public class RankTitlePricesFormTests
{
    /// <summary>All titles have local price fields, without navigating to a persistent entity.</summary>
    [Test]
    public void AllTitlePricesAreEditable()
    {
        using var context = new BunitContext();
        context.Services.AddSingleton(Mock.Of<IModalService>());
        context.Services.AddSingleton(Mock.Of<IChangeNotificationService>());
        var configuration = new RankTitleSettings();
        var form = context.Render<CascadingValue<IContext>>(parameters => parameters
            .Add(p => p.Value, Mock.Of<IContext>())
            .AddChildContent<EditForm>(edit => edit
                .Add(p => p.Model, configuration)
                .Add(p => p.ChildContent, _ => builder =>
                {
                    builder.OpenComponent<AutoFields>(0);
                    builder.CloseComponent();
                })));

        var table = form.FindComponent<ItemTable<RankTitleOffer>>();
        table.Find("h5 button").Click();
        var priceFields = form.FindAll("input[id='Price']").ToArray();
        Assert.That(priceFields, Has.Length.EqualTo(17));
        priceFields[0].Change("275");
        priceFields[^1].Change("0");
        Assert.That(configuration.Titles.Count(title => title.Price == 275), Is.EqualTo(1));
        Assert.That(configuration.Titles.Count(title => title.Price == 0), Is.EqualTo(1));
        Assert.That(configuration.IsValid(), Is.True);
        configuration.Titles.First().Price = -1;
        Assert.That(Validator.TryValidateObject(configuration, new ValidationContext(configuration), [], true), Is.False);
    }
}
