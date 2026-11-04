// <copyright file="ExcellentCombinationConfiguration.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions.Items;

/// <summary>Admin settings for excellent-option combination.</summary>
public sealed class ExcellentCombinationConfiguration : IValidatableObject
{
    /// <summary>Gets or sets the success percentage without cores.</summary>
    [Range(0, 100)]
    [Display(Name = nameof(ExcellentCombinationResources.BaseRate), ResourceType = typeof(ExcellentCombinationResources))]
    public int BaseRate { get; set; } = 35;

    /// <summary>Gets or sets percentage points added by each core.</summary>
    [Range(1, 100)]
    [Display(Name = nameof(ExcellentCombinationResources.BonusPerCore), ResourceType = typeof(ExcellentCombinationResources))]
    public int BonusPerCore { get; set; } = 10;

    /// <summary>Gets or sets the success percentage cap.</summary>
    [Range(0, 100)]
    [Display(Name = nameof(ExcellentCombinationResources.MaximumRate), ResourceType = typeof(ExcellentCombinationResources))]
    public int MaximumRate { get; set; } = 75;

    /// <summary>Gets or sets Zen consumed by a completed attempt.</summary>
    [Range(0, int.MaxValue)]
    [Display(Name = nameof(ExcellentCombinationResources.ZenCost), ResourceType = typeof(ExcellentCombinationResources))]
    public int ZenCost { get; set; } = 1_000_000;

    /// <summary>Gets a value indicating whether the settings can safely be used by the server.</summary>
    internal bool IsValid => this.BaseRate is >= 0 and <= 100 && this.MaximumRate >= this.BaseRate
        && this.MaximumRate <= 100 && this.BonusPerCore is >= 1 and <= 100 && this.ZenCost >= 0;

    /// <summary>Gets the greatest useful number of cores.</summary>
    internal int MaximumCores => this.IsValid ? (this.MaximumRate - this.BaseRate + this.BonusPerCore - 1) / this.BonusPerCore : 0;

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!this.IsValid)
        {
            yield return new ValidationResult(ExcellentCombinationResources.Message("Disabled"), [nameof(this.BaseRate), nameof(this.MaximumRate), nameof(this.BonusPerCore), nameof(this.ZenCost)]);
        }
    }

    /// <summary>Calculates a capped success percentage for a validated core quantity.</summary>
    /// <param name="cores">The selected core quantity.</param>
    /// <returns>The success percentage.</returns>
    internal int Rate(int cores) => Math.Min(this.MaximumRate, this.BaseRate + (cores * this.BonusPerCore));
}
