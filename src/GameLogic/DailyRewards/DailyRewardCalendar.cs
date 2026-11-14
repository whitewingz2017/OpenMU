// <copyright file="DailyRewardCalendar.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.DailyRewards;

/// <summary>
/// The calendar month of the daily login rewards. Day N of the month grants reward N; a day which passed unclaimed is lost,
/// and the claimed days start over with each month. Days follow the server's local time.
/// </summary>
public static class DailyRewardCalendar
{
    /// <summary>The maximum number of days of a month.</summary>
    public const int MaximumDays = 31;

    /// <summary>Gets the current month, day and length of the month.</summary>
    /// <param name="utcNow">The current time in UTC.</param>
    /// <param name="timeZone">The time zone of the server; <c>null</c> for its local time zone.</param>
    /// <returns>The month as year times 100 plus month, the day of the month and the number of days of the month.</returns>
    public static (int Month, int Day, int DaysInMonth) Today(DateTime utcNow, TimeZoneInfo? timeZone = null)
    {
        var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utcNow, DateTimeKind.Utc), timeZone ?? TimeZoneInfo.Local);
        return ((local.Year * 100) + local.Month, local.Day, DateTime.DaysInMonth(local.Year, local.Month));
    }

    /// <summary>Gets the days of the month whose reward the account claimed.</summary>
    /// <param name="account">The account.</param>
    /// <param name="month">The month as year times 100 plus month.</param>
    /// <returns>The claimed days; bit 0 is the first day.</returns>
    public static int ClaimedDays(Account account, int month) => account.DailyRewardMonth == month ? account.DailyRewardClaimedDays : 0;

    /// <summary>Determines whether the day is part of the claimed days.</summary>
    /// <param name="claimedDays">The claimed days; bit 0 is the first day.</param>
    /// <param name="day">The day of the month, from 1.</param>
    /// <returns><c>true</c>, if the day was claimed.</returns>
    public static bool IsClaimed(int claimedDays, int day) => day is >= 1 and <= MaximumDays && (claimedDays & DayBit(day)) != 0;

    /// <summary>Gets the bit of the day in the claimed days.</summary>
    /// <param name="day">The day of the month, from 1.</param>
    /// <returns>The bit of the day.</returns>
    public static int DayBit(int day) => 1 << (day - 1);
}
