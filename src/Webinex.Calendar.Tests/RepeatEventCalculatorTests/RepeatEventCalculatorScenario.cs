﻿using System;
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
            Repeat.NewWeekday(
                (int)TimeSpan.Parse(timeOfTheDay).TotalMinutes,
                (int)TimeSpan.Parse(duration).TotalMinutes,
                weekdays),
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
            Repeat.NewDayOfMonth(
                (int)TimeSpan.Parse(timeOfTheDay).TotalMinutes,
                (int)TimeSpan.Parse(duration).TotalMinutes,
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

        return RepeatEventCalculator.Matches(_event, _range.Start, _range.End!.Value);
    }

    public void ToBeEquivalent(params Period[] periods)
    {
        Run().Should().BeEquivalentTo(periods);
    }
}