using System;
using FluentAssertions;
using Webinex.Calendar.Common;
using Webinex.Calendar.Events;
using Webinex.Calendar.Repeats;

namespace Webinex.Calendar.Tests.RepeatEventCalculatorTests;

public class RepeatEventCalculatorScenario
{
    private RecurrentEvent<object>? _event;
    private OpenPeriod? _range;

    public RepeatEventCalculatorScenario WithWeekdayMatch(
        string timeOfTheDay,
        string duration,
        params Weekday[] weekdays)
    {
        _event = new RecurrentEvent<object>(
            Guid.NewGuid(),
            Repeat.NewMatch(
                (int)TimeSpan.Parse(timeOfTheDay).TotalMinutes,
                (int)TimeSpan.Parse(duration).TotalMinutes,
                weekdays,
                null),
            new OpenPeriod(DateTimeOffset.MinValue, null), new object());

        return this;
    }

    public RepeatEventCalculatorScenario WithDayOfMonthMatch(
        string timeOfTheDay,
        string duration,
        int dayOfMonth)
    {
        _event = new RecurrentEvent<object>(
            Guid.NewGuid(),
            Repeat.NewMatch(
                (int)TimeSpan.Parse(timeOfTheDay).TotalMinutes,
                (int)TimeSpan.Parse(duration).TotalMinutes,
                Array.Empty<Weekday>(),
                new DayOfMonth(dayOfMonth)),
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

        return _event.Repeat.Interval != null
            ? RepeatEventCalculator.Interval(_event, _range.Start, _range.End!.Value)
            : RepeatEventCalculator.Matches(_event, _range.Start, _range.End!.Value);
    }

    public void ToBeEquivalent(params Period[] periods)
    {
        Run().Should().BeEquivalentTo(periods);
    }
}