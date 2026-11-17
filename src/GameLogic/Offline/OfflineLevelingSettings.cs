// <copyright file="OfflineLevelingSettings.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Offline;

/// <summary>The prices and limits of offline leveling (/offlevel), edited on the plugin's Configure page.</summary>
public class OfflineLevelingSettings
{
    /// <summary>Gets or sets a value indicating whether players may pay with zen.</summary>
    [Display(Name = "Allow Zen")]
    public bool AllowZen { get; set; } = true;

    /// <summary>Gets or sets the zen of an hour; 0 keeps the MU Helper's level-based zen cost.</summary>
    [Display(Name = "Zen per hour", Description = "0: the MU Helper's level-based cost (its cost stages and pay interval).")]
    [Range(0, int.MaxValue)]
    public int ZenPerHour { get; set; }

    /// <summary>Gets or sets a value indicating whether players may pay with WCoinC.</summary>
    [Display(Name = "Allow WCoinC")]
    public bool AllowWCoinC { get; set; } = true;

    /// <summary>Gets or sets the WCoinC of an hour.</summary>
    [Display(Name = "WCoinC per hour")]
    [Range(0, int.MaxValue)]
    public int WCoinCPerHour { get; set; } = 30;

    /// <summary>Gets or sets a value indicating whether players may pay with Goblin Points.</summary>
    [Display(Name = "Allow Goblin Points")]
    public bool AllowGoblinPoints { get; set; } = true;

    /// <summary>Gets or sets the Goblin Points of an hour.</summary>
    [Display(Name = "Goblin Points per hour")]
    [Range(0, int.MaxValue)]
    public int GoblinPointsPerHour { get; set; } = 50;

    /// <summary>Gets or sets a value indicating whether players may buy the protection from players.</summary>
    [Display(Name = "Allow protection", Description = "Protected offline characters can't be damaged by players; monsters still hit them.")]
    public bool AllowProtection { get; set; } = true;

    /// <summary>Gets or sets the zen of an hour of protection; 0 bills a zen session's protection in WCoinC.</summary>
    [Display(Name = "Protection: Zen per hour", Description = "0: a session paid with zen pays its protection in WCoinC.")]
    [Range(0, int.MaxValue)]
    public int ProtectionZenPerHour { get; set; }

    /// <summary>Gets or sets the WCoinC of an hour of protection.</summary>
    [Display(Name = "Protection: WCoinC per hour")]
    [Range(0, int.MaxValue)]
    public int ProtectionWCoinCPerHour { get; set; } = 30;

    /// <summary>Gets or sets the Goblin Points of an hour of protection.</summary>
    [Display(Name = "Protection: Goblin Points per hour")]
    [Range(0, int.MaxValue)]
    public int ProtectionGoblinPointsPerHour { get; set; } = 50;

    /// <summary>Gets or sets the most hours a player may pay in advance.</summary>
    [Display(Name = "Maximum prepaid hours")]
    [Range(1, 720)]
    public int MaximumHours { get; set; } = 24;
}
