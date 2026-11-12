// <copyright file="SkinShop.razor.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.Pages;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.Cosmetics;
using MUnique.OpenMU.Persistence;
using MUnique.OpenMU.Web.AdminPanel.Components.SkinShop;
using MUnique.OpenMU.Web.Shared.Models;
using MUnique.OpenMU.Web.Shared.Services;

/// <summary>Configures skin shop availability and prices through the existing plugin persistence.</summary>
public partial class SkinShop
{
    private SkinShopSettings? _configuration;
    private GameConfiguration? _gameConfiguration;
    private PlugInConfigurationViewItem? _plugIn;
    private string _status = string.Empty;
    private bool _statusIsError;
    private bool _saving;

    [Inject]
    private PlugInController PlugInController { get; set; } = null!;

    [Inject]
    private IDataSource<GameConfiguration> ConfigurationSource { get; set; } = null!;

    [Inject]
    private ILogger<SkinShop> Logger { get; set; } = null!;

    /// <inheritdoc />
    protected override Task OnInitializedAsync() => this.LoadAsync();

    private async Task LoadAsync()
    {
        try
        {
            this._gameConfiguration = await this.ConfigurationSource.GetOwnerAsync(Guid.Empty).ConfigureAwait(true);
            this._plugIn = await this.PlugInController.GetByIdAsync(typeof(SkinShopFeaturePlugIn).GUID).ConfigureAwait(true);
            if (this._plugIn is null)
            {
                this.SetStatus("Missing", true);
                return;
            }

            var loaded = await this.PlugInController.LoadCustomConfigurationAsync(this._plugIn).ConfigureAwait(true) as SkinShopSettings;
            this._configuration = SkinShopEditor.Normalize(loaded, this._gameConfiguration.Items);
        }
        catch (Exception exception)
        {
            this.Logger.LogError(exception, "Loading the skin shop configuration failed.");
            this.SetStatus("LoadFailed", true);
        }
    }

    private async Task SaveAsync(EditContext context)
    {
        if (this._saving || this._configuration is null || this._plugIn is null)
        {
            return;
        }

        if (!context.Validate() || !SkinShopEditor.IsValid(this._configuration))
        {
            this.SetStatus("Invalid", true);
            return;
        }

        this._saving = true;
        try
        {
            await this.PlugInController.SaveCustomConfigurationAsync(this._plugIn, this._configuration).ConfigureAwait(true);
            this.SetStatus("Saved", false);
        }
        catch (Exception exception)
        {
            this.Logger.LogError(exception, "Saving the skin shop configuration failed.");
            this.SetStatus("SaveFailed", true);
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

    private void SetStatus(string key, bool isError)
    {
        this._status = SkinShopEditor.Text(key);
        this._statusIsError = isError;
    }
}
