﻿using Webinex.Calendar.Common;
using Webinex.Calendar.Repeats;
using Webinex.Calendar.Repeats.Calculators;

namespace Webinex.Calendar.Events;

public abstract class RecurrentEvent : IEvent
{
    public Guid Id { get; protected set; }
    public Repeat Repeat { get; protected set; } = null!;
    public OpenPeriod Effective { get; protected set; } = null!;
    EventType IEvent.Type => EventType.RecurrentEvent;
}

public class RecurrentEvent<TData> : RecurrentEvent
    where TData : class
{
    protected RecurrentEvent()
    {
    }

    public RecurrentEvent(Guid id, Repeat repeat, OpenPeriod effective, TData data)
    {
        Id = id;
        Repeat = repeat;
        Effective = effective;
        Data = data;
    }

    public TData Data { get; protected set; } = null!;

    public static RecurrentEvent<TData> NewWeekday(
        DateTimeOffset start,
        DateTimeOffset? end,
        int timeOfTheDayUtcMinutes,
        int durationMinutes,
        Weekday[] weekdays,
        string timeZone,
        TData data)
    {
        var repeat = Repeat.NewWeekday(timeOfTheDayUtcMinutes, durationMinutes, weekdays, timeZone);

        return new RecurrentEvent<TData>
        {
            Id = Guid.NewGuid(),
            Repeat = repeat,
            Effective = new OpenPeriod(start, end).ToUtc(),
            Data = data,
        };
    }

    public static RecurrentEvent<TData> NewInterval(
        DateTimeOffset start,
        DateTimeOffset? end,
        int intervalMinutes,
        int durationMinutes,
        TData data)
    {
        var repeat = Repeat.NewInterval(start, end, intervalMinutes, durationMinutes);

        return new RecurrentEvent<TData>
        {
            Id = Guid.NewGuid(),
            Repeat = repeat,
            Effective = new OpenPeriod(start, end).ToUtc(),
            Data = data,
        };
    }

    public static RecurrentEvent<TData> NewDayOfMonth(
        DateTimeOffset start,
        DateTimeOffset? end,
        int timeOfTheDayUtcMinutes,
        int durationMinutes,
        DayOfMonth dayOfMonth,
        string timeZone,
        TData data)
    {
        return new RecurrentEvent<TData>
        {
            Id = Guid.NewGuid(),
            Effective = new OpenPeriod(start, end).ToUtc(),
            Data = data,
            Repeat = Repeat.NewDayOfMonth(timeOfTheDayUtcMinutes, durationMinutes, dayOfMonth, timeZone),
        };
    }

    public Event<TData>[] ToEvents(
        DateTimeOffset from,
        DateTimeOffset to,
        IEnumerable<RecurrentEventState<TData>> eventStates)
    {
        from = from.ToUtc();
        to = to.ToUtc();

        var periods = ToPeriods(from, to);
        return ConvertToEvents(from, to, periods, eventStates.ToArray());
    }

    private Event<TData>[] ConvertToEvents(
        DateTimeOffset from,
        DateTimeOffset to,
        Period[] periods,
        RecurrentEventState<TData>[] states)
    {
        var movedToRangePeriods = states
            .Where(x => x.MoveTo != null && !periods.Contains(x.Period))
            .Select(x => x.MoveTo!)
            .ToArray();

        periods = periods.Concat(movedToRangePeriods).ToArray();

        Event<TData>? Map(Period period)
        {
            var state = states.FirstOrDefault(x => x.Period.Start == period.Start);
            var cancelled = state?.Cancelled ?? false;

            var data = state?.Data ?? Data;
            if (state?.MoveTo == null)
                return new Event<TData>(null, Id, period, data, cancelled, null);

            if (state.MoveTo.Start >= to || state.MoveTo.End <= from)
                return null;

            return new Event<TData>(null, Id, state.MoveTo, data, cancelled, state.Period);
        }

        return periods.Select(Map)
            .Where(x => x != null)
            .Cast<Event<TData>>()
            .OrderBy(x => x.Start)
            .ToArray();
    }

    /// <summary>
    /// Computes all occurrences periods for this <see cref="RecurrentEvent"/>.
    /// Also returns all partially aligned occurrences (occurrence.start &lt; from and occurrence.end &gt; from)
    /// <br/>
    /// INFINITELY returns events if <see cref="RecurrentEvent"/> has no end effective date
    /// </summary>
    /// <param name="from">Start of period</param>
    public IEnumerable<Period> ToPeriods(DateTimeOffset from)
    {
        return RepeatEventCalculator.Matches(this, from, null);
    }

    /// <summary>
    /// Computes all occurrences periods for this <see cref="RecurrentEvent"/>.
    /// Also returns all partially aligned occurrences
    /// </summary>
    /// <param name="from">Start of period</param>
    /// <param name="to">End of period</param>
    public Period[] ToPeriods(DateTimeOffset from, DateTimeOffset to)
    {
        return RepeatEventCalculator.Matches(this, from, to).ToArray();
    }

    public Period? LastPeriod(DateTimeOffset until)
    {
        if (until <= Effective.Start)
        {
            return null;
        }
        
        return RepeatEventCalculator.Matches(this, Effective.Start, until).MaxBy(x => x.Start);
    }

    public Period? MatchPeriod(DateTimeOffset eventStart)
    {
        return RepeatEventCalculator.Matches(this, eventStart, eventStart.AddMinutes(1))
            .FirstOrDefault(x => x.Start == eventStart);
    }

    public int DurationMinutes()
    {
        return Repeat.Interval != null ? Repeat.Interval.DurationMinutes :
            Repeat.Weekday != null ? Repeat.Weekday.DurationMinutes : throw new InvalidOperationException();
    }
}