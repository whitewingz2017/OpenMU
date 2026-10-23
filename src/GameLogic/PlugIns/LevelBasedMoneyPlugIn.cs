// <copyright file="LevelBasedMoneyPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Keeps monster Zen independent of experience rates for a predictable economy.
/// </summary>
[PlugIn]
[Guid("4DA2C2DD-114E-47A0-8D36-D301A1AB76FC")]
[Display(Name = nameof(PlugInResources.LevelBasedMoneyPlugIn_Name), Description = nameof(PlugInResources.LevelBasedMoneyPlugIn_Description), ResourceType = typeof(PlugInResources))]
public class LevelBasedMoneyPlugIn : IMonsterMoneyAmountPlugIn, ISupportCustomConfiguration<LevelBasedMoneyPlugInConfiguration>, ISupportDefaultCustomConfiguration
{
    /// <inheritdoc />
    public LevelBasedMoneyPlugInConfiguration? Configuration { get; set; }

    /// <inheritdoc />
    public ValueTask CalculateMoneyAmountAsync(MonsterMoneyAmountArgs args)
    {
        var configuration = this.Configuration ??= new();
        if (configuration.BaseZen < 0
            || configuration.ZenPerMonsterLevel < 0
            || configuration.MaximumRandomZen < 0)
        {
            throw new InvalidOperationException("Monster Zen settings must be non-negative.");
        }

        var level = Math.Clamp((int)args.Monster[Stats.Level], 0, ushort.MaxValue);

        // Keep both the payout and the randomizer's exclusive upper bound within signed integer limits.
        var minimum = (int)Math.Min(int.MaxValue - 1L, configuration.BaseZen + ((long)level * configuration.ZenPerMonsterLevel));
        var maximum = (int)Math.Min(int.MaxValue - 1L, (long)minimum + configuration.MaximumRandomZen);
        args.Amount = (uint)Rand.NextInt(minimum, maximum + 1);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public object CreateDefaultConfig() => new LevelBasedMoneyPlugInConfiguration();
}
