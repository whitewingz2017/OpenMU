// <copyright file="StartingItems.razor.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.Pages;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.Persistence;
using MUnique.OpenMU.Persistence.Initialization.PlugIns.CharacterCreated;
using MUnique.OpenMU.Web.AdminPanel.Components.StartingItems;
using MUnique.OpenMU.Web.Shared.Models;
using MUnique.OpenMU.Web.Shared.Services;

/// <summary>
/// Page to configure the starting items and Zen of each character class.
/// </summary>
public partial class StartingItems
{
    private StartingItemsConfiguration? _configuration;
    private GameConfiguration? _gameConfiguration;
    private PlugInConfigurationViewItem? _plugIn;
    private IContext? _persistenceContext;
    private string _status = string.Empty;
    private bool _statusIsError;
    private bool _loadFailed;
    private bool _saving;

    [Inject]
    private PlugInController PlugInController { get; set; } = null!;

    [Inject]
    private IDataSource<GameConfiguration> ConfigurationSource { get; set; } = null!;

    [Inject]
    private ILogger<StartingItems> Logger { get; set; } = null!;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        await this.LoadAsync().ConfigureAwait(true);
    }

    private async Task LoadAsync()
    {
        try
        {
            this._gameConfiguration = await this.ConfigurationSource.GetOwnerAsync(Guid.Empty).ConfigureAwait(true);
            this._persistenceContext = await this.ConfigurationSource.GetContextAsync().ConfigureAwait(true);
            this._plugIn = await this.PlugInController.GetByIdAsync(typeof(StartingItemsPlugIn).GUID).ConfigureAwait(true);
            if (this._plugIn is null)
            {
                this.SetStatus("The starting items plugin configuration was not found. Check the Plugins page.", true);
                this._loadFailed = true;
                return;
            }

            var loaded = await this.PlugInController.LoadCustomConfigurationAsync(this._plugIn).ConfigureAwait(true) as StartingItemsConfiguration;
            this._configuration = StartingItemsEditor.Normalize(loaded);
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Loading the starting items configuration failed");
            this.SetStatus("Could not load the starting items configuration. Check the server logs.", true);
            this._loadFailed = true;
        }
    }

    private async Task SaveAsync(EditContext context)
    {
        if (this._saving || this._configuration is null || this._gameConfiguration is null || this._plugIn is null)
        {
            return;
        }

        if (!context.Validate() || !StartingItemsEditor.IsValid(this._configuration, this._gameConfiguration.Items))
        {
            this.SetStatus(AdministrationResources.StartingInvalidItems, true);
            return;
        }

        this._saving = true;
        try
        {
            await this.PlugInController.SaveCustomConfigurationAsync(this._plugIn, this._configuration).ConfigureAwait(true);
            this.SetStatus(AdministrationResources.StartingSaved, false);
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Saving the starting items configuration failed");
            this.SetStatus("Could not save the starting items configuration. Check the server logs.", true);
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
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Activating the starting items plugin failed");
            this.SetStatus("Could not activate the plugin. Check the server logs.", true);
        }
        finally
        {
            this._saving = false;
        }
    }

    private void SetStatus(string text, bool isError)
    {
        this._status = text;
        this._statusIsError = isError;
    }
}
