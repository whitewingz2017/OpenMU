// <copyright file="PlugInConfigurationLinkTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.Tests;

using System.Threading;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.CashShop;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.Persistence;
using MUnique.OpenMU.Persistence.Initialization.PlugIns.CharacterCreated;
using MUnique.OpenMU.Web.Shared.Components.Modal;
using MUnique.OpenMU.Web.Shared.Models;
using MUnique.OpenMU.Web.AdminPanel.Pages;
using MUnique.OpenMU.Web.Shared.Services;
using Basic = MUnique.OpenMU.Persistence.BasicModel;

/// <summary>Checks the type identifiers used by direct configuration menu links.</summary>
[TestFixture]
public class PlugInConfigurationLinkTests
{
    /// <summary>Menu links resolve by type id even though the stored configuration has a different id.</summary>
    /// <param name="type">The plugin linked from the configuration menu.</param>
    [TestCase(typeof(StartingItemsPlugIn))]
    [TestCase(typeof(ZenDropPlugIn))]
    [TestCase(typeof(CashShopFeaturePlugIn))]
    public async Task ResolvesTypeAndSavedIdsAsync(Type type)
    {
        var configuration = new Basic.GameConfiguration();
        var plugin = new Basic.PlugInConfiguration { Id = Guid.NewGuid(), TypeId = type.GUID };
        configuration.PlugInConfigurations.Add(plugin);
        var source = new Mock<IDataSource<GameConfiguration>>();
        source.Setup(s => s.GetOwnerAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(configuration);
        var controller = new PlugInController(source.Object, Mock.Of<IModalService>(), Mock.Of<IPersistenceContextProvider>());

        var linked = await controller.GetByIdAsync(type.GUID).ConfigureAwait(false);
        Assert.That(linked, Is.Not.Null);
        Assert.That(linked!.Id, Is.EqualTo(plugin.Id));
        Assert.That(linked.ConfigurationType, Is.Not.Null);
        Assert.That((await controller.GetByIdAsync(plugin.Id).ConfigureAwait(false))!.TypeId, Is.EqualTo(type.GUID));
        Assert.That(await controller.GetByIdAsync(Guid.NewGuid()).ConfigureAwait(false), Is.Null);
    }

    /// <summary>The menu query opens its modal after rendering, without a delay or a gear click.</summary>
    /// <param name="type">The plugin linked from the configuration menu.</param>
    [TestCase(typeof(StartingItemsPlugIn))]
    [TestCase(typeof(ZenDropPlugIn))]
    [TestCase(typeof(CashShopFeaturePlugIn))]
    public void MenuQueryOpensConfiguration(Type type)
    {
        using var context = new BunitContext();
        var configuration = new Basic.GameConfiguration();
        configuration.PlugInConfigurations.Add(new Basic.PlugInConfiguration { Id = Guid.NewGuid(), TypeId = type.GUID });
        var source = new Mock<IDataSource<GameConfiguration>>();
        source.Setup(s => s.GetOwnerAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(configuration);
        source.Setup(s => s.GetContextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Mock.Of<IContext>());
        var provider = new Mock<IPersistenceContextProvider>();
        provider.Setup(p => p.CreateNewTypedContext(It.IsAny<Type>(), false, null)).Returns(Mock.Of<IContext>());
        var modal = new Mock<IModalService>();
        var reference = new Mock<IModalReference>();
        reference.SetupGet(r => r.Result).Returns(Task.FromResult(ModalResult.Cancel()));
        modal.Setup(m => m.Show(It.IsAny<Type>(), It.IsAny<string>(), It.IsAny<ModalParameters>(), It.IsAny<ModalOptions>())).Returns(reference.Object);
        var controller = new PlugInController(source.Object, modal.Object, provider.Object);
        context.Services.AddSingleton(controller);
        context.Services.AddSingleton<IDataService<PlugInConfigurationViewItem>>(controller);
        context.Services.AddSingleton<NavigationHistory>();
        context.Services.GetRequiredService<NavigationManager>().NavigateTo($"/plugins?id={type.GUID}");

        var page = context.Render<Plugins>();
        page.WaitForAssertion(() => modal.Verify(m => m.Show(It.IsAny<Type>(), It.IsAny<string>(), It.IsAny<ModalParameters>(), It.IsAny<ModalOptions>()), Times.Once));
        Assert.That(controller.NameFilter, Is.Not.Empty);
        var expectedEditor = type == typeof(StartingItemsPlugIn)
            ? typeof(MUnique.OpenMU.Web.AdminPanel.Components.StartingItems.StartingItemsEditor)
            : typeof(MUnique.OpenMU.Web.Shared.Components.Form.Modal.ModalCreateNew<>).MakeGenericType(type == typeof(ZenDropPlugIn) ? typeof(ZenDropConfiguration) : typeof(CashShopSettings));
        modal.Verify(m => m.Show(expectedEditor, It.IsAny<string>(), It.IsAny<ModalParameters>(), It.IsAny<ModalOptions>()), Times.Once);
    }
}
