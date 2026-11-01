// <copyright file="StartingItemEditorTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.Tests.Components;

using System.Threading;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.Persistence;
using MUnique.OpenMU.PlugIns;
using MUnique.OpenMU.Persistence.Initialization.PlugIns.CharacterCreated;
using MUnique.OpenMU.Web.AdminPanel.Components.StartingItems;
using MUnique.OpenMU.Web.Shared.Components.Form;
using MUnique.OpenMU.Web.Shared.Components.Modal;
using MUnique.OpenMU.Web.Shared.Services;
using Basic = MUnique.OpenMU.Persistence.BasicModel;

/// <summary>Checks named starter selection without creating persistent inventory objects.</summary>
[TestFixture]
public class StartingItemEditorTests
{
    /// <summary>Existing numeric settings reopen as selected named options and changes update the saved policy.</summary>
    [Test]
    public async Task NamedOptionsUpdateRewardAsync()
    {
        using var context = new BunitContext();
        context.Services.AddSingleton(Mock.Of<IChangeNotificationService>());
        context.Services.AddLocalization();
        context.Services.AddSingleton(Mock.Of<ILookupController>());
        var definition = CreateDefinition();
        var configuration = new Basic.GameConfiguration();
        configuration.Items.Add(definition);
        configuration.Items.Add(new Basic.ItemDefinition { Group = 12, Number = 134, MaximumItemLevel = 15 });
        var source = new Mock<IDataSource<GameConfiguration>>();
        source.Setup(s => s.GetOwnerAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(configuration);
        context.Services.AddSingleton(source.Object);
        var policy = new StartingItemsConfiguration();
        var reward = new StartingItemReward { Group = 8, Number = 1, Level = 9, Luck = true, AdditionalOptionLevel = 4, ExcellentOptions = new List<int> { 2, 3, 4 } };
        policy.Classes.First().Items.Add(reward);
        var persistence = new Mock<IContext>(MockBehavior.Strict);
        var form = context.Render<StartingItemsEditor>(p => p.Add(c => c.Item, policy).Add(c => c.PersistenceContext, persistence.Object));
        var editor = form.FindComponent<StartingItemEditor>();
        Assert.That(editor.FindComponent<LookupField<ItemDefinition>>().Instance.Value, Is.SameAs(definition));
        var excellent = editor.FindComponent<MultiLookupField<IncreasableItemOption>>();
        Assert.That(excellent.Instance.Value!.Select(o => o.Number), Is.EqualTo(new[] { 2, 3, 4 }));
        await form.InvokeAsync(() => excellent.Instance.ValueChanged.InvokeAsync(new List<IncreasableItemOption> { definition.PossibleItemOptions.First().PossibleOptions.Last() }));
        Assert.That(reward.ExcellentOptions, Is.EqualTo(new[] { 4 }));

        var additional = editor.FindComponent<LookupField<IncreasableItemOption>>();
        var choice = definition.PossibleItemOptions.First().PossibleOptions.First(o => o.OptionType == ItemOptionTypes.Option && o.Number == 2);
        await form.InvokeAsync(() => additional.Instance.ValueChanged.InvokeAsync(choice));
        Assert.That(reward.AdditionalOptionNumber, Is.EqualTo(2));
        Assert.That(reward.AdditionalOptionLevel, Is.EqualTo(4));
        Assert.That(StartingItemOptions.CreateItem(reward, definition).ItemOptions.Single(o => o.ItemOption!.OptionType == ItemOptionTypes.Option).ItemOption!.Number, Is.EqualTo(2));

        await form.InvokeAsync(() => editor.FindComponent<LookupField<ItemDefinition>>().Instance.ValueChanged.InvokeAsync(configuration.Items.Last()));
        Assert.That(reward.Group, Is.EqualTo(12));
        Assert.That(reward.Number, Is.EqualTo(134));
        Assert.That(reward.ExcellentOptions, Is.Empty);
        Assert.That(reward.Luck, Is.False);
        Assert.That(reward.AdditionalOptionLevel, Is.Zero);
        Assert.That(editor.FindComponents<MultiLookupField<IncreasableItemOption>>(), Is.Empty);
        persistence.VerifyNoOtherCalls();
    }

    /// <summary>An unselected item cannot be submitted, and invalid rows remain visible for correction.</summary>
    [Test]
    public void MissingDefinitionRejectsSubmit()
    {
        using var context = new BunitContext();
        context.Services.AddSingleton(Mock.Of<IChangeNotificationService>());
        context.Services.AddSingleton(Mock.Of<ILookupController>());
        var source = new Mock<IDataSource<GameConfiguration>>();
        source.Setup(s => s.GetOwnerAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(new Basic.GameConfiguration());
        context.Services.AddSingleton(source.Object);
        var policy = new StartingItemsConfiguration();
        policy.Classes.First().Items.Add(new StartingItemReward { Number = -1 });
        var form = context.Render<StartingItemsEditor>(p => p.Add(c => c.Item, policy));
        form.Find("form").Submit();
        Assert.That(form.FindAll("[role=alert]"), Has.Count.EqualTo(1));
        Assert.That(policy.Classes.First().Items, Has.Count.EqualTo(1));
    }

    /// <summary>Submitting saves the policy and cancellation discards changes without inventory writes.</summary>
    /// <param name="save">Whether to submit or cancel the configuration.</param>
    [TestCase(true)]
    [TestCase(false)]
    public async Task ModalSavesOnlyOnSubmitAsync(bool save)
    {
        using var context = new BunitContext();
        context.Services.AddSingleton(Mock.Of<IChangeNotificationService>());
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.Services.AddLocalization();
        context.Services.AddSingleton(Mock.Of<ILookupController>());
        var configuration = new Basic.GameConfiguration();
        configuration.Items.Add(CreateDefinition());
        var policy = new StartingItemsConfiguration();
        policy.Classes.First().Items.Add(new StartingItemReward { Group = 8, Number = 1 });
        var plugin = new Basic.PlugInConfiguration { Id = Guid.NewGuid(), TypeId = typeof(StartingItemsPlugIn).GUID };
        plugin.SetConfiguration(policy, null);
        configuration.PlugInConfigurations.Add(plugin);
        var source = new Mock<IDataSource<GameConfiguration>>();
        source.Setup(s => s.GetOwnerAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(configuration);
        var persistence = new Mock<IContext>();
        persistence.Setup(p => p.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        source.Setup(s => s.GetContextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(persistence.Object);
        context.Services.AddSingleton(source.Object);
        var provider = new Mock<IPersistenceContextProvider>();
        provider.Setup(p => p.CreateNewTypedContext(It.IsAny<Type>(), false, null)).Returns(Mock.Of<IContext>());
        using var service = new ModalService();
        context.Services.AddSingleton(service);
        var controller = new PlugInController(source.Object, service, provider.Object);
        var container = context.Render<ModalContainer>();
        var view = (await controller.GetByIdAsync(plugin.Id).ConfigureAwait(false))!;
        Task? pending = null;
        await container.InvokeAsync(() => { pending = controller.ShowPlugInConfigAsync(view, typeof(StartingItemsEditor)); });
        container.WaitForAssertion(() => Assert.That(container.FindComponents<StartingItemEditor>(), Has.Count.EqualTo(1)));
        var editor = container.FindComponent<StartingItemEditor>();
        await container.InvokeAsync(() => editor.FindComponent<ByteField>().Instance.ValueChanged.InvokeAsync((byte)9));
        if (save)
        {
            container.Find("form").Submit();
        }
        else
        {
            container.Find(".form-actions button[type=button]").Click();
        }

        await pending!.ConfigureAwait(false);
        var reopened = plugin.GetConfiguration<StartingItemsConfiguration>(new ByDataSourceReferenceHandler(source.Object))!;
        Assert.That(reopened.Classes.First().Items.First().Level, Is.EqualTo(save ? 9 : 0));
        persistence.Verify(p => p.SaveChangesAsync(It.IsAny<CancellationToken>()), save ? Times.Once() : Times.Never());
    }

    private static Basic.ItemDefinition CreateDefinition()
    {
        var item = new Basic.ItemDefinition { Group = 8, Number = 1, MaximumItemLevel = 15 };
        var options = new Basic.ItemOptionDefinition();
        options.PossibleOptions.Add(new Basic.IncreasableItemOption { OptionType = ItemOptionTypes.Luck });
        foreach (var number in new[] { 1, 2 })
        {
            var option = new Basic.IncreasableItemOption { Number = number, OptionType = ItemOptionTypes.Option };
            option.LevelDependentOptions.Add(new Basic.ItemOptionOfLevel { Level = 4 });
            options.PossibleOptions.Add(option);
        }

        foreach (var number in new[] { 2, 3, 4 })
        {
            options.PossibleOptions.Add(new Basic.IncreasableItemOption { Number = number, OptionType = ItemOptionTypes.Excellent });
        }

        item.PossibleItemOptions.Add(options);
        return item;
    }
}
