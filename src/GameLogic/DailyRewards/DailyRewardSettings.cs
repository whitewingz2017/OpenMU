// <copyright file="DailyRewardSettings.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.DailyRewards;

using MUnique.OpenMU.DataModel.Composition;

/// <summary>The daily login rewards of the days of a month, configured in the admin panel.</summary>
public sealed class DailyRewardSettings : IValidatableObject
{
    /// <summary>The default zen of a day.</summary>
    private const int DefaultZen = 1_000_000;

    /// <summary>
    /// Gets or sets the rewards per day of the month. A day without an entry, or with an empty entry, has no reward.
    /// Days 29 to 31 are only reached in months which have them.
    /// </summary>
    [Display(Name = nameof(DailyRewardResources.Days), ResourceType = typeof(DailyRewardResources))]
    [MemberOfAggregate]
    [ScaffoldColumn(true)]
    public ICollection<DailyRewardDay> Days { get; set; } = Enumerable.Range(1, DailyRewardCalendar.MaximumDays)
        .Select(day => new DailyRewardDay { Day = day, Zen = DefaultZen })
        .ToList();

    /// <summary>Gets the reward of the day of the month.</summary>
    /// <param name="day">The day of the month, from 1.</param>
    /// <returns>The reward, or <c>null</c> if the day has none.</returns>
    public DailyRewardDay? GetDay(int day) => this.Days.FirstOrDefault(entry => entry.Day == day && entry.HasReward);

    /// <summary>Determines whether every day is within its limits and configured once at most.</summary>
    /// <returns><c>true</c>, if the settings can be used.</returns>
    public bool IsValid() => this.Days is { Count: <= DailyRewardCalendar.MaximumDays }
        && this.Days.All(day => day is not null && day.IsValid)
        && this.Days.Select(day => day.Day).Distinct().Count() == this.Days.Count;

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!this.IsValid())
        {
            yield return new ValidationResult(DailyRewardResources.InvalidCalendar, [nameof(this.Days)]);
        }
    }
}
