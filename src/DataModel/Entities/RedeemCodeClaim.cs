// <copyright file="RedeemCodeClaim.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.DataModel.Entities;

/// <summary>A durable, unique receipt saved in the same transaction as its rewards.</summary>
[AggregateRoot]
public class RedeemCodeClaim
{
    /// <summary>Gets or sets the identifier derived from the code and its account or character.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the code identifier.</summary>
    public Guid CodeId { get; set; }
    /// <summary>Gets or sets the account identifier.</summary>
    public Guid AccountId { get; set; }
    /// <summary>Gets or sets the character identifier.</summary>
    public Guid CharacterId { get; set; }
    /// <summary>Gets or sets the character name at claim time.</summary>
    public string CharacterName { get; set; } = string.Empty;
    /// <summary>Gets or sets the UTC claim timestamp.</summary>
    public DateTime RedeemedAt { get; set; }
}
