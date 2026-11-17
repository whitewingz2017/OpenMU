// <copyright file="MarketFeaturePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Market;

using System.Runtime.InteropServices;
using System.Threading;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// The Marketplace of the custom client: players list inventory items for zen, WCoinC or jewels; buyers
/// get the item by mail, sellers the price after the tax. Unsold items go back by mail when the listing
/// expires. The listings are stored, and shared by the game servers.
/// </summary>
[PlugIn]
[Display(Name = nameof(MarketResources.Name), Description = nameof(MarketResources.Description), ResourceType = typeof(MarketResources))]
[Guid("a29989a5-4e2d-43d8-ae4d-15a14922a1ba")]
public sealed class MarketFeaturePlugIn : IFeaturePlugIn, IPeriodicTaskPlugIn, ISupportCustomConfiguration<MarketSettings>, ISupportDefaultCustomConfiguration
{
    /// <summary>The time between the checks for expired listings.</summary>
    private static readonly TimeSpan ExpiryInterval = TimeSpan.FromMinutes(1);

    private DateTime _nextExpiryUtc = DateTime.UtcNow;
    private int _expiring;

    /// <inheritdoc />
    public MarketSettings? Configuration { get; set; }

    /// <summary>Gets the settings.</summary>
    public MarketSettings Settings => this.Configuration ??= new MarketSettings();

    /// <summary>Gets the listings of this process.</summary>
    public MarketRegistry Registry { get; } = new();

    /// <summary>Gets the active plugin.</summary>
    /// <param name="context">The game context.</param>
    /// <returns>The plugin, or <c>null</c> if the Marketplace is disabled.</returns>
    public static MarketFeaturePlugIn? Get(IGameContext context) => context.FeaturePlugIns.GetPlugIn<MarketFeaturePlugIn>();

    /// <inheritdoc />
    public object CreateDefaultConfig() => new MarketSettings();

    /// <inheritdoc />
    public async ValueTask ExecuteTaskAsync(GameContext gameContext)
    {
        // The plugin is shared by the game servers of the process, and the timer doesn't await a run.
        if (DateTime.UtcNow < this._nextExpiryUtc || Interlocked.Exchange(ref this._expiring, 1) == 1)
        {
            return;
        }

        try
        {
            this._nextExpiryUtc = DateTime.UtcNow + ExpiryInterval;
            await this.ExpireAsync(gameContext).ConfigureAwait(false);
        }
        finally
        {
            Volatile.Write(ref this._expiring, 0);
        }
    }

    /// <inheritdoc />
    public void ForceStart()
    {
        this._nextExpiryUtc = DateTime.UtcNow;
    }

    private async ValueTask ExpireAsync(GameContext gameContext)
    {
        var logger = gameContext.LoggerFactory.CreateLogger<MarketFeaturePlugIn>();
        await this.Registry.WriteLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (await MarketReturn.ExpireAsync(gameContext, logger, this.Settings).ConfigureAwait(false) > 0)
            {
                this.Registry.Invalidate();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "The expired Marketplace listings couldn't be returned");
        }
        finally
        {
            this.Registry.WriteLock.Release();
        }
    }
}
