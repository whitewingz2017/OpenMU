// <copyright file="RedeemCodesPageTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.Tests.Pages;

using System.Threading;
using Bunit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.Persistence;
using MUnique.OpenMU.Persistence.InMemory;
using MUnique.OpenMU.Web.AdminPanel.Pages;

/// <summary>Checks the actual administrator gift-code form and authorization requirement.</summary>
[TestFixture]
public class RedeemCodesPageTests
{
    /// <summary>An operator can configure a coin-only code, and empty rewards are rejected.</summary>
    [Test]
    public async Task CreateCodeAndRejectEmptyRewardAsync()
    {
        using var context = new BunitContext();
        var persistence = new InMemoryPersistenceContextProvider();
        var source = new Mock<IDataSource<GameConfiguration>>();
        source.Setup(s => s.GetOwnerAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(new GameConfiguration());
        context.Services.AddSingleton<IPersistenceContextProvider>(persistence);
        context.Services.AddSingleton(source.Object);
        context.Services.AddSingleton(Mock.Of<ILogger<RedeemCodes>>());
        context.ComponentFactories.AddStub<MUnique.OpenMU.Web.Shared.Components.Breadcrumb>();
        var cut = context.Render<RedeemCodes>();
        cut.WaitForAssertion(() => Assert.That(cut.FindAll("form").Count, Is.EqualTo(1)));
        cut.Find("#redeem-code").Change("WELCOME-2026");
        cut.Find("form").Submit();
        cut.WaitForAssertion(() => Assert.That(cut.Markup, Does.Contain("Choose positive rewards")));
        cut.Find("#redeem-wcoinc").Change("150");
        cut.Find("form").Submit();
        cut.WaitForAssertion(() => Assert.That(cut.Markup, Does.Contain("Code saved.")));
        using var db = persistence.CreateNewContext();
        var gift = (await db.GetAsync<RedeemCode>().ConfigureAwait(false)).Single();
        Assert.That(gift.Code, Is.EqualTo("WELCOME-2026"));
        Assert.That(gift.WCoinC, Is.EqualTo(150));
        Assert.That(gift.Enabled, Is.False);
        Assert.That(gift.Id, Is.EqualTo(RedeemCodeRules.Key(gift.Code)));
        Assert.That(typeof(RedeemCodes).GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>().Any(a => a.Policy == "OpenMU.Administrator"), Is.True);
    }
    /// <summary>Adding and editing an item reward renders without breaking the Blazor circuit.</summary>
    [Test]
    public async Task AddItemAndSaveRewardAsync()
    {
        using var context = new BunitContext();
        var persistence = new InMemoryPersistenceContextProvider();
        var config = new Mock<GameConfiguration>();
        config.Setup(c => c.Items).Returns(new List<MUnique.OpenMU.DataModel.Configuration.Items.ItemDefinition>
        {
            new()
            {
            Group = 14,
            Number = 13,
            Name = "Jewel of Bless",
            },
        });
        var source = new Mock<IDataSource<GameConfiguration>>();
        source.Setup(s => s.GetOwnerAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(config.Object);
        context.Services.AddSingleton<IPersistenceContextProvider>(persistence);
        context.Services.AddSingleton(source.Object);
        context.Services.AddSingleton(Mock.Of<ILogger<RedeemCodes>>());
        context.ComponentFactories.AddStub<MUnique.OpenMU.Web.Shared.Components.Breadcrumb>();
        var cut = context.Render<RedeemCodes>();
        cut.WaitForAssertion(() => Assert.That(cut.FindAll("form").Count, Is.EqualTo(1)));
        cut.FindAll("button").Single(b => b.TextContent == "Add item").Click();
        cut.WaitForAssertion(() => Assert.That(cut.FindAll("select").Count, Is.EqualTo(1)));
        cut.Find("#redeem-code").Change("ITEM-GIFT");
        cut.FindAll("input[type=number]")[4].Change("2");
        cut.FindAll("input[type=number]")[5].Change("3");
        cut.Find("form").Submit();
        cut.WaitForAssertion(() => Assert.That(cut.Markup, Does.Contain("Code saved.")));
        using var db = persistence.CreateNewContext();
        var gift = (await db.GetAsync<RedeemCode>().ConfigureAwait(false)).Single();
        var reward = System.Text.Json.JsonSerializer.Deserialize<List<RedeemItemReward>>(gift.ItemsJson)!.Single();
        Assert.That(reward.ItemCode, Is.EqualTo(7181));
        Assert.That(reward.Level, Is.EqualTo(2));
        Assert.That(reward.Count, Is.EqualTo(3));
    }

}
