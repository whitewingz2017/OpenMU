// <copyright file="MuunFeaturePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Muun;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.GameLogic.Views.Inventory;
using MUnique.OpenMU.PlugIns;

/// <summary>Enables independent Muun equipment and refreshes conditional bonuses.</summary>
[PlugIn]
[Display(Name = nameof(MuunResources.Name), Description = nameof(MuunResources.Description), ResourceType = typeof(MuunResources))]
[Guid("f43252e4-5cbc-46b1-a283-ed91758d58bc")]
public sealed class MuunFeaturePlugIn : IFeaturePlugIn, IPlayerStateChangedPlugIn, IAttackableGotKilledPlugIn, IPeriodicTaskPlugIn, ISupportCustomConfiguration<MuunConfiguration>, ISupportDefaultCustomConfiguration, IDisabledByDefault
{
    /// <inheritdoc />
    public MuunConfiguration? Configuration { get; set; }
    /// <summary>Gets the active settings; disabled servers grant no Muun bonuses.</summary>
    public static MuunConfiguration? Settings(IGameContext context)
        => context.FeaturePlugIns.GetPlugIn<MuunFeaturePlugIn>() is { } plugin ? (plugin.Configuration ??= new MuunConfiguration()) : null;
    /// <inheritdoc />
    public object CreateDefaultConfig() => new MuunConfiguration();
    /// <inheritdoc />
    public async ValueTask AttackableGotKilledAsync(IAttackable killed, IAttacker? killer)
    {
        if (killed is not Player player || Settings(player.GameContext) is not { LifeLossOnDeath: > 0 } settings) return;
        await player.RunPersistenceExclusiveAsync(async () =>
        {
            var items = player.SelectedCharacter?.MuunInventory?.Items.Where(i => i.ItemSlot < MuunRules.EquipmentCount && MuunRules.Find(settings, MuunRules.Type(i)) is not null).ToArray() ?? [];
            if (items.Length == 0) return;
            var life = items.Select(i => i.Durability).ToArray();
            var committed = false;
            try
            {
                foreach (var item in items) item.Durability = Math.Max(0, item.Durability - settings.LifeLossOnDeath);
                committed = await player.SaveProgressAsync().ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                player.Logger.LogError(exception, "Saving Muun life loss failed for {Player}.", player);
            }
            finally
            {
                if (!committed) for (var i = 0; i < items.Length; i++) items[i].Durability = life[i];
                MuunBonuses.Refresh(player);
                await player.ForEachWorldObserverAsync<IMuunView>(v => v.AppearanceAsync(player), true).ConfigureAwait(false);
            }
        }).ConfigureAwait(false);
    }
    /// <inheritdoc />
    public async ValueTask PlayerStateChangedAsync(Player player, State previousState, State currentState)
    {
        if (currentState != PlayerState.EnteredWorld) return;
        MuunBonuses.Refresh(player);
        await player.ForEachWorldObserverAsync<IMuunView>(v => v.AppearanceAsync(player), true).ConfigureAwait(false);
    }
    /// <inheritdoc />
    public void ForceStart() { }
    /// <inheritdoc />
    public async ValueTask ExecuteTaskAsync(GameContext context)
    {
        foreach (var player in await context.GetPlayersAsync().ConfigureAwait(false))
        {
            if (player.PlayerState.CurrentState == PlayerState.EnteredWorld)
            {
                await player.RunPersistenceExclusiveAsync(async () =>
                {
                    if (MuunBonuses.Refresh(player))
                    {
                        await player.ForEachWorldObserverAsync<IMuunView>(v => v.AppearanceAsync(player), true).ConfigureAwait(false);
                    }
                }).ConfigureAwait(false);
            }
        }
    }
}
