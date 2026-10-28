// <copyright file="RedeemCode.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.DataModel.Entities;

using System.ComponentModel.DataAnnotations;

/// <summary>An administrator-managed gift code. Rewards and its usage count commit together.</summary>
[AggregateRoot]
public class RedeemCode
{
    /// <summary>Gets or sets the stable identifier derived from the normalized code.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the case-insensitive ASCII code.</summary>
    [MaxLength(32)]
    public string Code { get; set; } = string.Empty;
    /// <summary>Gets or sets an operator description.</summary>
    public string Description { get; set; } = string.Empty;
    /// <summary>Gets or sets a value indicating whether claims are allowed.</summary>
    public bool Enabled { get; set; }
    /// <summary>Gets or sets the optional UTC expiry.</summary>
    public DateTime? ExpiresAt { get; set; }
    /// <summary>Gets or sets the total claim limit, or zero for unlimited.</summary>
    public int MaximumUses { get; set; }
    /// <summary>Gets or sets the number of committed claims.</summary>
    public int UsedCount { get; set; }
    /// <summary>Gets or sets a value indicating whether each character, rather than each account, may claim once.</summary>
    public bool PerCharacter { get; set; }
    /// <summary>Gets or sets the reward list as JSON of ItemCode, Level and Count.</summary>
    public string ItemsJson { get; set; } = "[]";
    /// <summary>Gets or sets the WCoin C reward.</summary>
    public int WCoinC { get; set; }
    /// <summary>Gets or sets the WCoin P reward.</summary>
    public int WCoinP { get; set; }
    /// <summary>Gets or sets the Goblin Points reward.</summary>
    public int GoblinPoints { get; set; }
    /// <summary>Gets or sets the concurrency token, replaced by every edit or claim.</summary>
    [ConcurrencyCheck]
    public Guid Revision { get; set; } = Guid.NewGuid();
}
