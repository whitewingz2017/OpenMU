// <copyright file="StartingItemsEditor.razor.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.Components.StartingItems;

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Persistence;
using MUnique.OpenMU.Persistence.Initialization.PlugIns.CharacterCreated;
using MUnique.OpenMU.Web.Shared.Components.Modal;

/// <summary>Edits starter items by name without creating real inventory objects.</summary>
public partial class StartingItemsEditor
{
    private GameConfiguration? _configuration;
    private bool _invalid;

    /// <summary>Gets or sets the editable plugin configuration.</summary>
    [Parameter]
    [EditorRequired]
    public StartingItemsConfiguration Item { get; set; } = null!;

    /// <summary>Gets or sets the modal instance.</summary>
    [Parameter]
    public ModalInstance Modal { get; set; } = null!;

    /// <summary>Gets or sets the context for item lookups.</summary>
    [Parameter]
    public IContext PersistenceContext { get; set; } = null!;

    [Inject]
    private IDataSource<GameConfiguration> ConfigurationSource { get; set; } = null!;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        this._configuration = await this.ConfigurationSource.GetOwnerAsync(Guid.Empty).ConfigureAwait(true);
    }

    private static void AddItem(StartingClassItems profile)
    {
        profile.Items.Add(new StartingItemReward { Number = -1 });
    }

    private async Task SubmitAsync(EditContext context)
    {
        this._invalid = !context.Validate() || !this.IsValid();
        if (this._invalid)
        {
            return;
        }

        await this.Modal.CloseAsync(ModalResult.Ok(this.Item)).ConfigureAwait(true);
    }

    private bool IsValid()
    {
        if (this.Item.Classes.Select(c => c.Class).Distinct().Count() != this.Item.Classes.Count)
        {
            return false;
        }

        foreach (var profile in this.Item.Classes)
        {
            if (!Enum.IsDefined(profile.Class) || profile.Items.Sum(i => (long)i.Quantity) > 64)
            {
                return false;
            }

            foreach (var reward in profile.Items)
            {
                var definition = this._configuration!.Items.SingleOrDefault(i => i.Group == reward.Group && i.Number == reward.Number);
                if (definition is null || !Validator.TryValidateObject(reward, new ValidationContext(reward), null, true))
                {
                    return false;
                }

                try
                {
                    StartingItemOptions.CreateItem(reward, definition);
                }
                catch (InvalidOperationException)
                {
                    return false;
                }
            }
        }

        return true;
    }
}
