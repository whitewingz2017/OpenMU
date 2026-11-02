// <copyright file="MerchantsPageTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.Tests.Pages;

using System.Threading;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Persistence;
using MUnique.OpenMU.Web.AdminPanel.Pages;
using MUnique.OpenMU.Web.Shared.Components.Toast;
using MUnique.OpenMU.Web.Shared.Services;

/// <summary>
/// Tests navigation away from the merchants page while its data is loading.
/// </summary>
[TestFixture]
public class MerchantsPageTests
{
    /// <summary>
    /// Disposing the page cancels its load and ignores a data source which was disposed during that load.
    /// </summary>
    [Test]
    public async ValueTask DisposingDuringLoadCancelsAndIgnoresDisposedSourceAsync()
    {
        using var context = new BunitContext();
        var pendingContext = new TaskCompletionSource<IContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        var loadToken = CancellationToken.None;
        var source = new Mock<IDataSource<GameConfiguration>>();
        source.Setup(s => s.GetContextAsync(It.IsAny<CancellationToken>()))
            .Returns((CancellationToken token) =>
            {
                loadToken = token;
                return new ValueTask<IContext>(pendingContext.Task);
            });
        var loadingService = new LoadingOverlayService();
        var loadingStopped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        loadingService.StateChanged += () =>
        {
            if (!loadingService.IsVisible)
            {
                loadingStopped.TrySetResult();
            }
        };
        var logger = new Mock<ILogger<Merchants>>();
        context.Services.AddSingleton(source.Object);
        context.Services.AddSingleton(Mock.Of<IPersistenceContextProvider>());
        context.Services.AddSingleton(Mock.Of<IToastService>());
        context.Services.AddSingleton(logger.Object);
        context.Services.AddSingleton(loadingService);
        context.ComponentFactories.AddStub<MUnique.OpenMU.Web.Shared.Components.Breadcrumb>();

        context.Render<Merchants>();
        Assert.That(loadingService.IsVisible, Is.True);
        await context.DisposeComponentsAsync().ConfigureAwait(false);
        Assert.That(loadToken.IsCancellationRequested, Is.True);

        pendingContext.SetException(new ObjectDisposedException("DataSource"));
        await loadingStopped.Task.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);

        Assert.Multiple(() =>
        {
            Assert.That(loadingService.IsVisible, Is.False);
            Assert.That(logger.Invocations.Any(invocation => invocation.Method.Name == nameof(ILogger.Log)
                && invocation.Arguments[0] is LogLevel.Error), Is.False);
        });
    }
}
