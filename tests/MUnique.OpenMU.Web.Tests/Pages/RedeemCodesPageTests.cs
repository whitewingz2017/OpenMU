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
}
