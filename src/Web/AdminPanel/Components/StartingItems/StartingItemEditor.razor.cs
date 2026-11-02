// <copyright file="StartingItemEditor.razor.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.Components.StartingItems;

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Microsoft.AspNetCore.Components;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.Persistence.Initialization.PlugIns.CharacterCreated;

/// <summary>Uses the inventory editor's lookup widgets for a starter-item policy.</summary>
public partial class StartingItemEditor
{
    private StartingItemReward? _loadedReward;
    private ItemDefinition? _definition;
    private ObservableCollection<IncreasableItemOption> _excellentOptions = [];

    /// <summary>Gets or sets the editable starter reward.</summary>
    [Parameter]
    [EditorRequired]
    public StartingItemReward Reward { get; set; } = null!;

    /// <summary>Gets or sets the current configuration's item definitions.</summary>
    [Parameter]
    [EditorRequired]
    public IEnumerable<ItemDefinition> Definitions { get; set; } = [];

    private IEnumerable<IncreasableItemOption> Options => this.Definition?.PossibleItemOptions.SelectMany(o => o.PossibleOptions) ?? [];

    private IEnumerable<IncreasableItemOption> NormalOptions => this.Options.Where(o => o.OptionType == ItemOptionTypes.Option && !o.IsFixedOption());

    private ItemDefinition? Definition
    {
        get => this._definition;
        set
        {
            if (this._definition == value)
            {
                return;
            }

            this._definition = value;
            this.Reward.Group = value?.Group ?? 0;
            this.Reward.Number = value?.Number ?? -1;
            this.Reward.Level = 0;
            this.Reward.Luck = false;
            this.Reward.Skill = false;
            this.Reward.ExcellentOptions.Clear();
            this.Reward.AdditionalOptionNumber = null;
            this.Reward.AdditionalOptionLevel = 0;
            this.LoadExcellentOptions();
        }
    }

    /// <summary>
    /// Gets or sets the selected excellent options.
    /// </summary>
    /// <remarks>
    /// <see cref="MultiLookupField{TObject}"/> adds and removes entries on the bound list itself and
    /// doesn't raise ValueChanged for the same list instance. So the list must be a stable instance
    /// which writes its changes back into the reward.
    /// </remarks>
    private IList<IncreasableItemOption> ExcellentOptions
    {
        get => this._excellentOptions;
        set
        {
            if (!ReferenceEquals(value, this._excellentOptions))
            {
                this.SetExcellentOptions(value);
                this.SyncExcellentOptions();
            }
        }
    }

    private IncreasableItemOption? AdditionalOption
    {
        get => this.Reward.AdditionalOptionLevel == 0 ? null : this.NormalOptions.FirstOrDefault(o =>
            !this.Reward.AdditionalOptionNumber.HasValue || o.Number == this.Reward.AdditionalOptionNumber);
        set
        {
            this.Reward.AdditionalOptionNumber = value?.Number;
            this.Reward.AdditionalOptionLevel = value is null ? (byte)0 : (byte)value.LevelDependentOptions.Min(l => l.Level);
        }
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (this._loadedReward == this.Reward)
        {
            return;
        }

        this._loadedReward = this.Reward;
        this._definition = this.Definitions.SingleOrDefault(i => i.Group == this.Reward.Group && i.Number == this.Reward.Number);
        this.LoadExcellentOptions();
    }

    private static string OptionCaption(IncreasableItemOption? option)
    {
        if (option is null)
        {
            return string.Empty;
        }

        return (option.PowerUpDefinition ?? option.LevelDependentOptions.FirstOrDefault()?.PowerUpDefinition)?.ToString() ?? option.ToString();
    }

    private void LoadExcellentOptions()
    {
        this.SetExcellentOptions(this.Options.Where(o => o.OptionType == ItemOptionTypes.Excellent && this.Reward.ExcellentOptions.Contains(o.Number)));
    }

    private void SetExcellentOptions(IEnumerable<IncreasableItemOption> options)
    {
        this._excellentOptions.CollectionChanged -= this.OnExcellentOptionsChanged;
        this._excellentOptions = new ObservableCollection<IncreasableItemOption>(options);
        this._excellentOptions.CollectionChanged += this.OnExcellentOptionsChanged;
    }

    private void OnExcellentOptionsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        this.SyncExcellentOptions();
    }

    private void SyncExcellentOptions()
    {
        this.Reward.ExcellentOptions = this._excellentOptions.Select(o => o.Number).Distinct().ToList();
    }
}
