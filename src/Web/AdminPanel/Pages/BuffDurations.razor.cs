// <copyright file="BuffDurations.razor.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.Pages;

using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using MUnique.OpenMU.DataModel.Attributes;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Persistence;
using MUnique.OpenMU.Web.AdminPanel.Properties;
using MUnique.OpenMU.Web.Shared.Components.Toast;
using MUnique.OpenMU.Web.Shared.Services;

/// <summary>
/// Page to configure the durations of the timed magic effects, e.g. of buff skills like Infinite Arrow or Greater Damage,
/// on one page.
/// </summary>
public partial class BuffDurations
{
    private List<MagicEffectDefinition>? _effects;
    private IContext? _persistenceContext;
    private string _filter = string.Empty;

    [Inject]
    private IDataSource<GameConfiguration> DataSource { get; set; } = null!;

    [Inject]
    private IToastService ToastService { get; set; } = null!;

    [Inject]
    private ILogger<BuffDurations> Logger { get; set; } = null!;

    private IEnumerable<MagicEffectDefinition> FilteredEffects => string.IsNullOrWhiteSpace(this._filter)
        ? this._effects ?? []
        : (this._effects ?? []).Where(e => e.Name.ToString()?.Contains(this._filter, StringComparison.OrdinalIgnoreCase) is true
                                           || e.Number.ToString(CultureInfo.InvariantCulture) == this._filter.Trim());

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        await this.LoadAsync().ConfigureAwait(true);
    }

    private static string Text(string key)
    {
        return Resources.ResourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? key;
    }

    private static void SetSeconds(PowerUpDefinitionValue duration, object? input)
    {
        if (float.TryParse(input as string, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds) && seconds >= 0)
        {
            duration.ConstantValue.Value = seconds;
        }
    }

    /// <summary>
    /// Describes what else changes the duration, so that the admin knows that the seconds are just the base value.
    /// </summary>
    private static string ScalingText(MagicEffectDefinition effect)
    {
        if (effect.Duration is not { } duration)
        {
            return string.Empty;
        }

        var parts = duration.RelatedValues
            .Where(r => r.InputAttribute is not null)
            .Select(r => $"{r.InputAttribute!.Designation} ({r.InputOperator} {r.InputOperand.ToString(CultureInfo.CurrentCulture)})")
            .ToList();
        if (effect.DurationDependsOnTargetLevel)
        {
            parts.Add(Text("BuffDurations_TargetLevel"));
        }

        if (duration.MaximumValue is { } maximum)
        {
            parts.Add(string.Format(CultureInfo.CurrentCulture, Text("BuffDurations_Maximum"), maximum));
        }

        return string.Join(", ", parts);
    }

    private async Task LoadAsync()
    {
        try
        {
            this._persistenceContext = await this.DataSource.GetContextAsync().ConfigureAwait(true);
            var configuration = await this.DataSource.GetOwnerAsync(Guid.Empty).ConfigureAwait(true);
            this._effects = configuration.MagicEffects
                .Where(e => e.Duration is not null)
                .OrderBy(e => e.Name.ToString())
                .ThenBy(e => e.Number)
                .ToList();
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Loading the magic effects failed");
            this.ToastService.ShowError(string.Format(Resources.UnexpectedErrorOccurred, ex.Message));
        }
    }

    private async Task SaveAsync()
    {
        try
        {
            if (this._persistenceContext is not { } context)
            {
                this.ToastService.ShowError(Resources.FailedByUninitializedContext);
                return;
            }

            var success = await context.SaveChangesAsync().ConfigureAwait(true);
            this.ToastService.ShowSuccess(success ? Resources.SavedChanges : Resources.NoChangesToSave);
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Saving the magic effect durations failed");
            this.ToastService.ShowError(string.Format(Resources.UnexpectedErrorOccurred, ex.Message));
        }
    }

    private async Task DiscardAsync()
    {
        await this.DataSource.DiscardChangesAsync().ConfigureAwait(true);
        this._effects = null;
        await this.LoadAsync().ConfigureAwait(true);
    }
}
