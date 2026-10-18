// <copyright file="TestRegistrationProxyTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.Tests.Api;

using System.Net;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MUnique.OpenMU.Web.AdminPanel;

/// <summary>Tests the actual registration proxy configuration against trusted and untrusted senders.</summary>
[TestFixture]
public class TestRegistrationProxyTests
{
    /// <summary>Only an enabled loopback proxy may supply the original HTTPS scheme and client address.</summary>
    /// <param name="enabled">Whether the local gateway is enabled.</param>
    /// <param name="remoteAddress">The sender of the forwarded request.</param>
    /// <param name="forwarded">Whether the headers should be trusted.</param>
    [TestCase(true, "127.0.0.1", true)]
    [TestCase(true, "::1", true)]
    [TestCase(true, "203.0.113.99", false)]
    [TestCase(false, "127.0.0.1", false)]
    public async Task OnlyConfiguredLoopbackGatewayCanForwardHttpsAsync(bool enabled, string remoteAddress, bool forwarded)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["TestRegistration:TrustLoopbackProxy"] = enabled.ToString(),
        }).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        typeof(Startup).Assembly.GetType("MUnique.OpenMU.Web.AdminPanel.TestRegistrationExtensions")!
            .GetMethod("AddTestRegistration", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [services]);
        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<ForwardedHeadersOptions>>();
        var context = new DefaultHttpContext();
        context.Request.Scheme = "http";
        context.Connection.RemoteIpAddress = IPAddress.Parse(remoteAddress);
        context.Request.Headers["X-Forwarded-Proto"] = "https";
        context.Request.Headers["X-Forwarded-For"] = "203.0.113.7";
        var middleware = new ForwardedHeadersMiddleware(_ => Task.CompletedTask, NullLoggerFactory.Instance, options);
        await middleware.Invoke(context).ConfigureAwait(false);
        Assert.That(context.Request.IsHttps, Is.EqualTo(forwarded));
        Assert.That(context.Connection.RemoteIpAddress, Is.EqualTo(IPAddress.Parse(forwarded ? "203.0.113.7" : remoteAddress)));
    }
}
