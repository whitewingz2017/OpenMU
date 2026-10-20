// <copyright file="CharacterMagicEffect.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.DataModel.Entities;

using MUnique.OpenMU.DataModel.Configuration;

/// <summary>
/// A purchased effect with an absolute deadline, retained while the character is offline.
/// </summary>
public class CharacterMagicEffect
{
    private DateTime _expiresAtUtc;

    /// <summary>
    /// Gets or sets the effect which provides the configured bonuses and conflict group.
    /// </summary>
    public virtual MagicEffectDefinition? Definition { get; set; }

    /// <summary>
    /// Gets or sets the UTC instant when the effect expires.
    /// </summary>
    public DateTime ExpiresAtUtc
    {
        get => this._expiresAtUtc.Kind == DateTimeKind.Local ? this._expiresAtUtc.ToUniversalTime() : this._expiresAtUtc;
        set => this._expiresAtUtc = value.Kind == DateTimeKind.Local ? value.ToUniversalTime() : DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }
}
