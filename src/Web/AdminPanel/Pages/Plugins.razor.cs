// <copyright file="Plugins.razor.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.Pages;

using Microsoft.AspNetCore.Components;
using MUnique.OpenMU.Persistence.Initialization.PlugIns.CharacterCreated;
using MUnique.OpenMU.Web.Shared.Models;
using MUnique.OpenMU.Web.Shared.Services;

/// <summary>
/// Code-behind for the <see cref="Plugins"/> page.
/// </summary>
public partial class Plugins
{
    private Guid? _configurationToOpen;

    /// <summary>
    /// Gets or sets the plug-in identifier.
    /// </summary>
    [SupplyParameterFromQuery(Name = "id")]
    public string? PlugInId { get; set; }

    [Inject]
    private PlugInController PlugInController { get; set; } = null!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = null!;

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync().ConfigureAwait(true);

        if (Guid.TryParse(this.PlugInId, out var id))
        {
            var plugin = await this.PlugInController.GetByIdAsync(id).ConfigureAwait(true);

            if (plugin is { })
            {
                this.PlugInController.NameFilter = plugin.PlugInName ?? string.Empty;
                this.PlugInController.TypeFilter = string.Empty;
                this.PlugInController.PointFilter = Guid.Empty;

                if (plugin.ConfigurationType is { })
                {
                    this._configurationToOpen = id;
                }
            }

            this.PlugInId = null;
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender).ConfigureAwait(true);
        if (this._configurationToOpen is not { } id)
        {
            return;
        }

        this._configurationToOpen = null;
        if (await this.PlugInController.GetByIdAsync(id).ConfigureAwait(true) is { } plugin)
        {
            await this.ShowConfigurationAsync(plugin).ConfigureAwait(true);
        }
    }

    private Task ShowConfigurationAsync(PlugInConfigurationViewItem plugin)
    {
        if (plugin.ConfigurationType == typeof(StartingItemsConfiguration))
        {
            this.NavigationManager.NavigateTo("starting-items");
            return Task.CompletedTask;
        }

        return this.PlugInController.ShowPlugInConfigAsync(plugin);
    }

    private void OnPlugInPointSelected(ChangeEventArgs args)
    {
        if (args.Value is string guidString && Guid.TryParse(guidString, out var result))
        {
            this.PlugInController.PointFilter = result;
        }
    }
}
