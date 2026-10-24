using System;
using System.Linq;
using FluentAssertions;
using NodaTime;
using Webinex.Calendar.Calculators;
using Webinex.Calendar.Extensions;

namespace Webinex.Calendar.Tests.RepeatEventCalculatorTests;

public class RepeatEventCalculatorScenario
{
    private Event<TestEventData>? _event;
    private OpenPeriod<DateTimeOffset>? _range;

    public RepeatEventCalculatorScenario WithWeekdayMatch(
        string timeOfTheDay,
        string duration,
        string tz,
        int? interval,
        DateTimeOffset effectiveStart,
        DateTimeOffset? effectiveEnd,
        params DayOfWeek[] weekdays)
    {
        var start = Start(timeOfTheDay, tz, effectiveStart);
        _event = Event.Factory.MGWeekly(
            Period.New(start, start.Add(TimeSpan.Parse(duration))),
            tz,
            new TestEventData(),
            weekdays,
            interval: interval ?? 1,
            until: effectiveEnd.HasValue ? ToLocalDate(effectiveEnd.Value.AddTicks(-1), tz) : null);

        return this;
    }

    public RepeatEventCalculatorScenario WithWeekdayMatch(
        string timeOfTheDay,
        string duration,
        string tz,
        params DayOfWeek[] weekdays)
    {
        return WithWeekdayMatch(timeOfTheDay, duration, tz, null, DateTimeOffset.MinValue, null, weekdays);
    }

    public RepeatEventCalculatorScenario WithDayOfMonthMatch(
        string timeOfTheDay,
        string duration,
        int dayOfMonth,
        string? tz = default)
    {
        tz ??= TimeZoneInfo.Utc.Id;
        var start = Start(timeOfTheDay, tz);
        _event = Event.Factory.MGAbsoluteMonthly(
            Period.New(start, start.Add(TimeSpan.Parse(duration))),
            tz,
            new TestEventData(),
            dayOfMonth);

        return this;
    }

    public RepeatEventCalculatorScenario WithInterval(DateTimeOffset start, string interval, string duration)
    {
        var intervalDays = TimeSpan.Parse(interval).TotalDays;
        if (intervalDays < 1 || intervalDays != Math.Truncate(intervalDays))
            throw new ArgumentOutOfRangeException(nameof(interval), "Only whole-day intervals are supported.");

        _event = Event.Factory.MGDaily(
            Period.New(start, start.Add(TimeSpan.Parse(duration))),
            TimeZoneInfo.Utc.Id,
            new TestEventData(),
            interval: (int)intervalDays);

        return this;
    }

    public RepeatEventCalculatorScenario WithRange(DateTimeOffset start, DateTimeOffset end)
    {
        _range = new OpenPeriod<DateTimeOffset>(start, end);
        return this;
    }

    public Period<DateTimeOffset>[] Run()
    {
        if (_event == null || _range == null)
            throw new InvalidOperationException();

        return RecurrenceCalculator.Occurrences(_event, _range).ToArray();
    }

    public void ToBeEquivalent(params Period<DateTimeOffset>[] periods)
    {
        Run().Should().BeEquivalentTo(periods);
    }

    private DateTimeOffset Start(string timeOfTheDay, string tz, DateTimeOffset? effectiveStart = null)
    {
        var zone = DateTimeZoneProviders.Tzdb[tz];
        var date = Instant.FromDateTimeOffset(effectiveStart is { } value && value != DateTimeOffset.MinValue
            ? value
            : _range?.Start ?? throw new InvalidOperationException("Set the range before creating the event."))
            .InZone(zone).Date;

        // Unbounded legacy scenarios need an anchor before the range, including overnight occurrences.
        if (effectiveStart == null || effectiveStart == DateTimeOffset.MinValue)
            date = new LocalDate(date.Year, date.Month, 1).PlusMonths(-1);

        return date.At(LocalTime.FromTicksSinceMidnight(TimeSpan.Parse(timeOfTheDay).Ticks))
            .InZoneLeniently(zone).ToDateTimeOffset();
    }

    private static DateOnly ToLocalDate(DateTimeOffset value, string tz)
    {
        var date = Instant.FromDateTimeOffset(value).InZone(DateTimeZoneProviders.Tzdb[tz]).Date;
        return new DateOnly(date.Year, date.Month, date.Day);
    }
}
