// <copyright file="StartingItemEditor.razor.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.Components.StartingItems;

using Microsoft.AspNetCore.Components;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.Persistence.Initialization.PlugIns.CharacterCreated;

/// <summary>Uses the inventory editor's lookup widgets for a starter-item policy.</summary>
public partial class StartingItemEditor
{
    private StartingItemReward? _loadedReward;
    private ItemDefinition? _definition;

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
        }
    }

    private IList<IncreasableItemOption> ExcellentOptions
    {
        get => this.Options.Where(o => o.OptionType == ItemOptionTypes.Excellent && this.Reward.ExcellentOptions.Contains(o.Number)).ToList();
        set => this.Reward.ExcellentOptions = value.Select(o => o.Number).ToList();
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
    }

    private static string OptionCaption(IncreasableItemOption? option)
    {
        if (option is null)
        {
            return string.Empty;
        }

        return (option.PowerUpDefinition ?? option.LevelDependentOptions.FirstOrDefault()?.PowerUpDefinition)?.ToString() ?? option.ToString();
    }
}
