// <copyright file="BossKillRewards.razor.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.Pages;

using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.PlugIns.BossKillRewards;
using MUnique.OpenMU.Web.AdminPanel.Properties;
using MUnique.OpenMU.Web.Shared.Models;
using MUnique.OpenMU.Web.Shared.Services;

/// <summary>
/// Page to configure the rewards of the <see cref="BossKillRewardPlugIn"/>: per boss, chosen from the monster list,
/// the WCoinC, WCoinP and Goblin Points of its killer.
/// </summary>
public partial class BossKillRewards
{
    private BossKillRewardConfiguration? _configuration;
    private PlugInConfigurationViewItem? _plugIn;
    private List<MonsterDefinition> _monsters = [];
    private string _status = string.Empty;
    private bool _statusIsError;
    private bool _saving;

    [Inject]
    private PlugInController PlugInController { get; set; } = null!;

    [Inject]
    private IDataSource<GameConfiguration> ConfigurationSource { get; set; } = null!;

    [Inject]
    private ILogger<BossKillRewards> Logger { get; set; } = null!;

    /// <inheritdoc />
    protected override Task OnInitializedAsync() => this.LoadAsync();

    private static string Text(string key) => Resources.ResourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? key;

    private static string MonsterCaption(MonsterDefinition monster) => $"{monster.Designation} ({monster.Number})";

    private static void SetMonster(BossKillReward reward, object? value)
    {
        if (short.TryParse(value as string, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number))
        {
            reward.MonsterNumber = number;
        }
    }

    private MonsterDefinition? FindMonster(short number) => this._monsters.FirstOrDefault(m => m.Number == number);

    private void AddReward()
    {
        this._configuration?.Rewards.Add(new BossKillReward { MonsterNumber = -1 });
    }

    private async Task LoadAsync()
    {
        try
        {
            var gameConfiguration = await this.ConfigurationSource.GetOwnerAsync(Guid.Empty).ConfigureAwait(true);
            this._monsters = gameConfiguration.Monsters
                .Where(m => m.ObjectKind == NpcObjectKind.Monster)
                .OrderBy(m => m.Designation.ToString())
                .ThenBy(m => m.Number)
                .ToList();
            this._plugIn = await this.PlugInController.GetByIdAsync(typeof(BossKillRewardPlugIn).GUID).ConfigureAwait(true);
            if (this._plugIn is null)
            {
                this.SetStatus(Text("BossKillRewards_Missing"), true);
                return;
            }

            var loaded = await this.PlugInController.LoadCustomConfigurationAsync(this._plugIn).ConfigureAwait(true) as BossKillRewardConfiguration;
            this._configuration = loaded ?? new BossKillRewardConfiguration();
            this._configuration.Rewards ??= [];
        }
        catch (Exception exception)
        {
            this.Logger.LogError(exception, "Loading the boss kill reward configuration failed.");
            this.SetStatus(Text("BossKillRewards_LoadFailed"), true);
        }
    }

    private bool IsValid()
    {
        var rewards = this._configuration!.Rewards;
        return rewards.All(r => this.FindMonster(r.MonsterNumber) is not null && r is { WCoinC: >= 0, WCoinP: >= 0, GoblinPoints: >= 0 })
               && rewards.Select(r => r.MonsterNumber).Distinct().Count() == rewards.Count;
    }

    private async Task SaveAsync()
    {
        if (this._saving || this._configuration is null || this._plugIn is null)
        {
            return;
        }

        if (!this.IsValid())
        {
            this.SetStatus(Text("BossKillRewards_Invalid"), true);
            return;
        }

        this._saving = true;
        try
        {
            await this.PlugInController.SaveCustomConfigurationAsync(this._plugIn, this._configuration).ConfigureAwait(true);
            this.SetStatus(Text("BossKillRewards_Saved"), false);
        }
        catch (Exception exception)
        {
            this.Logger.LogError(exception, "Saving the boss kill reward configuration failed.");
            this.SetStatus(Text("BossKillRewards_SaveFailed"), true);
        }
        finally
        {
            this._saving = false;
        }
    }

    private async Task ActivateAsync()
    {
        if (this._plugIn is null)
        {
            return;
        }

        this._saving = true;
        try
        {
            await this.PlugInController.ActivateAsync(this._plugIn).ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            this.Logger.LogError(exception, "Activating the boss kill reward plugin failed.");
            this.SetStatus(Text("BossKillRewards_SaveFailed"), true);
        }
        finally
        {
            this._saving = false;
        }
    }

    private async Task DiscardAsync()
    {
        this._configuration = null;
        this._status = string.Empty;
        await this.LoadAsync().ConfigureAwait(true);
    }

    private void SetStatus(string text, bool isError)
    {
        this._status = text;
        this._statusIsError = isError;
    }
}
