// <copyright file="FasterRiddenMountSpeedUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Raises ridden mounts which already got the first TwinMU speed bonus (<see cref="RiddenMountSpeedUpdatePlugIn"/>)
/// to the current <see cref="MovementSpeedConstants.RiddenMountSpeedBonus"/>.
/// </summary>
[PlugIn]
[Display(Name = "Faster Ridden Mount Speed", Description = "Dark Horse, Fenrir and mount companions move one more step faster.")]
[Guid("e4a19b7c-3f62-4d85-b1c0-9a7d2e5f6c33")]
public sealed class FasterRiddenMountSpeedUpdatePlugIn : UpdatePlugInBase
{
    /// <summary>The horse/Fenrir speed given by the first TwinMU release.</summary>
    private const float PreviousHorseOrFenrirMovementSpeed = MovementSpeedConstants.HorseOrFenrirMovementSpeed - MovementSpeedConstants.RiddenMountSpeedBonus + MovementSpeedConstants.FirstRiddenMountSpeedBonus;

    /// <summary>The upgraded Fenrir speed given by the first TwinMU release.</summary>
    private const float PreviousUpgradedFenrirMovementSpeed = MovementSpeedConstants.UpgradedFenrirMovementSpeed - MovementSpeedConstants.RiddenMountSpeedBonus + MovementSpeedConstants.FirstRiddenMountSpeedBonus;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override string Name => "Faster Ridden Mount Speed";

    /// <inheritdoc />
    public override string Description => "Dark Horse, Fenrir (also upgraded and custom models) and mount companions move one more step faster. Requires the matching TwinMU client.";

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 11, 2, 0, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    public override bool IsMandatory => false;

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        // Only values still at the first TwinMU speed move; administrator-chosen speeds stay untouched.
        foreach (var powerUp in gameConfiguration.Items.SelectMany(i => i.BasePowerUpAttributes)
                     .Where(p => IsSpeed(p.TargetAttribute) && p.AggregateType == AggregateType.Maximum
                         && p.BaseValue == PreviousHorseOrFenrirMovementSpeed))
        {
            powerUp.BaseValue = MovementSpeedConstants.HorseOrFenrirMovementSpeed;
        }

        foreach (var boost in gameConfiguration.ItemOptionCombinationBonuses
                     .Where(b => IsSpeed(b.Bonus?.TargetAttribute))
                     .Select(b => b.Bonus!.Boost?.ConstantValue)
                     .Where(v => v is { AggregateType: AggregateType.Maximum } && v.Value == PreviousUpgradedFenrirMovementSpeed))
        {
            boost!.Value = MovementSpeedConstants.UpgradedFenrirMovementSpeed;
        }

        return ValueTask.CompletedTask;
    }

    private static bool IsSpeed(AttributeDefinition? attribute)
        => attribute?.Id == Stats.MovementSpeed.Id || attribute?.Id == Stats.MovementSpeedUnderwater.Id;
}
