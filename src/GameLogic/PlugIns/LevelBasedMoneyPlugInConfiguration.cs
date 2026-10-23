// <copyright file="LevelBasedMoneyPlugInConfiguration.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

/// <summary>
/// Configures monster Zen independently of character levels and experience rates.
/// </summary>
public class LevelBasedMoneyPlugInConfiguration
{
    /// <summary>
    /// Gets or sets the base Zen added to every monster's payout.
    /// </summary>
    [Range(0, int.MaxValue)]
    [Display(ResourceType = typeof(PlugInResources), Name = nameof(PlugInResources.LevelBasedMoneyPlugInConfiguration_BaseZen_Name))]
    public int BaseZen { get; set; } = 200;

    /// <summary>
    /// Gets or sets the Zen added per monster level.
    /// </summary>
    [Range(0, int.MaxValue)]
    [Display(ResourceType = typeof(PlugInResources), Name = nameof(PlugInResources.LevelBasedMoneyPlugInConfiguration_ZenPerMonsterLevel_Name))]
    public int ZenPerMonsterLevel { get; set; } = 75;

    /// <summary>
    /// Gets or sets the inclusive maximum random Zen added to the payout.
    /// </summary>
    [Range(0, int.MaxValue)]
    [Display(ResourceType = typeof(PlugInResources), Name = nameof(PlugInResources.LevelBasedMoneyPlugInConfiguration_MaximumRandomZen_Name))]
    public int MaximumRandomZen { get; set; } = 100;
}
