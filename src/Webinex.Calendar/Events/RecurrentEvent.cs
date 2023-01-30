using Webinex.Calendar.Common;
using Webinex.Calendar.Repeats;

namespace Webinex.Calendar.Events;

public abstract class RecurrentEvent
{
    public Guid Id { get; protected set; }
    public Repeat Repeat { get; protected set; } = null!;
    public OpenPeriod Effective { get; protected set; } = null!;
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

    public static RecurrentEvent<TData> NewMatch(
        int timeOfTheDayUtcMinutes,
        int durationMinutes,
        Weekday[] weekdays,
        DayOfMonth? dayOfMonth,
        DateTimeOffset since,
        DateTimeOffset? until,
        TData data)
    {
        var repeat = Repeat.NewMatch(timeOfTheDayUtcMinutes, durationMinutes, weekdays, dayOfMonth);

        return new RecurrentEvent<TData>
        {
            Id = Guid.NewGuid(),
            Repeat = repeat,
            Effective = new OpenPeriod(since, until).ToUtc(),
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

    internal Event<TData>[] ToEvents(
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
                return new Event<TData>(null, Id, period.Start, period.End, data, cancelled);

            if (state.MoveTo.Start >= to || state.MoveTo.End <= from)
                return null;

            return new Event<TData>(null, Id, state.MoveTo.Start, state.MoveTo.End, data, cancelled);
        }

        return periods.Select(Map)
            .Where(x => x != null)
            .Cast<Event<TData>>()
            .OrderBy(x => x.Start)
            .ToArray();
    }

    private Period[] ToPeriods(DateTimeOffset from, DateTimeOffset to)
    {
        return Repeat.Interval != null
            ? RepeatEventCalculator.Interval(this, from, to)
            : RepeatEventCalculator.Matches(this, from, to);
    }

    public int DurationMinutes() => Repeat.Interval != null ? Repeat.Interval.DurationMinutes :
        Repeat.Match != null ? Repeat.Match.DurationMinutes : throw new InvalidOperationException();
}