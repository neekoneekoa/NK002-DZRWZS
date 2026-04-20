using System;
using System.Collections.Generic;

namespace DiaryApp;

public static class ReminderScheduler
{
    public static DateTime? CalculateNextReminderDate(ReminderSetting setting, DateTime? referenceTime = null)
    {
        if (!setting.StartDate.HasValue || !setting.ReminderTime.HasValue || !setting.IsEnabled || !setting.IsActive)
        {
            return null;
        }

        var reference = referenceTime ?? DateTime.Now;
        var startDate = setting.StartDate.Value.Date;
        var reminderTime = setting.ReminderTime.Value;

        return setting.ReminderType switch
        {
            ReminderType.Once => BuildOccurrence(startDate, reminderTime) > reference
                ? BuildOccurrence(startDate, reminderTime)
                : null,
            ReminderType.Daily => FindNextDailyOccurrence(startDate, reminderTime, reference, Math.Max(1, setting.IntervalDays ?? 1)),
            ReminderType.Weekly => FindNextWeeklyOccurrence(setting, startDate, reminderTime, reference),
            ReminderType.Monthly => FindNextMonthlyOccurrence(setting, startDate, reminderTime, reference),
            ReminderType.Yearly => FindNextYearlyOccurrence(startDate, reminderTime, reference),
            ReminderType.Interval => FindNextDailyOccurrence(startDate, reminderTime, reference, Math.Max(1, setting.IntervalDays ?? 1)),
            _ => null
        };
    }

    public static List<DateTime> CalculateReminderDates(ReminderSetting setting, DateTime fromDate, DateTime toDate)
    {
        var dates = new List<DateTime>();
        if (fromDate > toDate)
        {
            return dates;
        }

        var cursor = fromDate.AddSeconds(-1);
        while (true)
        {
            var next = CalculateNextReminderDate(setting, cursor);
            if (!next.HasValue || next.Value > toDate)
            {
                break;
            }

            dates.Add(next.Value);
            cursor = next.Value;

            if (setting.ReminderType == ReminderType.Once)
            {
                break;
            }
        }

        return dates;
    }

    public static DateTime? GetMonthlyWeekDayDate(int year, int month, int weekNumber, DayOfWeek dayOfWeek)
    {
        try
        {
            if (weekNumber < 1 || weekNumber > 5)
            {
                return null;
            }

            var firstDayOfMonth = new DateTime(year, month, 1);
            var daysUntilTarget = ((int)dayOfWeek - (int)firstDayOfMonth.DayOfWeek + 7) % 7;
            var firstTargetDate = firstDayOfMonth.AddDays(daysUntilTarget);
            var targetDate = firstTargetDate.AddDays((weekNumber - 1) * 7);

            return targetDate.Month == month ? targetDate : null;
        }
        catch
        {
            return null;
        }
    }

    private static DateTime BuildOccurrence(DateTime date, TimeSpan time)
    {
        return date.Date.Add(new TimeSpan(time.Hours, time.Minutes, 0));
    }

    private static DateTime? FindNextDailyOccurrence(DateTime startDate, TimeSpan reminderTime, DateTime reference, int intervalDays)
    {
        var firstOccurrence = BuildOccurrence(startDate, reminderTime);
        if (firstOccurrence > reference)
        {
            return firstOccurrence;
        }

        var elapsedDays = Math.Max(0, (reference.Date - startDate).Days);
        var intervalsPassed = elapsedDays / intervalDays;
        var candidate = BuildOccurrence(startDate.AddDays(intervalsPassed * intervalDays), reminderTime);

        while (candidate <= reference)
        {
            candidate = candidate.AddDays(intervalDays);
        }

        return candidate;
    }

    private static DateTime? FindNextWeeklyOccurrence(ReminderSetting setting, DateTime startDate, TimeSpan reminderTime, DateTime reference)
    {
        var weekdays = setting.WeekDays ?? new List<DayOfWeek>();
        if (weekdays.Count == 0)
        {
            weekdays.Add(startDate.DayOfWeek);
        }

        var cursor = startDate > reference.Date ? startDate : reference.Date.AddDays(-1);
        for (var i = 0; i < 370; i++)
        {
            cursor = cursor.AddDays(1);
            if (cursor.Date < startDate || !weekdays.Contains(cursor.DayOfWeek))
            {
                continue;
            }

            var occurrence = BuildOccurrence(cursor, reminderTime);
            if (occurrence > reference)
            {
                return occurrence;
            }
        }

        return null;
    }

    private static DateTime? FindNextMonthlyOccurrence(ReminderSetting setting, DateTime startDate, TimeSpan reminderTime, DateTime reference)
    {
        for (var monthOffset = 0; monthOffset < 24; monthOffset++)
        {
            var monthDate = new DateTime(startDate.Year, startDate.Month, 1).AddMonths(monthOffset);
            DateTime candidateDate;

            if (setting.MonthlyDayNumber.HasValue && setting.MonthlyDayOfWeek.HasValue)
            {
                var monthlyDate = GetMonthlyWeekDayDate(
                    monthDate.Year,
                    monthDate.Month,
                    setting.MonthlyDayNumber.Value,
                    setting.MonthlyDayOfWeek.Value);

                if (!monthlyDate.HasValue)
                {
                    continue;
                }

                candidateDate = monthlyDate.Value;
            }
            else
            {
                var day = Math.Min(startDate.Day, DateTime.DaysInMonth(monthDate.Year, monthDate.Month));
                candidateDate = new DateTime(monthDate.Year, monthDate.Month, day);
            }

            if (candidateDate.Date < startDate)
            {
                continue;
            }

            var occurrence = BuildOccurrence(candidateDate, reminderTime);
            if (occurrence > reference)
            {
                return occurrence;
            }
        }

        return null;
    }

    private static DateTime? FindNextYearlyOccurrence(DateTime startDate, TimeSpan reminderTime, DateTime reference)
    {
        for (var yearOffset = 0; yearOffset < 10; yearOffset++)
        {
            var year = startDate.Year + yearOffset;
            var day = Math.Min(startDate.Day, DateTime.DaysInMonth(year, startDate.Month));
            var candidate = BuildOccurrence(new DateTime(year, startDate.Month, day), reminderTime);
            if (candidate > reference)
            {
                return candidate;
            }
        }

        return null;
    }
}
