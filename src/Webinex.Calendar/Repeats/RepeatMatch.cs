using Webinex.Calendar.Common;

namespace Webinex.Calendar.Repeats;

public class RepeatMatch : ValueObject
{
    protected RepeatMatch()
    {
    }

    public int TimeOfTheDayUtcMinutes { get; protected set; }
    public int DurationMinutes { get; protected set; }
    public int? OvernightDurationMinutes { get; protected set; }
    public int SameDayLastTime { get; protected set; }
    public Weekday[] Weekdays { get; protected set; } = null!;
    public DayOfMonth? DayOfMonth { get; protected set; }

    internal static RepeatMatch New(
        int timeOfTheDayUtcMinutes,
        int durationMinutes,
        Weekday[] weekdays,
        DayOfMonth? dayOfMonth)
    {
        if (!weekdays.Any() && dayOfMonth == null)
            throw new InvalidOperationException($"{nameof(weekdays)} or {nameof(dayOfMonth)} might be defined");

        if (durationMinutes > TimeSpan.FromDays(1).TotalMinutes)
            throw new InvalidOperationException("Duration cannot be more than 1 day");

        if (timeOfTheDayUtcMinutes < 0)
            throw new ArgumentException("Might be >= 0", nameof(timeOfTheDayUtcMinutes));

        if (durationMinutes < 0)
            throw new ArgumentException("Might be >= 0", nameof(durationMinutes));

        var totalEndMinutes = timeOfTheDayUtcMinutes + durationMinutes;
        var isOvernight = totalEndMinutes > TimeSpan.FromDays(1).TotalMinutes;
        int? overnightMinutes = isOvernight ? (int)(totalEndMinutes - TimeSpan.FromDays(1).TotalMinutes) : default(int?);

        return new RepeatMatch
        {
            Weekdays = weekdays,
            DayOfMonth = dayOfMonth,
            DurationMinutes = durationMinutes,
            TimeOfTheDayUtcMinutes = timeOfTheDayUtcMinutes,
            OvernightDurationMinutes = overnightMinutes,
            SameDayLastTime = isOvernight ? (int)TimeSpan.FromDays(1).TotalMinutes : totalEndMinutes,
        };
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        return new object?[] { TimeOfTheDayUtcMinutes, DurationMinutes, Weekdays, DayOfMonth }.Concat(Weekdays);
    }

    public static bool operator ==(RepeatMatch? left, RepeatMatch? right)
    {
        return EqualOperator(left, right);
    }

    public static bool operator !=(RepeatMatch? left, RepeatMatch? right)
    {
        return NotEqualOperator(left, right);
    }

    // internal bool MatchInPeriod(DateTimeOffset start, DateTimeOffset end)
    // {
    //     start = start.ToOffset(TimeSpan.Zero);
    //     end = end.ToOffset(TimeSpan.Zero);
    //
    //     if (start > end)
    //         throw new InvalidOperationException($"{nameof(start)} cannot be greater than {nameof(end)}");
    //
    //     return IsMatchByDayOfMonth(start, end) || IsMatchByWeekday(start, end);
    // }
    //
    // private bool IsMatchByWeekday(DateTimeOffset start, DateTimeOffset end)
    // {
    //     if (!Weekdays.Any())
    //         return false;
    //
    //     var wholeDayWeekdays = DateTimeOffsetUtil.GetUniqueUtcWholeWeekdaysInRange(start, end);
    //
    //     if (Weekdays.Any(weekday => wholeDayWeekdays.Contains(weekday)))
    //         return true;
    //
    //     if (OvernightDurationMinutes.HasValue && Weekdays.Contains(Weekday.From(start.AddDays(-1).DayOfWeek)) &&
    //         start.TotalMinutesFromStartOfTheDayUtc() < OvernightDurationMinutes)
    //         return true;
    //
    //     if (Weekdays.Contains(Weekday.From(start.DayOfWeek)) &&
    //         start.TotalMinutesFromStartOfTheDayUtc() < SameDayLastTime)
    //         return true;
    //
    //     if (Weekdays.Contains(Weekday.From(end.DayOfWeek)) &&
    //         end.TotalMinutesFromStartOfTheDayUtc() < TimeOfTheDayUtcMinutes)
    //         return true;
    //
    //     return false;
    // }
    //
    // private bool IsMatchByDayOfMonth(DateTimeOffset start, DateTimeOffset end)
    // {
    //     if (DayOfMonth == null)
    //         return false;
    //
    //     if (end - start >= TimeSpan.FromDays(31))
    //         return true;
    //
    //     if (OvernightDurationMinutes.HasValue && DayOfMonth.Value == start.AddDays(-1).Day &&
    //         start.TotalMinutesFromStartOfTheDayUtc() < OvernightDurationMinutes)
    //         return true;
    //
    //     if (DayOfMonth.Value == start.Day &&
    //         start.TotalMinutesFromStartOfTheDayUtc() < SameDayLastTime)
    //         return true;
    //
    //     if (DayOfMonth.Value == end.Day &&
    //         end.TotalMinutesFromStartOfTheDayUtc() < TimeOfTheDayUtcMinutes)
    //         return true;
    //
    //     if (DateTimeOffsetUtil.GetUniqueUtcWholeDayOfMonthInRange(start, end).Contains(DayOfMonth.Value))
    //         return true;
    //
    //     return false;
    // }
}