using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Webinex.Calendar.Common;
using Webinex.Calendar.Events;
using Webinex.Calendar.Repeats;

namespace Webinex.Calendar.DataAccess;

public static class EventRow
{
    public static EventRow<TData> From<TData>(OneTimeEvent<TData> @event)
        where TData : class, ICloneable
    {
        return EventRow<TData>.NewEvent(@event.Id, @event.Period, @event.Data);
    }
}

public class EventRow<TData>
    where TData : class, ICloneable
{
    protected EventRow()
    {
    }

    internal EventRow(
        Guid id,
        OpenPeriod effective,
        EventType type,
        EventRowRepeat? repeat,
        Guid? recurrentEventId,
        TData data,
        Period? moveTo,
        bool cancelled)
    {
        Id = id;
        Effective = new OpenPeriodMinutesSince1990(effective);
        Type = type;
        Repeat = repeat;
        RecurrentEventId = recurrentEventId;
        Data = data;
        MoveTo = moveTo;
        Cancelled = cancelled;
    }

    public Guid Id { get; protected set; }
    public OpenPeriodMinutesSince1990 Effective { get; protected internal set; } = null!;
    public EventType Type { get; protected set; }
    public Guid? RecurrentEventId { get; protected set; }
    public EventRowRepeat? Repeat { get; protected set; }
    public bool Cancelled { get; protected set; }
    public TData Data { get; protected set; } = null!;
    public Period? MoveTo { get; protected set; }

    [MemberNotNullWhen(true, nameof(RecurrentEventId))]
    internal bool IsRecurrentEventState() => Type == EventType.RecurrentEventState;

    internal EventRow<TData>? RecurrentEvent { get; set; }

    internal EventRowId GetEventRowId() => new EventRowId(Type, Id, RecurrentEventId, Effective.ToOpenPeriod().Start);

    internal void SetOneTimeEventData(TData data)
    {
        if (Type != EventType.OneTimeEvent)
            throw new InvalidOperationException("Unable to update event data not for one time event");

        Data = data;
    }

    internal void SetRecurrentEventData(TData data)
    {
        if (Type != EventType.RecurrentEventState)
            throw new InvalidOperationException("Unable to update event data not for recurrent event state");

        Data = data;
    }

    internal void Move(Period moveTo)
    {
        if (Type != EventType.RecurrentEventState)
            throw new InvalidOperationException("Unable to resize not recurrent event state");

        MoveTo = moveTo;
    }

    internal void Cancel()
    {
        if (Type != EventType.RecurrentEventState && Type != EventType.OneTimeEvent)
            throw new InvalidOperationException("Only recurrent event state and one time event can be cancelled");

        Cancelled = true;
    }

    internal static EventRow<TData> NewEvent(Period period, TData data)
    {
        return NewEvent(Guid.NewGuid(), period, data);
    }

    internal static EventRow<TData> NewEvent(Guid id, Period period, TData data)
    {
        return new EventRow<TData>
        {
            Id = id,
            Type = EventType.OneTimeEvent,
            Data = data,
            Effective = new OpenPeriodMinutesSince1990(period.Start.TotalMinutesSince1990(),
                period.End.TotalMinutesSince1990()),
        };
    }

    public static EventRow<TData> NewRecurrentEvent(Repeat repeat, OpenPeriod effective, TData data)
    {
        return NewRecurrentEvent(Guid.NewGuid(), repeat, effective, data);
    }

    internal static EventRow<TData> NewRecurrentEvent(Guid id, Repeat repeat, OpenPeriod effective, TData data)
    {
        return new EventRow<TData>
        {
            Id = id,
            Repeat = EventRowRepeat.From(repeat),
            Data = data,
            Type = EventType.RecurrentEvent,
            Effective = new OpenPeriodMinutesSince1990(effective),
        };
    }

    public static EventRow<TData> NewRecurrentEventState(
        Guid recurrentEventId,
        DateTimeOffset start,
        DateTimeOffset end,
        TData data,
        Period? moveTo,
        bool cancelled)
    {
        return NewRecurrentEventState(Guid.NewGuid(), recurrentEventId, start, end, data, moveTo, cancelled);
    }

    internal static EventRow<TData> NewRecurrentEventState(
        Guid id,
        Guid recurrentEventId,
        DateTimeOffset start,
        DateTimeOffset end,
        TData data,
        Period? moveTo,
        bool cancelled)
    {
        return new EventRow<TData>
        {
            Id = id,
            Data = data,
            Type = EventType.RecurrentEventState,
            Effective = new OpenPeriodMinutesSince1990(start, end),
            RecurrentEventId = recurrentEventId,
            MoveTo = moveTo,
            Cancelled = cancelled,
        };
    }

    public RecurrentEvent<TData> ToRecurrentEvent()
    {
        AssertConvert(EventType.RecurrentEvent, nameof(RecurrentEvent<object>));
        return new RecurrentEvent<TData>(Id, Repeat!.ToModel(Effective), Effective.ToOpenPeriod(), Data);
    }

    public OneTimeEvent<TData> ToOneTimeEvent()
    {
        AssertConvert(EventType.OneTimeEvent, nameof(OneTimeEvent<object>));
        return OneTimeEvent<TData>.New(Id, Effective.ToPeriod(), Data);
    }

    public RecurrentEventState<TData> ToRecurrentEventState()
    {
        AssertConvert(EventType.RecurrentEventState, nameof(RecurrentEventState<object>));
        return RecurrentEventState<TData>.New(RecurrentEventId!.Value, Effective.ToPeriod(), MoveTo, Cancelled, Data);
    }

    private void AssertConvert(EventType expectedType, string targetType)
    {
        if (Type != expectedType)
        {
            throw new InvalidOperationException(
                $"Unable to convert event of type {Type:G} to {targetType}");
        }
    }

    internal static Expression<Func<EventRow<TData>, bool>> InPeriodExpression(Period period)
    {
        var min = period.Start.TotalMinutesSince1990();
        var max = period.End.TotalMinutesSince1990();

        return x => (x.Effective.Start < max && (!x.Effective.End.HasValue || x.Effective.End.Value > min))
                    || (x.MoveTo!.Start < period.End && x.MoveTo.End > period.Start);
    }

    internal bool InPeriod(Period period)
    {
        var min = period.Start.TotalMinutesSince1990();
        var max = period.End.TotalMinutesSince1990();

        return (Effective.Start < max && (!Effective.End.HasValue || Effective.End.Value > min))
               || (MoveTo != null && MoveTo!.Start < period.End && MoveTo.End > period.Start);
    }

    internal static Expression<Func<EventRow<TData>, bool>> WeekdaySelector(Weekday weekday) =>
        WeekdaySelectorMap[weekday];

    private static IReadOnlyDictionary<Weekday, Expression<Func<EventRow<TData>, bool>>> WeekdaySelectorMap =
        new Dictionary<Weekday, Expression<Func<EventRow<TData>, bool>>>
        {
            [Weekday.Monday] = x => x.Repeat!.Monday!.Value,
            [Weekday.Tuesday] = x => x.Repeat!.Tuesday!.Value,
            [Weekday.Wednesday] = x => x.Repeat!.Wednesday!.Value,
            [Weekday.Thursday] = x => x.Repeat!.Thursday!.Value,
            [Weekday.Friday] = x => x.Repeat!.Friday!.Value,
            [Weekday.Saturday] = x => x.Repeat!.Saturday!.Value,
            [Weekday.Sunday] = x => x.Repeat!.Sunday!.Value
        };
}