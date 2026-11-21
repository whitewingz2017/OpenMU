// <copyright file="EventRewardBoxNamesTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.Tests.Pages;

using System.Globalization;
using System.Threading;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.Persistence;
using MUnique.OpenMU.Persistence.InMemory;
using MUnique.OpenMU.Web.AdminPanel.Pages;
using MUnique.OpenMU.Web.Shared.Components.Form;
using MUnique.OpenMU.Web.Shared.Components.Modal;
using MUnique.OpenMU.Web.Shared.Components.Toast;
using MUnique.OpenMU.Web.Shared.Services;

/// <summary>Checks that level-based event boxes are discoverable by their names.</summary>
[TestFixture]
[NonParallelizable]
public class EventRewardBoxNamesTests
{
    /// <summary>All five names are shown, search finds a variant, and adding rewards keeps its source level.</summary>
    [Test]
    public void PageNamesAndFiltersEventBoxes()
    {
        var previous = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en");
        try
        {
            using var context = new BunitContext();
            using var persistence = new InMemoryPersistenceContextProvider().CreateNewContext();
            var configuration = persistence.CreateNew<GameConfiguration>();
            var box = persistence.CreateNew<ItemDefinition>();
            box.Group = 14;
            box.Number = 11;
            box.Name = "Box of Luck";
            configuration.Items.Add(box);
            foreach (var level in new byte[] { 2, 3, 5, 6, 7 })
            {
                var group = persistence.CreateNew<ItemDropItemGroup>();
                group.SourceItemLevel = level;
                box.DropItems.Add(group);
            }

            var source = new Mock<IDataSource<GameConfiguration>>();
            source.Setup(service => service.GetContextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(persistence);
            source.Setup(service => service.GetOwnerAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(configuration);
            context.Services.AddSingleton(source.Object);
            context.Services.AddSingleton(Mock.Of<IToastService>());
            context.Services.AddSingleton(Mock.Of<IModalService>());
            context.Services.AddSingleton(Mock.Of<ILogger<EditItemDrops>>());
            context.Services.AddSingleton(new LoadingOverlayService());
            context.ComponentFactories.AddStub<MUnique.OpenMU.Web.Shared.Components.Breadcrumb>();
            context.ComponentFactories.AddStub<LookupField<ItemDefinition>>();
            context.ComponentFactories.AddStub<LookupField<MonsterDefinition>>();
            var page = context.Render<EditItemDrops>();
            page.WaitForState(() => page.FindAll(".collapsible-section__header").Count > 0, TimeSpan.FromSeconds(10));
            page.FindAll(".collapsible-section__header").Single(header => header.TextContent.Contains("Item box drops", StringComparison.OrdinalIgnoreCase)).Click();
            foreach (var name in new[] { "Firecracker", "Heart of Love", "Silver Medal", "Gold Medal", "Box of Heaven" })
            {
                Assert.That(page.FindAll("h6").Any(header => header.TextContent.Contains(name)), Is.True, name);
            }

            page.Find(".item-drops-toolbar input").Input("Heart of Love");
            Assert.That(page.FindAll("h6"), Has.Count.EqualTo(1));
            page.Find("h6").ParentElement!.QuerySelector("button.btn-outline-primary")!.Click();
            Assert.That(box.DropItems.Count(group => group.SourceItemLevel == 3), Is.EqualTo(2));
            Assert.That(box.DropItems.Count(group => group.SourceItemLevel == 2), Is.EqualTo(1));
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }
}
