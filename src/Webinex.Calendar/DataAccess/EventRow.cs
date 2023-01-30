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
        return EventRow<TData>.NewEvent(@event.Id, @event.Start, @event.End, @event.Data);
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
        EventRowType type,
        EventRowRepeat? repeat,
        Guid? recurrentEventId,
        TData data,
        Period? moveTo,
        bool cancelled)
    {
        Id = id;
        Effective = effective;
        Type = type;
        Repeat = repeat;
        RecurrentEventId = recurrentEventId;
        Data = data;
        MoveTo = moveTo;
        Cancelled = cancelled;
    }

    public Guid Id { get; protected set; }
    public OpenPeriod Effective { get; protected internal set; } = null!;
    public EventRowType Type { get; protected set; }
    public Guid? RecurrentEventId { get; protected set; }
    public EventRowRepeat? Repeat { get; protected set; }
    public bool Cancelled { get; protected set; }
    public TData Data { get; protected set; } = null!;
    public Period? MoveTo { get; protected set; }

    internal EventRow<TData>? RecurrentEvent { get; } = null!;

    public void SetOneTimeEventData(TData data)
    {
        if (Type != EventRowType.OneTimeEvent)
            throw new InvalidOperationException("Unable to update event data not for one time event");

        Data = data;
    }

    public void SetRecurrentEventData(TData data)
    {
        if (Type != EventRowType.RecurrentEventState)
            throw new InvalidOperationException("Unable to update event data not for recurrent event state");

        Data = data;
    }

    public void Move(Period moveTo)
    {
        if (Type != EventRowType.RecurrentEventState)
            throw new InvalidOperationException("Unable to resize not recurrent event state");

        MoveTo = moveTo;
    }

    public void Cancel()
    {
        if (Type != EventRowType.RecurrentEventState)
            throw new InvalidOperationException("Unable to cancel not recurrent event state");

        Cancelled = true;
    }

    internal static EventRow<TData> NewEvent(DateTimeOffset start, DateTimeOffset end, TData data)
    {
        return NewEvent(Guid.NewGuid(), start, end, data);
    }

    internal static EventRow<TData> NewEvent(Guid id, DateTimeOffset start, DateTimeOffset end, TData data)
    {
        return new EventRow<TData>
        {
            Id = id,
            Type = EventRowType.OneTimeEvent,
            Data = data,
            Effective = new OpenPeriod(start, end),
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
            Type = EventRowType.RecurrentEvent,
            Effective = effective,
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
            Type = EventRowType.RecurrentEventState,
            Effective = new OpenPeriod(start, end),
            RecurrentEventId = recurrentEventId,
            MoveTo = moveTo,
            Cancelled = cancelled,
        };
    }

    public RecurrentEvent<TData> ToRecurrentEvent()
    {
        AssertConvert(EventRowType.RecurrentEvent, nameof(RecurrentEvent<object>));
        return new RecurrentEvent<TData>(Id, Repeat!.ToModel(), Effective, Data!);
    }

    public OneTimeEvent<TData> ToOneTimeEvent()
    {
        AssertConvert(EventRowType.OneTimeEvent, nameof(OneTimeEvent<object>));
        return OneTimeEvent<TData>.New(Id, Effective.Start, Effective.End!.Value, Data!);
    }

    public RecurrentEventState<TData> ToRecurrentEventState()
    {
        AssertConvert(EventRowType.RecurrentEventState, nameof(RecurrentEventState<object>));
        return RecurrentEventState<TData>.New(RecurrentEventId!.Value, Effective.ToPeriod(), MoveTo, Cancelled, Data);
    }

    private void AssertConvert(EventRowType expectedRowType, string targetType)
    {
        if (Type != expectedRowType)
            throw new InvalidOperationException(
                $"Unable to convert event of type {Type:G} to {targetType}");
    }

    internal static Expression<Func<EventRow<TData>, object>> Selector(Weekday weekday)
    {
        if (weekday == Weekday.Monday)
            return x => x.Repeat!.Match!.Monday;

        if (weekday == Weekday.Tuesday)
            return x => x.Repeat!.Match!.Tuesday;

        if (weekday == Weekday.Wednesday)
            return x => x.Repeat!.Match!.Wednesday;
        
        if (weekday == Weekday.Thursday)
            return x => x.Repeat!.Match!.Thursday;
        
        if (weekday == Weekday.Friday)
            return x => x.Repeat!.Match!.Friday;
        
        if (weekday == Weekday.Saturday)
            return x => x.Repeat!.Match!.Saturday;
        
        if (weekday == Weekday.Sunday)
            return x => x.Repeat!.Match!.Sunday;

        throw new InvalidOperationException();
    }
}