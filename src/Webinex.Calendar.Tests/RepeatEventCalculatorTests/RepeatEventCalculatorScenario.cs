using System;
using System.Linq;
using FluentAssertions;
using Webinex.Calendar.Calculators;
using Webinex.Calendar.Extensions;

namespace Webinex.Calendar.Tests.RepeatEventCalculatorTests;

public class RepeatEventCalculatorScenario
{
    private Event<None>? _event;
    private OpenPeriod<DateTimeOffset>? _range;

    public RepeatEventCalculatorScenario WithWeekly(
        Period<DateTimeOffset> period,
        string tz,
        int? interval,
        params DayOfWeek[] daysOfWeek)
    {
        _event = Event.Factory.MGWeekly(
            period,
            tz,
            new None(),
            daysOfWeek,
            interval ?? 1);

        return this;
    }

    public RepeatEventCalculatorScenario WithWeekly(
        Period<DateTimeOffset> period,
        string tz,
        int? interval,
        DateOnly? until,
        params DayOfWeek[] daysOfWeek)
    {
        _event = Event.Factory.MGWeekly(
            period,
            tz,
            new None(),
            daysOfWeek,
            interval ?? 1,
            until);

        return this;
    }

    public RepeatEventCalculatorScenario WithAbsoluteMonthly(
        Period<DateTimeOffset> period,
        string tz,
        int dayOfMonth,
        int? interval = null,
        DateOnly? until = null)
    {
        _event = Event.Factory.MGAbsoluteMonthly(
            period,
            tz,
            new None(),
            dayOfMonth,
            until,
            interval ?? 1);

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
}
