using System;
using FluentAssertions;
using Webinex.Calendar.MicrosoftGraph;

namespace Webinex.Calendar.Tests.RepeatEventCalculatorTests;

public class RepeatEventCalculatorScenario
{
    private Event<object>? _event;
    private OpenPeriod? _range;

    public RepeatEventCalculatorScenario WithWeekdayMatch(
        string timeOfTheDay,
        string duration,
        string tz,
        int? interval,
        DateTimeOffset effectiveStart,
        DateTimeOffset? effectiveEnd,
        params Weekday[] weekdays)
    {
        _event = new RecurrentEvent<object>(
            Guid.NewGuid(),
            MGRecurrence.NewWeekday(
                (int)TimeSpan.Parse(timeOfTheDay).TotalMinutes,
                (int)TimeSpan.Parse(duration).TotalMinutes,
                weekdays,
                tz,
                interval),
            new OpenPeriod(effectiveStart, effectiveEnd), new object());

        return this;
    }

    public RepeatEventCalculatorScenario WithWeekdayMatch(
        string timeOfTheDay,
        string duration,
        string tz,
        params Weekday[] weekdays)
    {
        return WithWeekdayMatch(timeOfTheDay, duration, tz, null, DateTimeOffset.MinValue, null, weekdays);
    }

    public RepeatEventCalculatorScenario WithDayOfMonthMatch(
        string timeOfTheDay,
        string duration,
        int dayOfMonth,
        string? tz = default)
    {
        _event = new RecurrentEvent<object>(
            Guid.NewGuid(),
            MGRecurrence.NewDayOfMonth(
                (int)TimeSpan.Parse(timeOfTheDay).TotalMinutes,
                (int)TimeSpan.Parse(duration).TotalMinutes,
                new DayOfMonth(dayOfMonth),
                tz ?? TimeZoneInfo.Utc.Id),
            new OpenPeriod(DateTimeOffset.MinValue, null), new object());

        return this;
    }

    public RepeatEventCalculatorScenario WithInterval(DateTimeOffset start, string interval, string duration)
    {
        _event = RecurrentEvent<object>.NewInterval(
            start,
            null,
            (int)TimeSpan.Parse(interval).TotalMinutes,
            (int)TimeSpan.Parse(duration).TotalMinutes,
            new object());

        return this;
    }

    public RepeatEventCalculatorScenario WithRange(DateTimeOffset start, DateTimeOffset end)
    {
        _range = new OpenPeriod(start, end);
        return this;
    }

    public Period[] Run()
    {
        if (_event == null || _range == null)
            throw new InvalidOperationException();

        return RepeatEventCalculator.Matches(_event, _range.Start, _range.End!.Value).ToArray();
    }

    public void ToBeEquivalent(params Period[] periods)
    {
        Run().Should().BeEquivalentTo(periods);
    }
}