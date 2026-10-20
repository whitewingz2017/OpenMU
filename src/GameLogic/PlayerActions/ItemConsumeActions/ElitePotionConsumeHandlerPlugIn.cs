// <copyright file="ElitePotionConsumeHandlerPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions.ItemConsumeActions;

/// <summary>
/// Configurable instant recovery used by cash-shop elite potions.
/// </summary>
public abstract class ElitePotionConsumeHandlerPlugIn : RecoverConsumeHandlerPlugIn, IItemConsumeHandlerPlugIn
{
    /// <summary>
    /// Gets the recovery percentage of this potion.
    /// </summary>
    protected abstract double RecoveryPercentage { get; }

    /// <inheritdoc />
    public override object CreateDefaultConfig() => new RecoverConsumeHandlerConfiguration
    {
        TotalRecoverPercentage = this.RecoveryPercentage,
        CooldownTime = TimeSpan.FromSeconds(0.5),
    };
}
