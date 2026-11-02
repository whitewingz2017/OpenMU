// <copyright file="RiddenMountSpeedUpdatePlugIn.cs" company="MUnique">
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
/// Raises the riding speed of the Dark Horse, Fenrir (also upgraded and imported Fenrir models) and
/// gives mount companions the same speed, matching the custom client's ridden mount speed.
/// </summary>
[PlugIn]
[Display(Name = "Ridden Mount Speed", Description = "Ridden mounts and mount companions move one step faster.")]
[Guid("6b0f4a1e-2d9c-4c8e-9f3a-71c25e0d8b44")]
public sealed class RiddenMountSpeedUpdatePlugIn : UpdatePlugInBase
{
    /// <summary>The original client's horse/Fenrir speed, which existing configurations still carry.</summary>
    private const float PreviousHorseOrFenrirMovementSpeed = MovementSpeedConstants.HorseOrFenrirMovementSpeed - MovementSpeedConstants.RiddenMountSpeedBonus;

    /// <summary>The original client's upgraded Fenrir speed, which existing configurations still carry.</summary>
    private const float PreviousUpgradedFenrirMovementSpeed = MovementSpeedConstants.UpgradedFenrirMovementSpeed - MovementSpeedConstants.RiddenMountSpeedBonus;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override string Name => "Ridden Mount Speed";

    /// <inheritdoc />
    public override string Description => "Dark Horse and Fenrir move faster; mount companions in the Muun main slot ride at the same speed. Requires the matching TwinMU client.";

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    public override bool IsMandatory => false;

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        AttributeDefinition[] speedAttributes = [Stats.MovementSpeed, Stats.MovementSpeedUnderwater];
        bool IsSpeed(AttributeDefinition? attribute) => speedAttributes.Any(a => a.Id == attribute?.Id);

        // Only values still at the original speed move; administrator-chosen speeds stay untouched.
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

        var mountTypes = VersionSeasonSix.Items.ImportedMuunItems.MountTypes();
        foreach (var mount in gameConfiguration.Items.Where(i => mountTypes.Contains((i.Group * 512) + i.Number)))
        {
            foreach (var attribute in speedAttributes.Where(a => mount.BasePowerUpAttributes.All(p => p.TargetAttribute?.Id != a.Id)))
            {
                var powerUp = context.CreateNew<ItemBasePowerUpDefinition>();
                powerUp.TargetAttribute = attribute.GetPersistent(gameConfiguration);
                powerUp.BaseValue = MovementSpeedConstants.HorseOrFenrirMovementSpeed;
                powerUp.AggregateType = AggregateType.Maximum;
                mount.BasePowerUpAttributes.Add(powerUp);
            }
        }

        return ValueTask.CompletedTask;
    }
}
