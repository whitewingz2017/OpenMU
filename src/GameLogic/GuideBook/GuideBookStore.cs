// <copyright file="GuideBookStore.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.GuideBook;

using System.Threading;
using MUnique.OpenMU.Persistence;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Saves a change of the Guide Book which a game master made in the game: into the database, and into the
/// configuration of the running plugins.
/// </summary>
internal static class GuideBookStore
{
    /// <summary>The message key when the guide could not be saved.</summary>
    internal const string SaveFailedKey = "GuideBook_SaveFailed";

    /// <summary>Serializes the saves of all game servers of the process, so that no save gets lost.</summary>
    private static readonly SemaphoreSlim SaveLock = new(1, 1);

    /// <summary>
    /// Applies a change to the stored settings and saves them. The change reads the settings as they are in the
    /// database, not the ones in memory, so it never builds on an outdated draft.
    /// </summary>
    /// <param name="context">The game context.</param>
    /// <param name="change">Changes the settings; returns the message key of a refusal, or <c>null</c> to save them.</param>
    /// <returns>The message key of the refusal or failure, or <c>null</c> when the change was saved.</returns>
    public static async ValueTask<string?> UpdateAsync(IGameContext context, Func<GuideBookSettings, string?> change)
    {
        await SaveLock.WaitAsync().ConfigureAwait(false);
        try
        {
            using var persistenceContext = context.PersistenceContextProvider.CreateNewContext();
            var gameConfiguration = (await persistenceContext.GetAsync<GameConfiguration>().ConfigureAwait(false)).FirstOrDefault();
            var typeId = typeof(GuideBookFeaturePlugIn).GUID;
            if (gameConfiguration?.PlugInConfigurations.FirstOrDefault(c => c.TypeId == typeId) is not { } entity)
            {
                return SaveFailedKey;
            }

            var referenceHandler = context.PlugInManager.CustomConfigReferenceHandler;
            var settings = entity.GetConfiguration<GuideBookSettings>(referenceHandler) ?? new GuideBookSettings();

            // The guide in memory has the later default pages (GuideBookFeaturePlugIn.Settings), so has the stored one,
            // with the same revisions: a draft which a game master edited from it is saved, and the pages with it.
            GuideBookDefaults.AddLaterPages(settings);
            if (change(settings) is { } refusal)
            {
                return refusal;
            }

            entity.SetConfiguration(settings, referenceHandler);
            await persistenceContext.SaveChangesAsync().ConfigureAwait(false);
            ApplyToRunningPlugIns(context, settings);
            return null;
        }
        catch (Exception ex)
        {
            context.LoggerFactory.CreateLogger(typeof(GuideBookStore)).LogError(ex, "Saving the Guide Book failed.");
            return SaveFailedKey;
        }
        finally
        {
            SaveLock.Release();
        }
    }

    /// <summary>
    /// Sets the saved settings on the configuration which the plugin manager observes, so that the plugins of all
    /// game servers of the process are reconfigured, and directly on the plugin of this game server.
    /// </summary>
    private static void ApplyToRunningPlugIns(IGameContext context, GuideBookSettings settings)
    {
        var typeId = typeof(GuideBookFeaturePlugIn).GUID;
        context.Configuration.PlugInConfigurations.FirstOrDefault(c => c.TypeId == typeId)
            ?.SetConfiguration(settings, context.PlugInManager.CustomConfigReferenceHandler);
        if (context.FeaturePlugIns.GetPlugIn<GuideBookFeaturePlugIn>() is { } plugIn)
        {
            plugIn.Configuration = settings;
        }
    }
}
