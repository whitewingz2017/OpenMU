// <copyright file="StartingItemsFormTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.Tests.Components;

using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MUnique.OpenMU.Persistence;
using MUnique.OpenMU.Persistence.Initialization.PlugIns.CharacterCreated;
using MUnique.OpenMU.Web.Shared.Components.Form;
using MUnique.OpenMU.Web.Shared.Components.Modal;
using MUnique.OpenMU.Web.Shared.Services;

/// <summary>
/// Tests the starting-item configuration in the automatic admin form.
/// </summary>
[TestFixture]
public class StartingItemsFormTests
{
    /// <summary>
    /// All class profiles are editable and support creating reward rows locally.
    /// </summary>
    [Test]
    public void ClassProfilesRenderAndCreateRewards()
    {
        using var context = new BunitContext();
        context.Services.AddSingleton(Mock.Of<IModalService>());
        context.Services.AddSingleton(Mock.Of<IChangeNotificationService>());
        var configuration = new StartingItemsConfiguration();
        var form = context.Render<CascadingValue<IContext>>(parameters => parameters
            .Add(p => p.Value, Mock.Of<IContext>())
            .AddChildContent<EditForm>(edit => edit
                .Add(p => p.Model, configuration)
                .Add(p => p.ChildContent, _ => builder =>
                {
                    builder.OpenComponent<AutoFields>(0);
                    builder.CloseComponent();
                })));

        Assert.That(form.FindComponents<EnumField<StartingCharacterClass>>(), Has.Count.EqualTo(7));
        var rewards = form.FindComponents<ItemTable<StartingItemReward>>();
        Assert.That(rewards, Has.Count.EqualTo(7));
        rewards[0].Find("tfoot button").Click();
        Assert.That(configuration.Classes.Sum(c => c.Items.Count), Is.EqualTo(1));
        Assert.That(form.FindComponents<ByteField>().Count, Is.EqualTo(3));
        Assert.That(form.FindComponents<IntCollectionField>(), Has.Count.EqualTo(1));
        var excellent = form.FindComponent<IntCollectionField>();
        excellent.Find("input").Change("2;3;4");
        var reward = configuration.Classes.SelectMany(c => c.Items).Single();
        Assert.That(reward.ExcellentOptions, Is.EqualTo(new[] { 2, 3, 4 }));
        Assert.That(form.FindComponents<ShortField>(), Has.Count.EqualTo(1));
    }
}
